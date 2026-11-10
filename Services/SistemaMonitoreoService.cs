using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Asoagro.Data;
using Asoagro.Models;
using Microsoft.EntityFrameworkCore;

namespace Asoagro.Services;

/// <summary>
/// Monitor técnico en tiempo real y proveedor de logging de errores del sistema.
/// Registra errores técnicos reales sin revelar secretos, verifica salud de Supabase/almacenamiento
/// y gestiona revocaciones de sesión por usuario.
/// </summary>
public sealed class SistemaMonitoreoService : ILoggerProvider
{
    private const int MaximoErrores = 100;
    private static readonly Regex SecretPattern = new(
        @"(Password|Pwd|ClientSecret|Secret|Token|ApiKey|Authorization|Bearer|Host|Username)\s*=\s*[^;\r\n]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly DateTime _inicioAppUtc = DateTime.UtcNow;

    private readonly object _syncErrores = new();
    private readonly LinkedList<ErrorTecnicoItem> _erroresRecientes = new();
    private readonly ConcurrentDictionary<string, DateTime> _sesionesRevocadas = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _rutaSesiones;
    private readonly object _syncSesiones = new();

    private DateTime? _ultimoIntentoSupabaseUtc;
    private string? _ultimoErrorSupabase;
    private long? _ultimaLatenciaSupabaseMs;
    private bool _ultimoEstadoSupabase;

    public SistemaMonitoreoService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _environment = environment;

        var dataDir = ResolverDirectorioData();
        _rutaSesiones = Path.Combine(dataDir, "seguridad_sesiones_store.json");
        CargarSesionesRevocadas();
    }

    public string ResolverDirectorioData()
    {
        var projectDataDir = Path.Combine(_environment.ContentRootPath, "Data");
        if (Directory.Exists(projectDataDir))
        {
            return projectDataDir;
        }

        var localDataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(localDataDir);
        return localDataDir;
    }

    public ILogger CreateLogger(string categoryName) => new MonitorLogger(this, categoryName);

    public void Dispose()
    {
    }

    internal void RegistrarErrorTecnico(LogLevel level, string categoria, string mensaje, Exception? exception)
    {
        if (level < LogLevel.Error)
        {
            return;
        }

        // Evitar recursión o ruido interno trivial
        var texto = string.IsNullOrWhiteSpace(mensaje) ? exception?.Message ?? "Error técnico" : mensaje;
        if (exception != null && !texto.Contains(exception.GetType().Name, StringComparison.Ordinal))
        {
            texto = $"{texto} ({exception.GetType().Name}: {exception.Message})";
        }

        var sanitizado = SanitizarMensajeTecnico(texto);
        if (sanitizado.Length > 450)
        {
            sanitizado = sanitizado[..447] + "...";
        }

        lock (_syncErrores)
        {
            _erroresRecientes.AddFirst(new ErrorTecnicoItem
            {
                FechaUtc = DateTime.UtcNow,
                Nivel = level == LogLevel.Critical ? "Crítico" : "Error",
                Categoria = AcortarCategoria(categoria),
                Mensaje = sanitizado
            });

            while (_erroresRecientes.Count > MaximoErrores)
            {
                _erroresRecientes.RemoveLast();
            }
        }
    }

    public IReadOnlyList<ErrorTecnicoItem> ObtenerErroresRecientes()
    {
        lock (_syncErrores)
        {
            return _erroresRecientes.ToList();
        }
    }

    public void RevocarSesionUsuario(string correo)
    {
        if (string.IsNullOrWhiteSpace(correo)) return;
        var clave = correo.Trim().ToLowerInvariant();
        _sesionesRevocadas[clave] = DateTime.UtcNow;
        GuardarSesionesRevocadas();
    }

    public DateTime? ObtenerFechaRevocacion(string correo)
    {
        if (string.IsNullOrWhiteSpace(correo)) return null;
        return _sesionesRevocadas.TryGetValue(correo.Trim().ToLowerInvariant(), out var fecha)
            ? fecha
            : null;
    }

    public bool EstaSesionRevocada(string? correo, string? authIatTicks)
    {
        if (string.IsNullOrWhiteSpace(correo)) return false;
        if (!_sesionesRevocadas.TryGetValue(correo.Trim().ToLowerInvariant(), out var revocadaUtc))
        {
            return false;
        }

        if (!long.TryParse(authIatTicks, out var ticks))
        {
            return true;
        }

        var emitidaUtc = new DateTime(ticks, DateTimeKind.Utc);
        return emitidaUtc <= revocadaUtc;
    }

    public async Task<EstadoSistemaDto> DiagnosticarSistemaAsync()
    {
        var dto = new EstadoSistemaDto
        {
            FechaRevisionUtc = DateTime.UtcNow,
            Entorno = _environment.EnvironmentName,
            RuntimeDotNet = RuntimeInformation.FrameworkDescription,
            SistemaOperativo = RuntimeInformation.OSDescription,
            InicioAplicacionUtc = _inicioAppUtc,
            TiempoActivo = DateTime.UtcNow - _inicioAppUtc,
            ErroresRecientes = ObtenerErroresRecientes().ToList()
        };

        // 1. Verificar Supabase / PostgreSQL
        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        dto.SupabaseConfigurado = dbFactory != null;

        if (dbFactory != null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                var conectado = await db.Database.CanConnectAsync();
                sw.Stop();

                dto.SupabaseConectado = conectado;
                dto.SupabaseLatenciaMs = sw.ElapsedMilliseconds;
                dto.UltimoIntentoSupabaseUtc = DateTime.UtcNow;
                dto.SupabaseMensaje = conectado
                    ? $"Conexión activa y verificada ({sw.ElapsedMilliseconds} ms)."
                    : "No se pudo establecer conexión con la base de datos Supabase.";

                _ultimoIntentoSupabaseUtc = dto.UltimoIntentoSupabaseUtc;
                _ultimaLatenciaSupabaseMs = dto.SupabaseLatenciaMs;
                _ultimoEstadoSupabase = conectado;
                _ultimoErrorSupabase = conectado ? null : dto.SupabaseMensaje;

                if (conectado)
                {
                    dto.TablasSupabase = await ConsultarConteosTablasAsync(db);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                var msgSeguro = $"Error al conectar con Supabase ({ex.GetType().Name}): {SanitizarMensajeTecnico(ex.Message)}";
                dto.SupabaseConectado = false;
                dto.SupabaseLatenciaMs = sw.ElapsedMilliseconds;
                dto.UltimoIntentoSupabaseUtc = DateTime.UtcNow;
                dto.SupabaseMensaje = msgSeguro;

                _ultimoIntentoSupabaseUtc = dto.UltimoIntentoSupabaseUtc;
                _ultimaLatenciaSupabaseMs = dto.SupabaseLatenciaMs;
                _ultimoEstadoSupabase = false;
                _ultimoErrorSupabase = msgSeguro;
            }
        }
        else
        {
            dto.SupabaseConectado = false;
            dto.UltimoIntentoSupabaseUtc = _ultimoIntentoSupabaseUtc;
            dto.SupabaseMensaje = "No configurada: falta definir la variable ASOAGRO_SUPABASE_CONNECTION. La aplicación opera en modo de almacenamiento local en memoria/archivo.";
        }

        // 2. Verificar Almacenamiento local Data/
        try
        {
            var dataDir = ResolverDirectorioData();
            var testPath = Path.Combine(dataDir, $".healthcheck_{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(testPath, "ok");
            File.Delete(testPath);

            var archivosJson = Directory.GetFiles(dataDir, "*.json");
            dto.AlmacenamientoLocalDisponible = true;
            dto.AlmacenamientoLocalDetalle = $"Lectura y escritura operativas en Data/ ({archivosJson.Length} archivo(s) de estado persistente).";
        }
        catch (Exception ex)
        {
            dto.AlmacenamientoLocalDisponible = false;
            dto.AlmacenamientoLocalDetalle = $"Sin permiso de escritura en almacenamiento local: {SanitizarMensajeTecnico(ex.Message)}";
        }

        // 3. Google OAuth (solo verificar presencia sin revelar secretos)
        var googleClientId = _configuration["Authentication:Google:ClientId"];
        var googleClientSecret = _configuration["Authentication:Google:ClientSecret"];
        dto.GoogleOAuthConfigurado = !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret);
        dto.GoogleOAuthDetalle = dto.GoogleOAuthConfigurado
            ? "Credenciales OAuth presentes en configuración."
            : string.IsNullOrWhiteSpace(googleClientId)
                ? "Pendiente de configurar: faltan Authentication:Google:ClientId y ClientSecret."
                : "Pendiente de configurar: ClientId definido, pero falta Authentication:Google:ClientSecret.";

        // 4. SMTP (para correos reales de recuperación)
        var smtpHost = _configuration["Smtp:Host"] ?? _configuration["Email:SmtpHost"];
        dto.SmtpConfigurado = !string.IsNullOrWhiteSpace(smtpHost);

        // 5. Respaldo automático
        dto.RespaldoAutomaticoConfigurado = false;
        dto.RespaldoDetalle = "No disponible: no existe mecanismo automático de respaldo conectado en la aplicación.";

        // 6. Construir listado de integraciones reales
        dto.Integraciones = new List<EstadoIntegracionItem>
        {
            new()
            {
                Nombre = "Base de datos Supabase (PostgreSQL)",
                Tipo = "Persistencia principal",
                Operativo = dto.SupabaseConectado,
                Estado = !dto.SupabaseConfigurado ? "No configurado" : (dto.SupabaseConectado ? "Conectado" : "Error de conexión"),
                Detalle = dto.SupabaseMensaje,
                UltimaVerificacionUtc = dto.UltimoIntentoSupabaseUtc,
                UltimoError = _ultimoErrorSupabase
            },
            new()
            {
                Nombre = "Autenticación Google OAuth 2.0",
                Tipo = "Proveedor de identidad",
                Operativo = dto.GoogleOAuthConfigurado,
                Estado = dto.GoogleOAuthConfigurado ? "Configurado" : "Pendiente de configurar",
                Detalle = dto.GoogleOAuthDetalle,
                UltimaVerificacionUtc = dto.FechaRevisionUtc
            },
            new()
            {
                Nombre = "Almacenamiento local de estado (Data/)",
                Tipo = "Persistencia JSON / Notificaciones y Bitácora",
                Operativo = dto.AlmacenamientoLocalDisponible,
                Estado = dto.AlmacenamientoLocalDisponible ? "Operativo" : "Error de escritura",
                Detalle = dto.AlmacenamientoLocalDetalle,
                UltimaVerificacionUtc = dto.FechaRevisionUtc
            },
            new()
            {
                Nombre = "Servicios en segundo plano (Alertas Vencimiento y Seguimiento)",
                Tipo = "Workers ejecutados en Host",
                Operativo = true,
                Estado = "Activos",
                Detalle = "AlertasVencimientoWorker y AlertasSeguimientoWorker registrados en el ciclo de vida de la aplicación.",
                UltimaVerificacionUtc = dto.FechaRevisionUtc
            },
            new()
            {
                Nombre = "Servicio de correo saliente (SMTP)",
                Tipo = "Notificaciones y recuperación por correo",
                Operativo = dto.SmtpConfigurado,
                Estado = dto.SmtpConfigurado ? "Configurado" : "No configurado",
                Detalle = dto.SmtpConfigurado
                    ? "Servidor SMTP detectado en configuración."
                    : "Pendiente de configurar: no hay servidor SMTP definido para envío automático de enlaces.",
                UltimaVerificacionUtc = dto.FechaRevisionUtc
            },
            new()
            {
                Nombre = "Copias de seguridad automáticas",
                Tipo = "Continuidad operativa",
                Operativo = false,
                Estado = "No disponible",
                Detalle = dto.RespaldoDetalle,
                UltimaVerificacionUtc = dto.FechaRevisionUtc
            }
        };

        return dto;
    }

    private static async Task<List<ConteoTablaSistema>> ConsultarConteosTablasAsync(AsoagroDbContext db)
    {
        var resultado = new List<ConteoTablaSistema>();

        async Task MedirAsync(string nombre, Func<Task<int>> consulta)
        {
            try
            {
                var count = await consulta();
                resultado.Add(new ConteoTablaSistema { Tabla = nombre, Registros = count, Estado = "Disponible" });
            }
            catch (Exception ex)
            {
                resultado.Add(new ConteoTablaSistema
                {
                    Tabla = nombre,
                    Registros = null,
                    Estado = $"No accesible ({ex.GetType().Name})"
                });
            }
        }

        await MedirAsync("public.usuarios", () => db.Usuarios.CountAsync());
        await MedirAsync("public.permisos_modulo", () => db.PermisosModulos.CountAsync());
        await MedirAsync("public.auditoria", () => db.Auditorias.CountAsync());
        await MedirAsync("public.asociaciones", () => db.Asociaciones.CountAsync());
        await MedirAsync("public.asociados", () => db.Asociados.CountAsync());
        await MedirAsync("public.documentos", () => db.Documentos.CountAsync());
        await MedirAsync("public.seguimientos_asociados", () => db.SeguimientosAsociados.CountAsync());
        await MedirAsync("public.cultivos", () => db.Cultivos.CountAsync());
        await MedirAsync("public.grupos_vulnerables", () => db.GruposVulnerables.CountAsync());
        await MedirAsync("public.tipos_cuota", () => db.TiposCuota.CountAsync());
        await MedirAsync("public.tipos_documento", () => db.TiposDocumento.CountAsync());

        return resultado;
    }

    public static string SanitizarMensajeTecnico(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var limpio = SecretPattern.Replace(texto, "$1=[REDACTADO]");
        limpio = Regex.Replace(limpio, @"postgres(ql)?://[^\s""']+", "[CADENA_CONEXION_REDACTADA]", RegexOptions.IgnoreCase);
        return limpio;
    }

    private static string AcortarCategoria(string categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria)) return "Sistema";
        var idx = categoria.LastIndexOf('.');
        return idx >= 0 && idx < categoria.Length - 1 ? categoria[(idx + 1)..] : categoria;
    }

    private void CargarSesionesRevocadas()
    {
        lock (_syncSesiones)
        {
            try
            {
                if (!File.Exists(_rutaSesiones)) return;
                var json = File.ReadAllText(_rutaSesiones);
                var data = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json);
                if (data == null) return;
                foreach (var kv in data)
                {
                    _sesionesRevocadas[kv.Key] = DateTime.SpecifyKind(kv.Value, DateTimeKind.Utc);
                }
            }
            catch
            {
                // Ignorar archivo corrupto o inaccesible
            }
        }
    }

    private void GuardarSesionesRevocadas()
    {
        lock (_syncSesiones)
        {
            try
            {
                var dict = _sesionesRevocadas.ToDictionary(k => k.Key, v => v.Value);
                var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_rutaSesiones, json);
            }
            catch
            {
                // Ignorar error de E/S sin interrumpir flujo
            }
        }
    }

    private sealed class MonitorLogger(SistemaMonitoreoService owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var msg = formatter(state, exception);
            owner.RegistrarErrorTecnico(logLevel, category, msg, exception);
        }
    }
}


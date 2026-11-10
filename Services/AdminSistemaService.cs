using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Asoagro.Configuration;
using Asoagro.Data;
using Asoagro.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Asoagro.Services;

public sealed class AdminSistemaService
{
    public static readonly string[] RolesSistema =
    {
        "Administrador",
        "Presidenta",
        "Secretaria",
        "Tesorero",
        "AdministradorPdv"
    };

    private static readonly Regex CorreoRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private readonly IServiceProvider _serviceProvider;
    private readonly SistemaMonitoreoService _monitoreo;
    private readonly IOptions<AsoagroOptions> _options;
    private readonly IDocumentoAsociacionService _documentoAsociacionService;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<UsuarioEntity> _passwordHasher = new();

    private readonly object _syncLocal = new();
    private readonly string _rutaAuditoriaLocal;
    private readonly string _rutaUsuariosLocal;
    private readonly string _rutaCatalogosLocal;
    private readonly string _rutaParametrosLocal;
    private readonly string _rutaRecuperacionLocal;
    private readonly string _rutaBootstrapEstadoLocal;

    public AdminSistemaService(
        IServiceProvider serviceProvider,
        SistemaMonitoreoService monitoreo,
        IOptions<AsoagroOptions> options,
        IDocumentoAsociacionService documentoAsociacionService,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _monitoreo = monitoreo;
        _options = options;
        _documentoAsociacionService = documentoAsociacionService;
        _configuration = configuration;

        var dataDir = _monitoreo.ResolverDirectorioData();
        _rutaAuditoriaLocal = Path.Combine(dataDir, "auditoria_store.json");
        _rutaUsuariosLocal = Path.Combine(dataDir, "usuarios_permisos_store.json");
        _rutaCatalogosLocal = Path.Combine(dataDir, "catalogos_sistema_store.json");
        _rutaParametrosLocal = Path.Combine(dataDir, "parametros_sistema_store.json");
        _rutaRecuperacionLocal = Path.Combine(dataDir, "recuperacion_tokens_store.json");
        _rutaBootstrapEstadoLocal = Path.Combine(dataDir, "admin_bootstrap_estado.json");

        InicializarParametrosEnInicio();
    }

    public bool UsaSupabase => _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>() != null;

    // =========================================================================
    // 1. ASOCIACIONES DISPONIBLES PARA ASIGNACIÓN DE USUARIOS
    // =========================================================================
    public async Task<List<(int Id, string Nombre)>> ObtenerAsociacionesDisponiblesAsync()
    {
        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                return await db.Asociaciones
                    .AsNoTracking()
                    .Where(a => a.Activo)
                    .OrderBy(a => a.Nombre)
                    .Select(a => new ValueTuple<int, string>(a.Id, a.Nombre))
                    .ToListAsync();
            }
            catch
            {
                // Fallback seguro si falla consulta
            }
        }

        var inst = _documentoAsociacionService.ObtenerAsociacionesInstitucionales();
        return inst
            .Select((a, idx) => (idx + 1, string.IsNullOrWhiteSpace(a.Sigla) ? a.NombreLegal : $"{a.Sigla} - {a.NombreLegal}"))
            .ToList();
    }

    // =========================================================================
    // 2. USUARIOS, ROLES Y PERMISOS
    // =========================================================================
    public async Task<List<UsuarioAdminDto>> ObtenerUsuariosAsync()
    {
        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        var recuperaciones = CargarRecuperacionesVigentes();

        if (dbFactory != null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var lista = await db.Usuarios
                .AsNoTracking()
                .Include(u => u.Asociacion)
                .OrderBy(u => u.NombreCompleto)
                .ToListAsync();

            return lista.Select(u => MapearUsuarioDto(u, recuperaciones)).ToList();
        }

        var store = CargarStoreUsuariosLocal();
        var asocMap = (await ObtenerAsociacionesDisponiblesAsync()).ToDictionary(a => a.Id, a => a.Nombre);
        return store.Usuarios
            .OrderBy(u => u.NombreCompleto)
            .Select(u => new UsuarioAdminDto
            {
                Id = u.Id,
                NombreCompleto = u.NombreCompleto,
                DocumentoId = u.DocumentoId,
                Correo = u.Correo,
                Telefono = u.Telefono,
                Rol = u.Rol,
                AsociacionId = u.AsociacionId,
                AsociacionNombre = u.AsociacionId.HasValue && asocMap.TryGetValue(u.AsociacionId.Value, out var nom)
                    ? nom
                    : "Federación FAAUA (Global)",
                Activo = u.Activo,
                TieneContrasenaLocal = !string.IsNullOrWhiteSpace(u.ContrasenaHash),
                RecuperacionPendiente = recuperaciones.ContainsKey(u.Correo.ToLowerInvariant()),
                UltimoAccesoUtc = u.UltimoAcceso,
                SesionRevocadaEnUtc = _monitoreo.ObtenerFechaRevocacion(u.Correo),
                CreadoEnUtc = u.CreadoEn
            })
            .ToList();
    }

    public async Task<UsuarioEntity?> BuscarUsuarioPorCorreoAsync(string correo)
    {
        if (string.IsNullOrWhiteSpace(correo)) return null;
        var norm = correo.Trim().ToLowerInvariant();

        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                return await db.Usuarios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Correo.ToLower() == norm);
            }
            catch
            {
                return null;
            }
        }

        var store = CargarStoreUsuariosLocal();
        return store.Usuarios.FirstOrDefault(u => u.Correo.Equals(norm, StringComparison.OrdinalIgnoreCase));
    }

    public bool VerificarPasswordHash(UsuarioEntity usuario, string passwordPlano)
    {
        if (string.IsNullOrWhiteSpace(usuario.ContrasenaHash) || string.IsNullOrWhiteSpace(passwordPlano))
        {
            return false;
        }

        var res = _passwordHasher.VerifyHashedPassword(usuario, usuario.ContrasenaHash, passwordPlano);
        return res == PasswordVerificationResult.Success || res == PasswordVerificationResult.SuccessRehashNeeded;
    }

    public async Task RegistrarAccesoUsuarioAsync(string correo)
    {
        if (string.IsNullOrWhiteSpace(correo)) return;
        var norm = correo.Trim().ToLowerInvariant();

        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo.ToLower() == norm);
                if (user != null)
                {
                    user.UltimoAcceso = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
                return;
            }
            catch
            {
                return;
            }
        }

        lock (_syncLocal)
        {
            var store = CargarStoreUsuariosLocalInternal();
            var user = store.Usuarios.FirstOrDefault(u => u.Correo.Equals(norm, StringComparison.OrdinalIgnoreCase));
            if (user != null)
            {
                user.UltimoAcceso = DateTime.UtcNow;
                GuardarStoreUsuariosLocalInternal(store);
            }
        }
    }

    public async Task<ResultadoOperacionAdmin> GuardarUsuarioAsync(
        SolicitudUsuarioAdmin solicitud,
        string actorCorreo,
        string actorNombre,
        string actorRol,
        string? ipTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(solicitud.NombreCompleto) || solicitud.NombreCompleto.Trim().Length < 3)
        {
            return Fallo("El nombre completo es obligatorio (mínimo 3 caracteres).");
        }

        if (string.IsNullOrWhiteSpace(solicitud.DocumentoId) || solicitud.DocumentoId.Trim().Length < 5)
        {
            return Fallo("El documento de identidad es obligatorio (mínimo 5 caracteres).");
        }

        if (string.IsNullOrWhiteSpace(solicitud.Correo) || !CorreoRegex.IsMatch(solicitud.Correo.Trim()))
        {
            return Fallo("Ingresa un correo institucional válido.");
        }

        if (!RolesSistema.Contains(solicitud.Rol))
        {
            return Fallo("El rol seleccionado no es válido en el sistema.");
        }

        if (!string.IsNullOrWhiteSpace(solicitud.ContrasenaNueva) && solicitud.ContrasenaNueva.Trim().Length < 8)
        {
            return Fallo("La contraseña debe tener al menos 8 caracteres.");
        }

        var correoNorm = solicitud.Correo.Trim().ToLowerInvariant();
        var docNorm = solicitud.DocumentoId.Trim();
        var nombreNorm = solicitud.NombreCompleto.Trim();

        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var duplicadoCorreo = await db.Usuarios.AnyAsync(u =>
                u.Correo.ToLower() == correoNorm && (!solicitud.Id.HasValue || u.Id != solicitud.Id.Value));
            if (duplicadoCorreo)
            {
                return Fallo($"Ya existe una cuenta registrada con el correo '{correoNorm}'.");
            }

            if (solicitud.Id.HasValue && solicitud.Id.Value > 0)
            {
                var entidad = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == solicitud.Id.Value);
                if (entidad == null) return Fallo("El usuario especificado ya no existe.");

                var rolAnterior = entidad.Rol;
                var activoAnterior = entidad.Activo;

                entidad.NombreCompleto = nombreNorm;
                entidad.DocumentoId = docNorm;
                entidad.Correo = correoNorm;
                entidad.Telefono = solicitud.Telefono?.Trim();
                entidad.Rol = solicitud.Rol;
                entidad.AsociacionId = solicitud.AsociacionId;
                entidad.Activo = solicitud.Activo;

                if (!string.IsNullOrWhiteSpace(solicitud.ContrasenaNueva))
                {
                    entidad.ContrasenaHash = _passwordHasher.HashPassword(entidad, solicitud.ContrasenaNueva.Trim());
                }

                await db.SaveChangesAsync();

                if (!entidad.Activo || rolAnterior != entidad.Rol)
                {
                    _monitoreo.RevocarSesionUsuario(entidad.Correo);
                }

                await RegistrarAuditoriaAsync(
                    actorNombre, actorCorreo, actorRol,
                    "Usuarios, roles y permisos",
                    "Actualización de cuenta y permisos",
                    entidad.Correo,
                    "Autorizado",
                    $"Rol: {rolAnterior} -> {entidad.Rol}; Activo: {activoAnterior} -> {entidad.Activo}; AsociacionId: {entidad.AsociacionId?.ToString() ?? "Global"}",
                    ipTerminal,
                    entidad.Id);

                return Ok($"Cuenta '{entidad.NombreCompleto}' actualizada correctamente.");
            }
            else
            {
                var nueva = new UsuarioEntity
                {
                    NombreCompleto = nombreNorm,
                    DocumentoId = docNorm,
                    Correo = correoNorm,
                    Telefono = solicitud.Telefono?.Trim(),
                    Rol = solicitud.Rol,
                    AsociacionId = solicitud.AsociacionId,
                    Activo = solicitud.Activo,
                    CreadoEn = DateTime.UtcNow
                };

                if (!string.IsNullOrWhiteSpace(solicitud.ContrasenaNueva))
                {
                    nueva.ContrasenaHash = _passwordHasher.HashPassword(nueva, solicitud.ContrasenaNueva.Trim());
                }

                db.Usuarios.Add(nueva);
                await db.SaveChangesAsync();

                await RegistrarAuditoriaAsync(
                    actorNombre, actorCorreo, actorRol,
                    "Usuarios, roles y permisos",
                    "Creación de cuenta de usuario",
                    nueva.Correo,
                    "Autorizado",
                    $"Rol asignado: {nueva.Rol}; Activo: {nueva.Activo}; AsociacionId: {nueva.AsociacionId?.ToString() ?? "Global"}",
                    ipTerminal,
                    nueva.Id);

                return Ok($"Cuenta '{nueva.NombreCompleto}' creada correctamente.");
            }
        }

        // Modo almacenamiento local
        lock (_syncLocal)
        {
            var store = CargarStoreUsuariosLocalInternal();
            var dup = store.Usuarios.Any(u =>
                u.Correo.Equals(correoNorm, StringComparison.OrdinalIgnoreCase) &&
                (!solicitud.Id.HasValue || u.Id != solicitud.Id.Value));
            if (dup)
            {
                return Fallo($"Ya existe una cuenta registrada con el correo '{correoNorm}'.");
            }

            if (solicitud.Id.HasValue && solicitud.Id.Value > 0)
            {
                var existente = store.Usuarios.FirstOrDefault(u => u.Id == solicitud.Id.Value);
                if (existente == null) return Fallo("El usuario especificado no existe.");

                var rolAnterior = existente.Rol;
                var activoAnterior = existente.Activo;

                existente.NombreCompleto = nombreNorm;
                existente.DocumentoId = docNorm;
                existente.Correo = correoNorm;
                existente.Telefono = solicitud.Telefono?.Trim();
                existente.Rol = solicitud.Rol;
                existente.AsociacionId = solicitud.AsociacionId;
                existente.Activo = solicitud.Activo;

                if (!string.IsNullOrWhiteSpace(solicitud.ContrasenaNueva))
                {
                    existente.ContrasenaHash = _passwordHasher.HashPassword(existente, solicitud.ContrasenaNueva.Trim());
                }

                GuardarStoreUsuariosLocalInternal(store);

                if (!existente.Activo || rolAnterior != existente.Rol)
                {
                    _monitoreo.RevocarSesionUsuario(existente.Correo);
                }

                _ = RegistrarAuditoriaAsync(
                    actorNombre, actorCorreo, actorRol,
                    "Usuarios, roles y permisos",
                    "Actualización de cuenta y permisos",
                    existente.Correo,
                    "Autorizado",
                    $"Rol: {rolAnterior} -> {existente.Rol}; Activo: {activoAnterior} -> {existente.Activo}",
                    ipTerminal,
                    existente.Id);

                return Ok($"Cuenta '{existente.NombreCompleto}' actualizada correctamente.");
            }
            else
            {
                var nuevoId = store.Usuarios.Count == 0 ? 1 : store.Usuarios.Max(u => u.Id) + 1;
                var nuevo = new UsuarioEntity
                {
                    Id = nuevoId,
                    NombreCompleto = nombreNorm,
                    DocumentoId = docNorm,
                    Correo = correoNorm,
                    Telefono = solicitud.Telefono?.Trim(),
                    Rol = solicitud.Rol,
                    AsociacionId = solicitud.AsociacionId,
                    Activo = solicitud.Activo,
                    CreadoEn = DateTime.UtcNow
                };

                if (!string.IsNullOrWhiteSpace(solicitud.ContrasenaNueva))
                {
                    nuevo.ContrasenaHash = _passwordHasher.HashPassword(nuevo, solicitud.ContrasenaNueva.Trim());
                }

                store.Usuarios.Add(nuevo);
                GuardarStoreUsuariosLocalInternal(store);

                _ = RegistrarAuditoriaAsync(
                    actorNombre, actorCorreo, actorRol,
                    "Usuarios, roles y permisos",
                    "Creación de cuenta de usuario",
                    nuevo.Correo,
                    "Autorizado",
                    $"Rol asignado: {nuevo.Rol}; Activo: {nuevo.Activo}",
                    ipTerminal,
                    nuevo.Id);

                return Ok($"Cuenta '{nuevo.NombreCompleto}' creada correctamente.");
            }
        }
    }

    public async Task<ResultadoOperacionAdmin> AlternarEstadoUsuarioAsync(
        int usuarioId,
        string actorCorreo,
        string actorNombre,
        string actorRol,
        string? ipTerminal = null)
    {
        if (string.Equals(actorCorreo, string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            return Fallo("No se pudo identificar al administrador en sesión.");
        }

        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (user == null) return Fallo("Usuario no encontrado.");

            if (user.Correo.Equals(actorCorreo, StringComparison.OrdinalIgnoreCase) && user.Activo)
            {
                return Fallo("No puedes desactivar tu propia cuenta mientras administras el sistema.");
            }

            user.Activo = !user.Activo;
            await db.SaveChangesAsync();

            if (!user.Activo)
            {
                _monitoreo.RevocarSesionUsuario(user.Correo);
            }

            await RegistrarAuditoriaAsync(
                actorNombre, actorCorreo, actorRol,
                "Usuarios, roles y permisos",
                user.Activo ? "Activación de usuario" : "Desactivación de usuario",
                user.Correo,
                "Autorizado",
                $"Estado actualizado a {(user.Activo ? "Activo" : "Inactivo")}",
                ipTerminal,
                user.Id);

            return Ok($"Usuario '{user.NombreCompleto}' {(user.Activo ? "activado" : "desactivado")}.");
        }

        lock (_syncLocal)
        {
            var store = CargarStoreUsuariosLocalInternal();
            var user = store.Usuarios.FirstOrDefault(u => u.Id == usuarioId);
            if (user == null) return Fallo("Usuario no encontrado.");

            if (user.Correo.Equals(actorCorreo, StringComparison.OrdinalIgnoreCase) && user.Activo)
            {
                return Fallo("No puedes desactivar tu propia cuenta mientras administras el sistema.");
            }

            user.Activo = !user.Activo;
            GuardarStoreUsuariosLocalInternal(store);

            if (!user.Activo)
            {
                _monitoreo.RevocarSesionUsuario(user.Correo);
            }

            _ = RegistrarAuditoriaAsync(
                actorNombre, actorCorreo, actorRol,
                "Usuarios, roles y permisos",
                user.Activo ? "Activación de usuario" : "Desactivación de usuario",
                user.Correo,
                "Autorizado",
                $"Estado actualizado a {(user.Activo ? "Activo" : "Inactivo")}",
                ipTerminal,
                user.Id);

            return Ok($"Usuario '{user.NombreCompleto}' {(user.Activo ? "activado" : "desactivado")}.");
        }
    }

    public async Task<ResultadoOperacionAdmin> RevocarSesionesAsync(
        int usuarioId,
        string correoUsuario,
        string actorCorreo,
        string actorNombre,
        string actorRol,
        string? ipTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(correoUsuario))
        {
            return Fallo("Correo de usuario inválido.");
        }

        _monitoreo.RevocarSesionUsuario(correoUsuario);

        await RegistrarAuditoriaAsync(
            actorNombre, actorCorreo, actorRol,
            "Usuarios, roles y permisos",
            "Revocación de sesiones activas",
            correoUsuario,
            "Autorizado",
            "Se invalidaron las sesiones previas del usuario; deberá autenticarse nuevamente.",
            ipTerminal,
            usuarioId);

        return Ok($"Sesiones revocadas para '{correoUsuario}'. Cualquier sesión activa anterior quedará cerrada en su siguiente solicitud.");
    }

    public async Task<ResultadoOperacionAdmin> SolicitarRecuperacionAccesoAsync(
        int usuarioId,
        string correoUsuario,
        string actorCorreo,
        string actorNombre,
        string actorRol,
        string? ipTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(correoUsuario))
        {
            return Fallo("Correo de usuario inválido.");
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(24);
        var tokenPlano = Convert.ToHexString(tokenBytes);
        var tokenHash = CalcularSha256(tokenPlano);

        lock (_syncLocal)
        {
            var dict = CargarRecuperacionesVigentesInternal();
            dict[correoUsuario.Trim().ToLowerInvariant()] = new EstadoRecuperacionUsuario
            {
                Correo = correoUsuario.Trim().ToLowerInvariant(),
                TokenHash = tokenHash,
                ExpiraEnUtc = DateTime.UtcNow.AddMinutes(30),
                SolicitadoPor = actorCorreo
            };
            GuardarRecuperacionesInternal(dict);
        }

        // Revocar sesiones anteriores al iniciar recuperación
        _monitoreo.RevocarSesionUsuario(correoUsuario);

        await RegistrarAuditoriaAsync(
            actorNombre, actorCorreo, actorRol,
            "Usuarios, roles y permisos",
            "Inicio de flujo de recuperación de acceso",
            correoUsuario,
            "Autorizado",
            "Solicitud de restablecimiento seguro registrada (vigencia 30 min) con sesiones previas revocadas. Sin exposición de contraseñas.",
            ipTerminal,
            usuarioId);

        var smtpHost = _serviceProvider.GetService<IConfiguration>()?["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            return Ok($"Recuperación habilitada para '{correoUsuario}' por 30 minutos y sesiones previas revocadas. Nota: no hay servidor SMTP configurado para envío automático de correo; el usuario puede completar el restablecimiento en /recuperar o mediante Google OAuth.");
        }

        return Ok($"Flujo seguro de recuperación iniciado para '{correoUsuario}' con vigencia de 30 minutos.");
    }

    public async Task<ResultadoOperacionAdmin> CompletarRecuperacionAccesoAsync(
        string correo,
        string nuevaContrasena,
        string? ipTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(correo) || !CorreoRegex.IsMatch(correo.Trim()))
        {
            return Fallo("Ingresa un correo institucional válido.");
        }

        if (string.IsNullOrWhiteSpace(nuevaContrasena) || nuevaContrasena.Trim().Length < 8)
        {
            return Fallo("La nueva contraseña debe tener al menos 8 caracteres.");
        }

        var norm = correo.Trim().ToLowerInvariant();
        bool tieneSolicitudVigente;
        lock (_syncLocal)
        {
            var dict = CargarRecuperacionesVigentesInternal();
            tieneSolicitudVigente = dict.TryGetValue(norm, out var estado) && estado.ExpiraEnUtc > DateTime.UtcNow;
            if (!tieneSolicitudVigente)
            {
                return Fallo("No existe una solicitud de recuperación vigente para este correo. Solicita al Administrador del sistema habilitar el restablecimiento de acceso.");
            }
            dict.Remove(norm);
            GuardarRecuperacionesInternal(dict);
        }

        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo.ToLower() == norm);
            if (user == null || !user.Activo)
            {
                return Fallo("La cuenta no existe o está desactivada.");
            }

            user.ContrasenaHash = _passwordHasher.HashPassword(user, nuevaContrasena.Trim());
            await db.SaveChangesAsync();
            _monitoreo.RevocarSesionUsuario(norm);

            await RegistrarAuditoriaAsync(
                user.NombreCompleto, user.Correo, user.Rol,
                "Autenticación",
                "Restablecimiento seguro de contraseña",
                user.Correo,
                "Autorizado",
                "Contraseña actualizada mediante hash seguro en flujo /recuperar.",
                ipTerminal,
                user.Id);

            return Ok("Tu contraseña ha sido actualizada de forma segura. Ya puedes iniciar sesión.");
        }

        lock (_syncLocal)
        {
            var store = CargarStoreUsuariosLocalInternal();
            var user = store.Usuarios.FirstOrDefault(u => u.Correo.Equals(norm, StringComparison.OrdinalIgnoreCase));
            if (user == null || !user.Activo)
            {
                return Fallo("La cuenta no existe o está desactivada.");
            }

            user.ContrasenaHash = _passwordHasher.HashPassword(user, nuevaContrasena.Trim());
            GuardarStoreUsuariosLocalInternal(store);
            _monitoreo.RevocarSesionUsuario(norm);

            _ = RegistrarAuditoriaAsync(
                user.NombreCompleto, user.Correo, user.Rol,
                "Autenticación",
                "Restablecimiento seguro de contraseña",
                user.Correo,
                "Autorizado",
                "Contraseña actualizada mediante hash seguro en flujo /recuperar.",
                ipTerminal,
                user.Id);

            return Ok("Tu contraseña ha sido actualizada de forma segura. Ya puedes iniciar sesión.");
        }
    }

    public bool RequiereCambioContrasena(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo)) return false;
        var norm = correo.Trim().ToLowerInvariant();
        lock (_syncLocal)
        {
            var estado = CargarBootstrapEstadoInternal();
            return estado.CuentasPendientesCambioClave.Contains(norm);
        }
    }

    public async Task<ResultadoOperacionAdmin> CompletarPrimerCambioContrasenaAsync(
        string correo,
        string contrasenaActual,
        string nuevaContrasena,
        string? ipTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(correo) || !CorreoRegex.IsMatch(correo.Trim()))
        {
            return Fallo("Correo institucional inválido.");
        }

        if (string.IsNullOrWhiteSpace(contrasenaActual))
        {
            return Fallo("Ingresa tu contraseña inicial actual.");
        }

        if (string.IsNullOrWhiteSpace(nuevaContrasena) || nuevaContrasena.Trim().Length < 8)
        {
            return Fallo("La nueva contraseña debe tener al menos 8 caracteres.");
        }

        if (string.Equals(contrasenaActual.Trim(), nuevaContrasena.Trim(), StringComparison.Ordinal))
        {
            return Fallo("La nueva contraseña debe ser diferente a la contraseña inicial.");
        }

        var norm = correo.Trim().ToLowerInvariant();
        var nuevoLimpio = nuevaContrasena.Trim();

        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo.ToLower() == norm);
            if (user == null || !user.Activo)
            {
                return Fallo("La cuenta no existe o está desactivada.");
            }

            if (string.IsNullOrWhiteSpace(user.ContrasenaHash) || !VerificarPasswordHash(user, contrasenaActual.Trim()))
            {
                return Fallo("La contraseña inicial actual no coincide.");
            }

            user.ContrasenaHash = _passwordHasher.HashPassword(user, nuevoLimpio);
            await db.SaveChangesAsync();

            LimpiarRequisitoCambioContrasenaInternal(norm);
            await RegistrarAuditoriaAsync(
                user.NombreCompleto, user.Correo, user.Rol,
                "Autenticación",
                "Cambio obligatorio de contraseña en primer ingreso",
                user.Correo,
                "Autorizado",
                "Credencial inicial actualizada con hash seguro por el operador autenticado.",
                ipTerminal,
                user.Id);

            return Ok("Contraseña personal configurada correctamente.");
        }

        lock (_syncLocal)
        {
            var store = CargarStoreUsuariosLocalInternal();
            var user = store.Usuarios.FirstOrDefault(u => u.Correo.Equals(norm, StringComparison.OrdinalIgnoreCase));
            if (user == null || !user.Activo)
            {
                return Fallo("La cuenta no existe o está desactivada.");
            }

            if (string.IsNullOrWhiteSpace(user.ContrasenaHash) || !VerificarPasswordHash(user, contrasenaActual.Trim()))
            {
                return Fallo("La contraseña inicial actual no coincide.");
            }

            user.ContrasenaHash = _passwordHasher.HashPassword(user, nuevoLimpio);
            GuardarStoreUsuariosLocalInternal(store);
            LimpiarRequisitoCambioContrasenaInternal(norm);

            _ = RegistrarAuditoriaAsync(
                user.NombreCompleto, user.Correo, user.Rol,
                "Autenticación",
                "Cambio obligatorio de contraseña en primer ingreso",
                user.Correo,
                "Autorizado",
                "Credencial inicial actualizada con hash seguro por el operador autenticado.",
                ipTerminal,
                user.Id);

            return Ok("Contraseña personal configurada correctamente.");
        }
    }

    public async Task AsegurarCuentasYAdminInicialAsync()
    {
        var claveInicial = (_configuration["Asoagro:AdminBootstrap:InitialPassword"]
            ?? Environment.GetEnvironmentVariable("ASOAGRO_ADMIN_INITIAL_PASSWORD")
            ?? Environment.GetEnvironmentVariable("ASOAGRO_ADMIN_INITIAL_PASSWORD", EnvironmentVariableTarget.User)
            ?? string.Empty).Trim();

        var asignacionesEquipo = new (string Correo, string Nombre, string Documento, string Rol)[]
        {
            ("admin@asoagro-faaua.org", "Administrador del Sistema ASOAGRO", "900000001", AuthRoleHelper.RolAdministrador),
            ("manuela.mosqueraar@amigo.edu.co", "Manuela Andrea Mosquera", "100000004", AuthRoleHelper.RolSecretaria),
            ("manumosque743@gmail.com", "Manuela Mosquera", "100000007", AuthRoleHelper.RolPresidenta),
            ("juan.padillamo@amigo.edu.co", "Juan Padilla", "100000003", AuthRoleHelper.RolTesorero),
            ("karen.argumedoco@amigo.edu.co", "Karen Argumedo", "100000002", AuthRoleHelper.RolAdministradorPdv)
        };

        var estadoBootstrap = CargarBootstrapEstadoInternal();
        var requiereSincEquipo = !estadoBootstrap.RolesEquipoV2Sincronizados;

        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                if (requiereSincEquipo)
                {
                    foreach (var item in asignacionesEquipo)
                    {
                        var existente = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo.ToLower() == item.Correo);
                        if (existente == null)
                        {
                            db.Usuarios.Add(new UsuarioEntity
                            {
                                NombreCompleto = item.Nombre,
                                DocumentoId = item.Documento,
                                Correo = item.Correo,
                                Rol = item.Rol,
                                Activo = true,
                                CreadoEn = DateTime.UtcNow
                            });
                        }
                        else
                        {
                            existente.Rol = item.Rol;
                            existente.Activo = true;
                        }
                    }

                    await db.SaveChangesAsync();
                    MarcarRolesEquipoSincronizadosInternal();
                }

                var adminDb = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo.ToLower() == "admin@asoagro-faaua.org");
                if (adminDb == null)
                {
                    adminDb = new UsuarioEntity
                    {
                        NombreCompleto = "Administrador del Sistema ASOAGRO",
                        DocumentoId = "900000001",
                        Correo = "admin@asoagro-faaua.org",
                        Rol = AuthRoleHelper.RolAdministrador,
                        Activo = true,
                        CreadoEn = DateTime.UtcNow
                    };
                    db.Usuarios.Add(adminDb);
                    await db.SaveChangesAsync();
                }

                if (string.IsNullOrWhiteSpace(adminDb.ContrasenaHash) && claveInicial.Length >= 8)
                {
                    adminDb.ContrasenaHash = _passwordHasher.HashPassword(adminDb, claveInicial);
                    await db.SaveChangesAsync();
                    MarcarRequisitoCambioContrasenaInternal("admin@asoagro-faaua.org");
                    await RegistrarAuditoriaAsync(
                        "Sistema ASOAGRO", "admin@asoagro-faaua.org", AuthRoleHelper.RolAdministrador,
                        "Autenticación",
                        "Inicialización segura de cuenta Administrador",
                        "admin@asoagro-faaua.org",
                        "Autorizado",
                        "Contraseña inicial configurada mediante hash seguro; cambio obligatorio pendiente en primer ingreso.");
                }
                return;
            }
            catch
            {
                // Si Supabase aún no responde o está sin tablas, continuamos sin bloquear arranque
            }
        }

        lock (_syncLocal)
        {
            var store = CargarStoreUsuariosLocalInternal();
            var modificado = false;

            if (requiereSincEquipo)
            {
                var nextId = store.Usuarios.Count == 0 ? 1 : store.Usuarios.Max(u => u.Id) + 1;
                foreach (var item in asignacionesEquipo)
                {
                    var existente = store.Usuarios.FirstOrDefault(u => u.Correo.Equals(item.Correo, StringComparison.OrdinalIgnoreCase));
                    if (existente == null)
                    {
                        store.Usuarios.Add(new UsuarioEntity
                        {
                            Id = nextId++,
                            NombreCompleto = item.Nombre,
                            DocumentoId = item.Documento,
                            Correo = item.Correo,
                            Rol = item.Rol,
                            Activo = true,
                            CreadoEn = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        existente.Rol = item.Rol;
                        existente.Activo = true;
                    }
                }
                modificado = true;
                MarcarRolesEquipoSincronizadosInternal();
            }

            var adminLocal = store.Usuarios.FirstOrDefault(u => u.Correo.Equals("admin@asoagro-faaua.org", StringComparison.OrdinalIgnoreCase));
            if (adminLocal == null)
            {
                var nextId = store.Usuarios.Count == 0 ? 1 : store.Usuarios.Max(u => u.Id) + 1;
                adminLocal = new UsuarioEntity
                {
                    Id = nextId,
                    NombreCompleto = "Administrador del Sistema ASOAGRO",
                    DocumentoId = "900000001",
                    Correo = "admin@asoagro-faaua.org",
                    Rol = AuthRoleHelper.RolAdministrador,
                    Activo = true,
                    CreadoEn = DateTime.UtcNow
                };
                store.Usuarios.Add(adminLocal);
                modificado = true;
            }

            if (string.IsNullOrWhiteSpace(adminLocal.ContrasenaHash) && claveInicial.Length >= 8)
            {
                adminLocal.ContrasenaHash = _passwordHasher.HashPassword(adminLocal, claveInicial);
                modificado = true;
                MarcarRequisitoCambioContrasenaInternal("admin@asoagro-faaua.org");
                _ = RegistrarAuditoriaAsync(
                    "Sistema ASOAGRO", "admin@asoagro-faaua.org", AuthRoleHelper.RolAdministrador,
                    "Autenticación",
                    "Inicialización segura de cuenta Administrador",
                    "admin@asoagro-faaua.org",
                    "Autorizado",
                    "Contraseña inicial configurada mediante hash seguro; cambio obligatorio pendiente en primer ingreso.");
            }

            if (modificado)
            {
                GuardarStoreUsuariosLocalInternal(store);
            }
        }
    }

    // Matriz de roles y permisos (Mínimo Privilegio)
    public async Task<List<PermisoModuloEntity>> ObtenerMatrizPermisosAsync()
    {
        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var existentes = await db.PermisosModulos
                .AsNoTracking()
                .OrderBy(p => p.Rol)
                .ThenBy(p => p.Modulo)
                .ToListAsync();

            if (existentes.Count > 0)
            {
                return existentes;
            }

            var semilla = GenerarMatrizMinimoPrivilegio();
            db.PermisosModulos.AddRange(semilla);
            await db.SaveChangesAsync();
            return semilla;
        }

        var store = CargarStoreUsuariosLocal();
        return store.Permisos
            .OrderBy(p => p.Rol)
            .ThenBy(p => p.Modulo)
            .ToList();
    }

    public async Task<ResultadoOperacionAdmin> GuardarPermisoModuloAsync(
        PermisoModuloEntity permiso,
        string actorCorreo,
        string actorNombre,
        string actorRol,
        string? ipTerminal = null)
    {
        // Regla de seguridad: Auditoría y seguridad es de solo lectura (inmutable para todos)
        if (permiso.Modulo.Equals("Auditoría y seguridad", StringComparison.OrdinalIgnoreCase))
        {
            permiso.PuedeCrear = false;
            permiso.PuedeEditar = false;
            permiso.PuedeEliminar = false;
        }

        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var existente = await db.PermisosModulos.FirstOrDefaultAsync(p =>
                p.Id == permiso.Id || (p.Rol == permiso.Rol && p.Modulo == permiso.Modulo));

            if (existente == null)
            {
                db.PermisosModulos.Add(permiso);
            }
            else
            {
                existente.PuedeVer = permiso.PuedeVer;
                existente.PuedeCrear = permiso.PuedeCrear;
                existente.PuedeEditar = permiso.PuedeEditar;
                existente.PuedeEliminar = permiso.PuedeEliminar;
            }

            await db.SaveChangesAsync();

            await RegistrarAuditoriaAsync(
                actorNombre, actorCorreo, actorRol,
                "Usuarios, roles y permisos",
                "Actualización en matriz de permisos",
                $"{permiso.Rol} · {permiso.Modulo}",
                "Autorizado",
                $"Ver={permiso.PuedeVer}, Crear={permiso.PuedeCrear}, Editar={permiso.PuedeEditar}, Eliminar={permiso.PuedeEliminar}",
                ipTerminal,
                permiso.Id);

            return Ok($"Permisos actualizados para {permiso.Rol} en '{permiso.Modulo}'.");
        }

        lock (_syncLocal)
        {
            var store = CargarStoreUsuariosLocalInternal();
            var existente = store.Permisos.FirstOrDefault(p =>
                p.Id == permiso.Id || (p.Rol == permiso.Rol && p.Modulo == permiso.Modulo));

            if (existente == null)
            {
                permiso.Id = store.Permisos.Count == 0 ? 1 : store.Permisos.Max(p => p.Id) + 1;
                store.Permisos.Add(permiso);
            }
            else
            {
                existente.PuedeVer = permiso.PuedeVer;
                existente.PuedeCrear = permiso.PuedeCrear;
                existente.PuedeEditar = permiso.PuedeEditar;
                existente.PuedeEliminar = permiso.PuedeEliminar;
            }

            GuardarStoreUsuariosLocalInternal(store);

            _ = RegistrarAuditoriaAsync(
                actorNombre, actorCorreo, actorRol,
                "Usuarios, roles y permisos",
                "Actualización en matriz de permisos",
                $"{permiso.Rol} · {permiso.Modulo}",
                "Autorizado",
                $"Ver={permiso.PuedeVer}, Crear={permiso.PuedeCrear}, Editar={permiso.PuedeEditar}, Eliminar={permiso.PuedeEliminar}",
                ipTerminal,
                permiso.Id);

            return Ok($"Permisos actualizados para {permiso.Rol} en '{permiso.Modulo}'.");
        }
    }

    // =========================================================================
    // 3. AUDITORÍA Y SEGURIDAD (APPEND-ONLY, SIN FILAS FICTICIAS)
    // =========================================================================
    public async Task RegistrarAuditoriaAsync(
        string usuarioNombre,
        string usuarioCorreo,
        string rol,
        string modulo,
        string accion,
        string registroAfectado,
        string resultado,
        string detalle,
        string? ipTerminal = null,
        int? registroId = null,
        int? usuarioId = null)
    {
        var ipLimpia = string.IsNullOrWhiteSpace(ipTerminal) ? "No disponible" : ipTerminal.Trim();
        var detalleSanitizado = SistemaMonitoreoService.SanitizarMensajeTecnico(detalle);

        var metadata = new AuditMetadataPayload
        {
            UsuarioNombre = string.IsNullOrWhiteSpace(usuarioNombre) ? "Sistema" : usuarioNombre.Trim(),
            UsuarioCorreo = string.IsNullOrWhiteSpace(usuarioCorreo) ? string.Empty : usuarioCorreo.Trim(),
            Rol = string.IsNullOrWhiteSpace(rol) ? "Sin rol" : rol.Trim(),
            Modulo = modulo,
            RegistroAfectado = registroAfectado,
            Resultado = resultado,
            Detalle = detalleSanitizado
        };

        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                if (!usuarioId.HasValue && !string.IsNullOrWhiteSpace(usuarioCorreo))
                {
                    var norm = usuarioCorreo.Trim().ToLowerInvariant();
                    usuarioId = await db.Usuarios
                        .Where(u => u.Correo.ToLower() == norm)
                        .Select(u => (int?)u.Id)
                        .FirstOrDefaultAsync();
                }

                db.Auditorias.Add(new AuditoriaEntity
                {
                    UsuarioId = usuarioId,
                    Accion = Truncar(accion, 100),
                    Tabla = Truncar(modulo, 100),
                    RegistroId = registroId,
                    Detalle = JsonSerializer.Serialize(metadata),
                    Ip = Truncar(ipLimpia, 50),
                    Fecha = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                return;
            }
            catch
            {
                // Si falla la escritura en BD, persistir en bitácora local sin perder trazabilidad
            }
        }

        lock (_syncLocal)
        {
            var lista = CargarAuditoriaLocalInternal();
            var nextId = lista.Count == 0 ? 1 : lista.Max(x => x.Id) + 1;
            lista.Insert(0, new AuditoriaEventoDto
            {
                Id = nextId,
                FechaUtc = DateTime.UtcNow,
                UsuarioId = usuarioId,
                UsuarioNombre = metadata.UsuarioNombre,
                UsuarioCorreo = metadata.UsuarioCorreo,
                Rol = metadata.Rol,
                Modulo = metadata.Modulo,
                Accion = accion,
                RegistroAfectado = metadata.RegistroAfectado,
                Resultado = metadata.Resultado,
                IpTerminal = ipLimpia,
                Detalle = metadata.Detalle,
                FuenteDatos = "Bitácora local (Data/auditoria_store.json)"
            });

            if (lista.Count > 500)
            {
                lista.RemoveRange(500, lista.Count - 500);
            }

            GuardarAuditoriaLocalInternal(lista);
        }
    }

    public async Task<(List<AuditoriaEventoDto> Eventos, string Fuente)> ConsultarAuditoriaAsync(FiltroAuditoria? filtro = null)
    {
        List<AuditoriaEventoDto> eventos;
        string fuente;

        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var rows = await db.Auditorias
                .AsNoTracking()
                .OrderByDescending(a => a.Fecha)
                .Take(400)
                .ToListAsync();

            var usuariosPorId = await db.Usuarios
                .AsNoTracking()
                .ToDictionaryAsync(u => u.Id, u => u);

            eventos = rows.Select(r => ParsearAuditoriaEntity(r, usuariosPorId)).ToList();
            fuente = "Supabase (public.auditoria)";
        }
        else
        {
            lock (_syncLocal)
            {
                eventos = CargarAuditoriaLocalInternal();
            }
            fuente = "Bitácora local (Data/auditoria_store.json · sin Supabase conectado)";
        }

        if (filtro != null)
        {
            var query = eventos.AsEnumerable();

            if (filtro.FechaDesde.HasValue)
            {
                var desde = filtro.FechaDesde.Value;
                query = query.Where(e => DateOnly.FromDateTime(e.FechaUtc.ToLocalTime()) >= desde);
            }

            if (filtro.FechaHasta.HasValue)
            {
                var hasta = filtro.FechaHasta.Value;
                query = query.Where(e => DateOnly.FromDateTime(e.FechaUtc.ToLocalTime()) <= hasta);
            }

            if (!string.IsNullOrWhiteSpace(filtro.Rol))
            {
                query = query.Where(e => e.Rol.Contains(filtro.Rol, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filtro.Modulo))
            {
                query = query.Where(e => e.Modulo.Equals(filtro.Modulo, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filtro.Accion))
            {
                query = query.Where(e => e.Accion.Contains(filtro.Accion, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filtro.Resultado))
            {
                query = query.Where(e => e.Resultado.Equals(filtro.Resultado, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filtro.TextoBusqueda))
            {
                var q = filtro.TextoBusqueda.Trim();
                query = query.Where(e =>
                    e.UsuarioNombre.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    e.UsuarioCorreo.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    e.Modulo.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    e.Accion.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    e.RegistroAfectado.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    e.Detalle.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            eventos = query.ToList();
        }

        return (eventos, fuente);
    }

    public byte[] ExportarAuditoriaCsv(IEnumerable<AuditoriaEventoDto> eventos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,FechaHoraLocal,Usuario,Correo,Rol,Modulo,Accion,RegistroAfectado,Resultado,IpTerminal,Detalle");

        foreach (var e in eventos)
        {
            var fecha = e.FechaUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            sb.AppendLine(string.Join(",",
                EscaparCsv(e.Id.ToString()),
                EscaparCsv(fecha),
                EscaparCsv(e.UsuarioNombre),
                EscaparCsv(e.UsuarioCorreo),
                EscaparCsv(e.Rol),
                EscaparCsv(e.Modulo),
                EscaparCsv(e.Accion),
                EscaparCsv(e.RegistroAfectado),
                EscaparCsv(e.Resultado),
                EscaparCsv(e.IpTerminal),
                EscaparCsv(e.Detalle)));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string EscaparCsv(string? valor)
    {
        var s = valor ?? string.Empty;
        // Neutralizar inyección de fórmulas en Excel/LibreOffice
        if (s.Length > 0 && (s[0] == '=' || s[0] == '+' || s[0] == '-' || s[0] == '@' || s[0] == '\t' || s[0] == '\r'))
        {
            s = "'" + s;
        }
        return $"\"{s.Replace("\"", "\"\"")}\"";
    }

    // =========================================================================
    // 4. CATÁLOGOS Y PARÁMETROS GENERALES REALES
    // =========================================================================
    public async Task<CatalogosSistemaStore> ObtenerCatalogosRealesAsync()
    {
        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            return new CatalogosSistemaStore
            {
                Cultivos = await db.Cultivos.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync(),
                GruposVulnerables = await db.GruposVulnerables.AsNoTracking().OrderBy(g => g.Nombre).ToListAsync(),
                TiposCuota = await db.TiposCuota.AsNoTracking().OrderBy(t => t.Nombre).ToListAsync(),
                TiposDocumento = await db.TiposDocumento.AsNoTracking().OrderBy(t => t.Nombre).ToListAsync()
            };
        }

        lock (_syncLocal)
        {
            return CargarCatalogosLocalInternal();
        }
    }

    public async Task<ResultadoOperacionAdmin> GuardarCultivoAsync(
        int? id, string nombre, bool activo,
        string actorNombre, string actorCorreo, string actorRol, string? ipTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length < 2)
            return Fallo("El nombre del cultivo es obligatorio.");

        var nom = nombre.Trim();
        if (nom.Length > 100) return Fallo("El nombre del cultivo no puede superar 100 caracteres.");

        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var dup = await db.Cultivos.AnyAsync(c => c.Nombre.ToLower() == nom.ToLower() && (!id.HasValue || c.Id != id.Value));
            if (dup) return Fallo($"El cultivo '{nom}' ya existe en el catálogo.");

            if (id.HasValue && id.Value > 0)
            {
                var ent = await db.Cultivos.FirstOrDefaultAsync(c => c.Id == id.Value);
                if (ent == null) return Fallo("Cultivo no encontrado.");
                var prev = $"{ent.Nombre} (Activo={ent.Activo})";
                ent.Nombre = nom;
                ent.Activo = activo;
                await db.SaveChangesAsync();

                await RegistrarAuditoriaAsync(actorNombre, actorCorreo, actorRol, "Catálogos y parámetros",
                    "Actualización en catálogo de cultivos", nom, "Autorizado", $"Antes: {prev} -> Ahora: {nom} (Activo={activo})", ipTerminal, ent.Id);
                return Ok($"Cultivo '{nom}' actualizado.");
            }
            else
            {
                var nuevo = new CultivoEntity { Nombre = nom, Activo = activo };
                db.Cultivos.Add(nuevo);
                await db.SaveChangesAsync();
                await RegistrarAuditoriaAsync(actorNombre, actorCorreo, actorRol, "Catálogos y parámetros",
                    "Alta en catálogo de cultivos", nom, "Autorizado", $"Cultivo registrado (Activo={activo})", ipTerminal, nuevo.Id);
                return Ok($"Cultivo '{nom}' agregado al catálogo.");
            }
        }

        lock (_syncLocal)
        {
            var store = CargarCatalogosLocalInternal();
            var dup = store.Cultivos.Any(c => c.Nombre.Equals(nom, StringComparison.OrdinalIgnoreCase) && (!id.HasValue || c.Id != id.Value));
            if (dup) return Fallo($"El cultivo '{nom}' ya existe en el catálogo.");

            if (id.HasValue && id.Value > 0)
            {
                var ent = store.Cultivos.FirstOrDefault(c => c.Id == id.Value);
                if (ent == null) return Fallo("Cultivo no encontrado.");
                var prev = $"{ent.Nombre} (Activo={ent.Activo})";
                ent.Nombre = nom;
                ent.Activo = activo;
                GuardarCatalogosLocalInternal(store);
                _ = RegistrarAuditoriaAsync(actorNombre, actorCorreo, actorRol, "Catálogos y parámetros",
                    "Actualización en catálogo de cultivos", nom, "Autorizado", $"Antes: {prev} -> Ahora: {nom} (Activo={activo})", ipTerminal, ent.Id);
                return Ok($"Cultivo '{nom}' actualizado.");
            }
            else
            {
                var nextId = store.Cultivos.Count == 0 ? 1 : store.Cultivos.Max(c => c.Id) + 1;
                store.Cultivos.Add(new CultivoEntity { Id = nextId, Nombre = nom, Activo = activo });
                GuardarCatalogosLocalInternal(store);
                _ = RegistrarAuditoriaAsync(actorNombre, actorCorreo, actorRol, "Catálogos y parámetros",
                    "Alta en catálogo de cultivos", nom, "Autorizado", $"Cultivo registrado (Activo={activo})", ipTerminal, nextId);
                return Ok($"Cultivo '{nom}' agregado al catálogo.");
            }
        }
    }

    public async Task<ResultadoOperacionAdmin> GuardarTipoCuotaAsync(
        int? id, string codigo, string nombre, bool permitePeriodicidad, bool activo,
        string actorNombre, string actorCorreo, string actorRol, string? ipTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
            return Fallo("Código y nombre del tipo de cuota son obligatorios.");

        var cod = codigo.Trim();
        var nom = nombre.Trim();

        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var dup = await db.TiposCuota.AnyAsync(t => t.Codigo.ToLower() == cod.ToLower() && (!id.HasValue || t.Id != id.Value));
            if (dup) return Fallo($"Ya existe un tipo de cuota con código '{cod}'.");

            if (id.HasValue && id.Value > 0)
            {
                var ent = await db.TiposCuota.FirstOrDefaultAsync(t => t.Id == id.Value);
                if (ent == null) return Fallo("Tipo de cuota no encontrado.");
                ent.Codigo = cod;
                ent.Nombre = nom;
                ent.PermitePeriodicidad = permitePeriodicidad;
                ent.Activo = activo;
                await db.SaveChangesAsync();
                await RegistrarAuditoriaAsync(actorNombre, actorCorreo, actorRol, "Catálogos y parámetros",
                    "Actualización en catálogo de tipos de cuota", nom, "Autorizado", $"Código={cod}, Periodicidad={permitePeriodicidad}, Activo={activo}", ipTerminal, ent.Id);
                return Ok($"Tipo de cuota '{nom}' actualizado.");
            }
            else
            {
                var nuevo = new TipoCuotaEntity { Codigo = cod, Nombre = nom, PermitePeriodicidad = permitePeriodicidad, Activo = activo };
                db.TiposCuota.Add(nuevo);
                await db.SaveChangesAsync();
                await RegistrarAuditoriaAsync(actorNombre, actorCorreo, actorRol, "Catálogos y parámetros",
                    "Alta en catálogo de tipos de cuota", nom, "Autorizado", $"Código={cod}, Periodicidad={permitePeriodicidad}, Activo={activo}", ipTerminal, nuevo.Id);
                return Ok($"Tipo de cuota '{nom}' creado.");
            }
        }

        lock (_syncLocal)
        {
            var store = CargarCatalogosLocalInternal();
            var dup = store.TiposCuota.Any(t => t.Codigo.Equals(cod, StringComparison.OrdinalIgnoreCase) && (!id.HasValue || t.Id != id.Value));
            if (dup) return Fallo($"Ya existe un tipo de cuota con código '{cod}'.");

            if (id.HasValue && id.Value > 0)
            {
                var ent = store.TiposCuota.FirstOrDefault(t => t.Id == id.Value);
                if (ent == null) return Fallo("Tipo de cuota no encontrado.");
                ent.Codigo = cod;
                ent.Nombre = nom;
                ent.PermitePeriodicidad = permitePeriodicidad;
                ent.Activo = activo;
                GuardarCatalogosLocalInternal(store);
                _ = RegistrarAuditoriaAsync(actorNombre, actorCorreo, actorRol, "Catálogos y parámetros",
                    "Actualización en catálogo de tipos de cuota", nom, "Autorizado", $"Código={cod}, Periodicidad={permitePeriodicidad}, Activo={activo}", ipTerminal, ent.Id);
                return Ok($"Tipo de cuota '{nom}' actualizado.");
            }
            else
            {
                var nextId = store.TiposCuota.Count == 0 ? 1 : store.TiposCuota.Max(t => t.Id) + 1;
                store.TiposCuota.Add(new TipoCuotaEntity { Id = nextId, Codigo = cod, Nombre = nom, PermitePeriodicidad = permitePeriodicidad, Activo = activo });
                GuardarCatalogosLocalInternal(store);
                _ = RegistrarAuditoriaAsync(actorNombre, actorCorreo, actorRol, "Catálogos y parámetros",
                    "Alta en catálogo de tipos de cuota", nom, "Autorizado", $"Código={cod}, Periodicidad={permitePeriodicidad}, Activo={activo}", ipTerminal, nextId);
                return Ok($"Tipo de cuota '{nom}' creado.");
            }
        }
    }

    public async Task<ResultadoOperacionAdmin> GuardarTipoDocumentoAsync(
        int? id, string codigo, string nombre,
        string actorNombre, string actorCorreo, string actorRol, string? ipTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
            return Fallo("Código y nombre del tipo de documento son obligatorios.");

        var cod = codigo.Trim();
        var nom = nombre.Trim();

        var dbFactory = _serviceProvider.GetService<IDbContextFactory<AsoagroDbContext>>();
        if (dbFactory != null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var dup = await db.TiposDocumento.AnyAsync(t => t.Codigo.ToLower() == cod.ToLower() && (!id.HasValue || t.Id != id.Value));
            if (dup) return Fallo($"Ya existe un tipo de documento con código '{cod}'.");

            if (id.HasValue && id.Value > 0)
            {
                var ent = await db.TiposDocumento.FirstOrDefaultAsync(t => t.Id == id.Value);
                if (ent == null) return Fallo("Tipo de documento no encontrado.");
                var prev = $"{ent.Codigo} - {ent.Nombre}";
                ent.Codigo = cod;
                ent.Nombre = nom;
                await db.SaveChangesAsync();
                await RegistrarAuditoriaAsync(actorNombre, actorCorreo, actorRol, "Catálogos y parámetros",
                    "Actualización en catálogo de tipos de documento", nom, "Autorizado", $"Antes: {prev} -> Ahora: {cod} - {nom}", ipTerminal, ent.Id);
                return Ok($"Tipo de documento '{nom}' actualizado.");
            }
            else
            {
                var nuevo = new TipoDocumento { Codigo = cod, Nombre = nom };
                db.TiposDocumento.Add(nuevo);
                await db.SaveChangesAsync();
                await RegistrarAuditoriaAsync(actorNombre, actorCorreo, actorRol, "Catálogos y parámetros",
                    "Alta en catálogo de tipos de documento", nom, "Autorizado", $"Código={cod}", ipTerminal, nuevo.Id);
                return Ok($"Tipo de documento '{nom}' registrado.");
            }
        }

        lock (_syncLocal)
        {
            var store = CargarCatalogosLocalInternal();
            var dup = store.TiposDocumento.Any(t => t.Codigo.Equals(cod, StringComparison.OrdinalIgnoreCase) && (!id.HasValue || t.Id != id.Value));
            if (dup) return Fallo($"Ya existe un tipo de documento con código '{cod}'.");

            if (id.HasValue && id.Value > 0)
            {
                var ent = store.TiposDocumento.FirstOrDefault(t => t.Id == id.Value);
                if (ent == null) return Fallo("Tipo de documento no encontrado.");
                ent.Codigo = cod;
                ent.Nombre = nom;
                GuardarCatalogosLocalInternal(store);
                _ = RegistrarAuditoriaAsync(actorNombre, actorCorreo, actorRol, "Catálogos y parámetros",
                    "Actualización en catálogo de tipos de documento", nom, "Autorizado", $"Código={cod}, Nombre={nom}", ipTerminal, ent.Id);
                return Ok($"Tipo de documento '{nom}' actualizado.");
            }
            else
            {
                var nextId = store.TiposDocumento.Count == 0 ? 1 : store.TiposDocumento.Max(t => t.Id) + 1;
                store.TiposDocumento.Add(new TipoDocumento { Id = nextId, Codigo = cod, Nombre = nom });
                GuardarCatalogosLocalInternal(store);
                _ = RegistrarAuditoriaAsync(actorNombre, actorCorreo, actorRol, "Catálogos y parámetros",
                    "Alta en catálogo de tipos de documento", nom, "Autorizado", $"Código={cod}", ipTerminal, nextId);
                return Ok($"Tipo de documento '{nom}' registrado.");
            }
        }
    }

    public List<ParametroSistemaItem> ObtenerParametrosSistema()
    {
        var meta = CargarParametrosLocal();
        return new List<ParametroSistemaItem>
        {
            new()
            {
                Clave = "Asoagro:Audit:DefaultUser",
                Nombre = "Autor predeterminado de procesos automáticos",
                Valor = _options.Value.Audit.DefaultUser,
                Descripcion = "Nombre asignado en creado_por / actualizado_por cuando un proceso interno no recibe usuario explícito (AsoagroOptions).",
                Editable = true,
                Origen = "AsoagroOptions / Data/parametros_sistema_store.json",
                UltimoCambioPor = meta.DefaultUserCambiadoPor,
                UltimoCambioUtc = meta.DefaultUserCambiadoUtc
            },
            new()
            {
                Clave = "Authentication:Cookie:LoginPath",
                Nombre = "Ruta de inicio de sesión",
                Valor = "/login",
                Descripcion = "Ruta configurada en AddCookie para redirección de sesiones no autenticadas.",
                Editable = false,
                Origen = "Program.cs"
            },
            new()
            {
                Clave = "Authentication:Cookie:AccessDeniedPath",
                Nombre = "Ruta de acceso denegado",
                Valor = "/access-denied",
                Descripcion = "Ruta configurada en AddCookie cuando el rol autenticado carece de permiso.",
                Editable = false,
                Origen = "Program.cs"
            },
            new()
            {
                Clave = "Workers:AlertasVencimiento",
                Nombre = "Frecuencia de revisión de alertas documentales",
                Valor = "Cada 1 hora",
                Descripcion = "Intervalo de ejecución del servicio AlertasVencimientoWorker en segundo plano.",
                Editable = false,
                Origen = "AlertasVencimientoWorker.cs"
            }
        };
    }

    public async Task<ResultadoOperacionAdmin> ActualizarParametroDefaultUserAsync(
        string nuevoValor,
        string actorNombre,
        string actorCorreo,
        string actorRol,
        string? ipTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(nuevoValor) || nuevoValor.Trim().Length < 2 || nuevoValor.Trim().Length > 80)
        {
            return Fallo("El valor debe tener entre 2 y 80 caracteres.");
        }

        var limpio = nuevoValor.Trim();
        var anterior = _options.Value.Audit.DefaultUser;
        _options.Value.Audit.DefaultUser = limpio;

        lock (_syncLocal)
        {
            var p = new ParametrosSistemaStore
            {
                DefaultUser = limpio,
                DefaultUserCambiadoPor = actorNombre,
                DefaultUserCambiadoUtc = DateTime.UtcNow
            };
            File.WriteAllText(_rutaParametrosLocal, JsonSerializer.Serialize(p, new JsonSerializerOptions { WriteIndented = true }));
        }

        await RegistrarAuditoriaAsync(
            actorNombre, actorCorreo, actorRol,
            "Catálogos y parámetros",
            "Cambio de parámetro del sistema",
            "Asoagro:Audit:DefaultUser",
            "Autorizado",
            $"Valor anterior: '{anterior}' -> Nuevo valor: '{limpio}'",
            ipTerminal);

        return Ok($"Parámetro 'Asoagro:Audit:DefaultUser' actualizado a '{limpio}'.");
    }

    // =========================================================================
    // HELPERS PRIVADOS Y ALMACENAMIENTO LOCAL
    // =========================================================================
    private static ResultadoOperacionAdmin Ok(string msg) => new() { Exitoso = true, Mensaje = msg };
    private static ResultadoOperacionAdmin Fallo(string msg) => new() { Exitoso = false, Mensaje = msg };

    private static string Truncar(string texto, int max) =>
        texto.Length <= max ? texto : texto[..max];

    private static string CalcularSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private UsuarioAdminDto MapearUsuarioDto(UsuarioEntity u, Dictionary<string, EstadoRecuperacionUsuario> recuperaciones) => new()
    {
        Id = u.Id,
        NombreCompleto = u.NombreCompleto,
        DocumentoId = u.DocumentoId,
        Correo = u.Correo,
        Telefono = u.Telefono,
        Rol = u.Rol,
        AsociacionId = u.AsociacionId,
        AsociacionNombre = u.Asociacion?.Nombre ?? "Federación FAAUA (Global)",
        Activo = u.Activo,
        TieneContrasenaLocal = !string.IsNullOrWhiteSpace(u.ContrasenaHash),
        RecuperacionPendiente = recuperaciones.ContainsKey(u.Correo.ToLowerInvariant()),
        UltimoAccesoUtc = u.UltimoAcceso,
        SesionRevocadaEnUtc = _monitoreo.ObtenerFechaRevocacion(u.Correo),
        CreadoEnUtc = u.CreadoEn
    };

    private static AuditoriaEventoDto ParsearAuditoriaEntity(AuditoriaEntity r, Dictionary<int, UsuarioEntity> usuarios)
    {
        var dto = new AuditoriaEventoDto
        {
            Id = r.Id,
            FechaUtc = r.Fecha,
            UsuarioId = r.UsuarioId,
            Accion = r.Accion,
            Modulo = r.Tabla ?? "Sistema",
            RegistroAfectado = r.RegistroId?.ToString() ?? "N/A",
            IpTerminal = string.IsNullOrWhiteSpace(r.Ip) ? "No disponible" : r.Ip,
            FuenteDatos = "Supabase (public.auditoria)"
        };

        if (r.UsuarioId.HasValue && usuarios.TryGetValue(r.UsuarioId.Value, out var u))
        {
            dto.UsuarioNombre = u.NombreCompleto;
            dto.UsuarioCorreo = u.Correo;
            dto.Rol = u.Rol;
        }

        if (!string.IsNullOrWhiteSpace(r.Detalle))
        {
            try
            {
                var meta = JsonSerializer.Deserialize<AuditMetadataPayload>(r.Detalle);
                if (meta != null && !string.IsNullOrWhiteSpace(meta.Modulo))
                {
                    if (!string.IsNullOrWhiteSpace(meta.UsuarioNombre)) dto.UsuarioNombre = meta.UsuarioNombre;
                    if (!string.IsNullOrWhiteSpace(meta.UsuarioCorreo)) dto.UsuarioCorreo = meta.UsuarioCorreo;
                    if (!string.IsNullOrWhiteSpace(meta.Rol)) dto.Rol = meta.Rol;
                    dto.Modulo = meta.Modulo;
                    if (!string.IsNullOrWhiteSpace(meta.RegistroAfectado)) dto.RegistroAfectado = meta.RegistroAfectado;
                    if (!string.IsNullOrWhiteSpace(meta.Resultado)) dto.Resultado = meta.Resultado;
                    dto.Detalle = meta.Detalle;
                    return dto;
                }
            }
            catch
            {
                // Detalle en texto plano previo
            }
            dto.Detalle = SistemaMonitoreoService.SanitizarMensajeTecnico(r.Detalle);
        }

        if (string.IsNullOrWhiteSpace(dto.UsuarioNombre)) dto.UsuarioNombre = "Sistema / No registrado";
        if (string.IsNullOrWhiteSpace(dto.Rol)) dto.Rol = "No especificado";
        return dto;
    }

    private void InicializarParametrosEnInicio()
    {
        try
        {
            var meta = CargarParametrosLocal();
            if (!string.IsNullOrWhiteSpace(meta.DefaultUser))
            {
                _options.Value.Audit.DefaultUser = meta.DefaultUser;
            }
        }
        catch { }
    }

    private ParametrosSistemaStore CargarParametrosLocal()
    {
        try
        {
            if (File.Exists(_rutaParametrosLocal))
            {
                var json = File.ReadAllText(_rutaParametrosLocal);
                return JsonSerializer.Deserialize<ParametrosSistemaStore>(json) ?? new ParametrosSistemaStore();
            }
        }
        catch { }
        return new ParametrosSistemaStore { DefaultUser = _options.Value.Audit.DefaultUser };
    }

    private Dictionary<string, EstadoRecuperacionUsuario> CargarRecuperacionesVigentes()
    {
        lock (_syncLocal) { return CargarRecuperacionesVigentesInternal(); }
    }

    private Dictionary<string, EstadoRecuperacionUsuario> CargarRecuperacionesVigentesInternal()
    {
        try
        {
            if (!File.Exists(_rutaRecuperacionLocal)) return new(StringComparer.OrdinalIgnoreCase);
            var dict = JsonSerializer.Deserialize<Dictionary<string, EstadoRecuperacionUsuario>>(File.ReadAllText(_rutaRecuperacionLocal))
                       ?? new(StringComparer.OrdinalIgnoreCase);
            return dict.Where(kv => kv.Value.ExpiraEnUtc > DateTime.UtcNow)
                       .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void GuardarRecuperacionesInternal(Dictionary<string, EstadoRecuperacionUsuario> dict)
    {
        try
        {
            File.WriteAllText(_rutaRecuperacionLocal, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private List<AuditoriaEventoDto> CargarAuditoriaLocalInternal()
    {
        try
        {
            if (!File.Exists(_rutaAuditoriaLocal)) return new();
            return JsonSerializer.Deserialize<List<AuditoriaEventoDto>>(File.ReadAllText(_rutaAuditoriaLocal)) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private void GuardarAuditoriaLocalInternal(List<AuditoriaEventoDto> lista)
    {
        try
        {
            File.WriteAllText(_rutaAuditoriaLocal, JsonSerializer.Serialize(lista, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private UsuariosPermisosStore CargarStoreUsuariosLocal()
    {
        lock (_syncLocal) { return CargarStoreUsuariosLocalInternal(); }
    }

    private UsuariosPermisosStore CargarStoreUsuariosLocalInternal()
    {
        try
        {
            if (File.Exists(_rutaUsuariosLocal))
            {
                var store = JsonSerializer.Deserialize<UsuariosPermisosStore>(File.ReadAllText(_rutaUsuariosLocal));
                if (store != null && store.Usuarios.Count > 0)
                {
                    if (store.Permisos.Count == 0)
                    {
                        store.Permisos = GenerarMatrizMinimoPrivilegio();
                    }
                    return store;
                }
            }
        }
        catch { }

        // Sembrar únicamente las cuentas institucionales reales definidas en AuthController / Program.cs
        var inicial = new UsuariosPermisosStore
        {
            Usuarios = new List<UsuarioEntity>
            {
                new() { Id = 1, NombreCompleto = "Administrador del Sistema ASOAGRO", DocumentoId = "900000001", Correo = "admin@asoagro-faaua.org", Rol = "Administrador", Activo = true, CreadoEn = DateTime.UtcNow },
                new() { Id = 2, NombreCompleto = "Karen Argumedo", DocumentoId = "100000002", Correo = "karen.argumedoco@amigo.edu.co", Rol = "AdministradorPdv", Activo = true, CreadoEn = DateTime.UtcNow },
                new() { Id = 3, NombreCompleto = "Juan Padilla", DocumentoId = "100000003", Correo = "juan.padillamo@amigo.edu.co", Rol = "Tesorero", Activo = true, CreadoEn = DateTime.UtcNow },
                new() { Id = 4, NombreCompleto = "Manuela Andrea Mosquera", DocumentoId = "100000004", Correo = "manuela.mosqueraar@amigo.edu.co", Rol = "Secretaria", Activo = true, CreadoEn = DateTime.UtcNow },
                new() { Id = 5, NombreCompleto = "Manuela Mosquera", DocumentoId = "100000005", Correo = "manumosque743@gmail.com", Rol = "Presidenta", Activo = true, CreadoEn = DateTime.UtcNow },
                new() { Id = 6, NombreCompleto = "Secretaría General FAAUA", DocumentoId = "100000006", Correo = "secretaria@asoagro-faaua.org", Rol = "Secretaria", Activo = true, CreadoEn = DateTime.UtcNow },
                new() { Id = 7, NombreCompleto = "Tesorería FAAUA", DocumentoId = "100000007", Correo = "tesoreria@asoagro-faaua.org", Rol = "Tesorero", Activo = true, CreadoEn = DateTime.UtcNow },
                new() { Id = 8, NombreCompleto = "Administrador Punto de Venta", DocumentoId = "100000008", Correo = "pdv@asoagro-faaua.org", Rol = "AdministradorPdv", Activo = true, CreadoEn = DateTime.UtcNow }
            },
            Permisos = GenerarMatrizMinimoPrivilegio()
        };

        GuardarStoreUsuariosLocalInternal(inicial);
        return inicial;
    }

    private void GuardarStoreUsuariosLocalInternal(UsuariosPermisosStore store)
    {
        try
        {
            File.WriteAllText(_rutaUsuariosLocal, JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private CatalogosSistemaStore CargarCatalogosLocalInternal()
    {
        try
        {
            if (File.Exists(_rutaCatalogosLocal))
            {
                var store = JsonSerializer.Deserialize<CatalogosSistemaStore>(File.ReadAllText(_rutaCatalogosLocal));
                if (store != null && (store.Cultivos.Count > 0 || store.TiposDocumento.Count > 0))
                {
                    return store;
                }
            }
        }
        catch { }

        var inicial = new CatalogosSistemaStore
        {
            Cultivos = new List<CultivoEntity>
            {
                new() { Id = 1, Nombre = "Platano", Activo = true },
                new() { Id = 2, Nombre = "Banano", Activo = true },
                new() { Id = 3, Nombre = "Cacao", Activo = true },
                new() { Id = 4, Nombre = "Yuca", Activo = true },
                new() { Id = 5, Nombre = "Maiz", Activo = true },
                new() { Id = 6, Nombre = "Maracuya", Activo = true },
                new() { Id = 7, Nombre = "Ganaderia", Activo = true }
            },
            GruposVulnerables = AsociadoModel.CatalogoGruposVulnerables
                .Select((g, i) => new GrupoVulnerableEntity { Id = i + 1, Nombre = g.Nombre, Activo = true })
                .ToList(),
            TiposCuota = new List<TipoCuotaEntity>
            {
                new() { Id = 1, Codigo = "Sostenimiento", Nombre = "Cuota de Sostenimiento", PermitePeriodicidad = true, Activo = true },
                new() { Id = 2, Codigo = "Afiliacion", Nombre = "Cuota de Afiliación", PermitePeriodicidad = false, Activo = true },
                new() { Id = 3, Codigo = "Extraordinaria", Nombre = "Cuota Extraordinaria", PermitePeriodicidad = false, Activo = true }
            },
            TiposDocumento = new List<TipoDocumento>
            {
                new() { Id = 1, Codigo = "CertificadoFinca", Nombre = "Certificado de Tradición" },
                new() { Id = 2, Codigo = "CedulaCiudadania", Nombre = "Cédula de Ciudadanía" },
                new() { Id = 3, Codigo = "RegistroBPA", Nombre = "Buenas Prácticas Agrícolas (BPA)" },
                new() { Id = 4, Codigo = "AvalTecnico", Nombre = "Aval de Asistencia Técnica" },
                new() { Id = 5, Codigo = "RegistroICA", Nombre = "Registro ICA" },
                new() { Id = 6, Codigo = "UsoSuelo", Nombre = "Uso de Suelo" },
                new() { Id = 7, Codigo = "RegistroRUT", Nombre = "Registro RUT" }
            }
        };

        GuardarCatalogosLocalInternal(inicial);
        return inicial;
    }

    private void GuardarCatalogosLocalInternal(CatalogosSistemaStore store)
    {
        try
        {
            File.WriteAllText(_rutaCatalogosLocal, JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public static List<PermisoModuloEntity> GenerarMatrizMinimoPrivilegio()
    {
        var lista = new List<PermisoModuloEntity>();
        int id = 1;

        void Add(string rol, string mod, bool ver, bool crear, bool editar, bool eliminar) =>
            lista.Add(new PermisoModuloEntity
            {
                Id = id++,
                Rol = rol,
                Modulo = mod,
                PuedeVer = ver,
                PuedeCrear = crear,
                PuedeEditar = editar,
                PuedeEliminar = eliminar
            });

        // 1. Administrador del Sistema: acceso global técnico y supervisión de todos los paneles (auditoría solo lectura)
        Add("Administrador", "Supervisión del sistema", true, false, false, false);
        Add("Administrador", "Usuarios, roles y permisos", true, true, true, true);
        Add("Administrador", "Auditoría y seguridad", true, false, false, false);
        Add("Administrador", "Catálogos y parámetros", true, true, true, false);
        Add("Administrador", "Estado de datos e integraciones", true, false, false, false);
        Add("Administrador", "Actas y Estatutos", true, true, true, false);
        Add("Administrador", "Secretaría y Padrón", true, true, true, false);
        Add("Administrador", "Tesorería y Finanzas", true, true, true, false);
        Add("Administrador", "Punto de Venta", true, true, true, false);

        // 2. Presidenta / Presidencia: supervisión institucional y actas/estatutos (sin tareas técnicas de admin)
        Add("Presidenta", "Supervisión del sistema", false, false, false, false);
        Add("Presidenta", "Usuarios, roles y permisos", false, false, false, false);
        Add("Presidenta", "Auditoría y seguridad", false, false, false, false);
        Add("Presidenta", "Actas y Estatutos", true, true, true, false);
        Add("Presidenta", "Secretaría y Padrón", true, false, false, false);
        Add("Presidenta", "Tesorería y Finanzas", true, false, false, false);
        Add("Presidenta", "Punto de Venta", true, false, false, false);

        // 3. Secretaría: asociaciones, productores, expedientes y seguimiento
        Add("Secretaria", "Secretaría y Padrón", true, true, true, false);
        Add("Secretaria", "Expediente Documental", true, true, true, false);
        Add("Secretaria", "Seguimiento de Asociados", true, true, false, false);
        Add("Secretaria", "Usuarios, roles y permisos", false, false, false, false);
        Add("Secretaria", "Tesorería y Finanzas", false, false, false, false);
        Add("Secretaria", "Punto de Venta", false, false, false, false);

        // 4. Tesorero: gestión financiera y consulta PDV
        Add("Tesorero", "Tesorería y Finanzas", true, true, true, false);
        Add("Tesorero", "Punto de Venta", true, false, false, false);
        Add("Tesorero", "Secretaría y Padrón", false, false, false, false);
        Add("Tesorero", "Usuarios, roles y permisos", false, false, false, false);

        // 5. Administrador Punto de Venta: operación comercial PDV
        Add("AdministradorPdv", "Punto de Venta", true, true, true, false);
        Add("AdministradorPdv", "Secretaría y Padrón", false, false, false, false);
        Add("AdministradorPdv", "Tesorería y Finanzas", false, false, false, false);
        Add("AdministradorPdv", "Usuarios, roles y permisos", false, false, false, false);

        return lista;
    }

    private sealed class AuditMetadataPayload
    {
        public string UsuarioNombre { get; set; } = string.Empty;
        public string UsuarioCorreo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string RegistroAfectado { get; set; } = string.Empty;
        public string Resultado { get; set; } = "Autorizado";
        public string Detalle { get; set; } = string.Empty;
    }

    private sealed class UsuariosPermisosStore
    {
        public List<UsuarioEntity> Usuarios { get; set; } = new();
        public List<PermisoModuloEntity> Permisos { get; set; } = new();
    }

    private AdminBootstrapEstado CargarBootstrapEstadoInternal()
    {
        try
        {
            if (File.Exists(_rutaBootstrapEstadoLocal))
            {
                return JsonSerializer.Deserialize<AdminBootstrapEstado>(File.ReadAllText(_rutaBootstrapEstadoLocal)) ?? new AdminBootstrapEstado();
            }
        }
        catch { }
        return new AdminBootstrapEstado();
    }

    private void GuardarBootstrapEstadoInternal(AdminBootstrapEstado estado)
    {
        try
        {
            File.WriteAllText(_rutaBootstrapEstadoLocal, JsonSerializer.Serialize(estado, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private void MarcarRolesEquipoSincronizadosInternal()
    {
        var st = CargarBootstrapEstadoInternal();
        st.RolesEquipoV2Sincronizados = true;
        GuardarBootstrapEstadoInternal(st);
    }

    private void MarcarRequisitoCambioContrasenaInternal(string normEmail)
    {
        var st = CargarBootstrapEstadoInternal();
        if (!st.CuentasPendientesCambioClave.Contains(normEmail))
        {
            st.CuentasPendientesCambioClave.Add(normEmail);
            GuardarBootstrapEstadoInternal(st);
        }
    }

    private void LimpiarRequisitoCambioContrasenaInternal(string normEmail)
    {
        var st = CargarBootstrapEstadoInternal();
        if (st.CuentasPendientesCambioClave.RemoveAll(c => c.Equals(normEmail, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            GuardarBootstrapEstadoInternal(st);
        }
    }

    private sealed class AdminBootstrapEstado
    {
        public bool RolesEquipoV2Sincronizados { get; set; }
        public List<string> CuentasPendientesCambioClave { get; set; } = new();
    }

    private sealed class ParametrosSistemaStore
    {
        public string DefaultUser { get; set; } = "Sistema";
        public string? DefaultUserCambiadoPor { get; set; }
        public DateTime? DefaultUserCambiadoUtc { get; set; }
    }
}

public sealed class CatalogosSistemaStore
{
    public List<CultivoEntity> Cultivos { get; set; } = new();
    public List<GrupoVulnerableEntity> GruposVulnerables { get; set; } = new();
    public List<TipoCuotaEntity> TiposCuota { get; set; } = new();
    public List<TipoDocumento> TiposDocumento { get; set; } = new();
}

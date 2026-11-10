using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Asoagro.Models;

namespace Asoagro.Services;

/// <summary>
/// Gestiona y persiste los avisos y notificaciones operacionales e institucionales del sistema ASOAGRO.
/// Se registra como Singleton: todas las pantallas conectadas comparten el servicio y reciben eventos en tiempo real.
/// Persiste las notificaciones y los estados de lectura por usuario en disco para conservarlos tras reinicios y recargas.
/// </summary>
public class NotificacionService
{
    private const int MaximoGuardadas = 300;
    private readonly string _rutaArchivoAlmacenamiento;

    private readonly object _sync = new();
    private readonly List<Notificacion> _notificaciones = new();
    private readonly HashSet<string> _clavesUsadas = new(StringComparer.OrdinalIgnoreCase);

    // Clave: identificador/correo del usuario. Valor: IDs de las notificaciones que ya leyó.
    private readonly Dictionary<string, HashSet<Guid>> _leidasPorUsuario = new(StringComparer.OrdinalIgnoreCase);

    // Evento para notificar a las pantallas conectadas en tiempo real
    public event Action<Notificacion>? NotificacionPublicada;

    public NotificacionService()
    {
        // Ruta de almacenamiento persistente en el directorio Data del proyecto
        var baseDir = AppContext.BaseDirectory;
        var projectDataDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        
        var dirFinal = Directory.Exists(projectDataDir) 
            ? projectDataDir 
            : Path.Combine(baseDir, "Data");

        try
        {
            Directory.CreateDirectory(dirFinal);
        }
        catch
        {
            dirFinal = AppDomain.CurrentDomain.BaseDirectory;
        }

        _rutaArchivoAlmacenamiento = Path.Combine(dirFinal, "notificaciones_store.json");
        CargarDesdeAlmacenamiento();
    }

    /// <summary>
    /// Publica una nueva notificación. Devuelve false si ya existía (misma ClaveUnica o duplicado reciente).
    /// </summary>
    public bool Publicar(Notificacion notificacion)
    {
        lock (_sync)
        {
            // 1. Validación de clave única explícita
            if (!string.IsNullOrWhiteSpace(notificacion.ClaveUnica))
            {
                if (!_clavesUsadas.Add(notificacion.ClaveUnica))
                {
                    return false;
                }
            }
            else
            {
                // 2. Prevención de duplicados recientes por contenido idéntico (últimos 30 minutos)
                var esDuplicada = _notificaciones.Any(n =>
                    string.Equals(n.Titulo, notificacion.Titulo, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(n.Descripcion, notificacion.Descripcion, StringComparison.OrdinalIgnoreCase) &&
                    (notificacion.FechaCreacion - n.FechaCreacion).TotalMinutes < 30);

                if (esDuplicada)
                {
                    return false;
                }
            }

            _notificaciones.Insert(0, notificacion);

            if (_notificaciones.Count > MaximoGuardadas)
            {
                _notificaciones.RemoveRange(MaximoGuardadas, _notificaciones.Count - MaximoGuardadas);
            }

            GuardarEnAlmacenamientoDesacoplado();
        }

        // Se invoca el evento fuera del candado para no bloquear hilos UI
        try
        {
            NotificacionPublicada?.Invoke(notificacion);
        }
        catch
        {
            // Protección ante suscriptores desconectados
        }

        return true;
    }

    /// <summary>
    /// Obtiene las notificaciones visibles para los roles indicados, ordenadas de la más reciente a la más antigua.
    /// </summary>
    public IReadOnlyList<Notificacion> ObtenerVisiblesPara(IReadOnlyCollection<string> roles)
    {
        lock (_sync)
        {
            return _notificaciones
                .Where(n => EsVisible(n, roles))
                .OrderByDescending(n => n.FechaCreacion)
                .ToList();
        }
    }

    /// <summary>
    /// Cuenta las notificaciones pendientes de leer para el usuario y roles dados.
    /// </summary>
    public int ContarNoLeidas(string usuario, IReadOnlyCollection<string> roles)
    {
        if (string.IsNullOrWhiteSpace(usuario)) return 0;

        lock (_sync)
        {
            var leidas = LeidasDe(usuario);
            return _notificaciones.Count(n => EsVisible(n, roles) && !leidas.Contains(n.Id));
        }
    }

    /// <summary>
    /// Verifica si un aviso específico ya fue marcado como leído por el usuario.
    /// </summary>
    public bool EsLeida(string usuario, Guid id)
    {
        if (string.IsNullOrWhiteSpace(usuario)) return false;

        lock (_sync)
        {
            return LeidasDe(usuario).Contains(id);
        }
    }

    /// <summary>
    /// Marca una notificación como leída para el usuario y persiste el estado.
    /// </summary>
    public void MarcarComoLeida(string usuario, Guid id)
    {
        if (string.IsNullOrWhiteSpace(usuario)) return;

        lock (_sync)
        {
            var agregada = LeidasDe(usuario).Add(id);
            if (agregada)
            {
                GuardarEnAlmacenamientoDesacoplado();
            }
        }
    }

    /// <summary>
    /// Marca una notificación como no leída para el usuario y persiste el estado.
    /// </summary>
    public void MarcarComoNoLeida(string usuario, Guid id)
    {
        if (string.IsNullOrWhiteSpace(usuario)) return;

        lock (_sync)
        {
            var removida = LeidasDe(usuario).Remove(id);
            if (removida)
            {
                GuardarEnAlmacenamientoDesacoplado();
            }
        }
    }

    /// <summary>
    /// Alterna el estado de lectura de una notificación para el usuario.
    /// </summary>
    public bool AlternarLeida(string usuario, Guid id)
    {
        if (string.IsNullOrWhiteSpace(usuario)) return false;

        lock (_sync)
        {
            var leidas = LeidasDe(usuario);
            bool ahoraLeida;
            if (leidas.Contains(id))
            {
                leidas.Remove(id);
                ahoraLeida = false;
            }
            else
            {
                leidas.Add(id);
                ahoraLeida = true;
            }

            GuardarEnAlmacenamientoDesacoplado();
            return ahoraLeida;
        }
    }

    /// <summary>
    /// Marca todas las notificaciones visibles para los roles del usuario como leídas.
    /// </summary>
    public void MarcarTodasComoLeidas(string usuario, IReadOnlyCollection<string> roles)
    {
        if (string.IsNullOrWhiteSpace(usuario)) return;

        lock (_sync)
        {
            var leidas = LeidasDe(usuario);
            var huboCambios = false;

            foreach (var notificacion in _notificaciones.Where(n => EsVisible(n, roles)))
            {
                if (leidas.Add(notificacion.Id))
                {
                    huboCambios = true;
                }
            }

            if (huboCambios)
            {
                GuardarEnAlmacenamientoDesacoplado();
            }
        }
    }

    /// <summary>
    /// Determina si una notificación debe ser visible para un conjunto de roles.
    /// Si RolesDestino está vacía, es institucional y visible para cualquier usuario autenticado.
    /// </summary>
    public static bool EsVisible(Notificacion notificacion, IReadOnlyCollection<string> roles)
    {
        if (notificacion.RolesDestino == null || notificacion.RolesDestino.Count == 0) return true;
        if (roles == null || roles.Count == 0) return false;

        return notificacion.RolesDestino.Any(rol =>
            roles.Any(r => string.Equals(r, rol, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Determina si el usuario tiene permiso para navegar al enlace de la notificación.
    /// Si el enlace está vacío o apunta a "#" o "/", se considera no navegable.
    /// Valida que el rol tenga acceso al módulo de destino.
    /// </summary>
    public static bool PuedeAbrirEnlace(Notificacion notificacion, IReadOnlyCollection<string> roles)
    {
        if (string.IsNullOrWhiteSpace(notificacion.Enlace)) return false;
        var enlaceLimpio = notificacion.Enlace.Trim();
        if (enlaceLimpio == "#" || enlaceLimpio == "/" || enlaceLimpio.Equals("/home", StringComparison.OrdinalIgnoreCase)) return false;

        if (roles == null || roles.Count == 0) return false;

        // Roles con acceso global
        if (roles.Any(r => string.Equals(r, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(r, "Presidenta", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(r, "Presidente", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Si se definieron roles específicos para el enlace
        if (notificacion.RolesEnlace != null && notificacion.RolesEnlace.Count > 0)
        {
            return notificacion.RolesEnlace.Any(rol =>
                roles.Any(r => string.Equals(r, rol, StringComparison.OrdinalIgnoreCase)));
        }

        // Límites según el rol operativo del usuario:
        var esSoloPdv = roles.All(r => string.Equals(r, "AdministradorPdv", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(r, "PuntoVenta", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(r, "Vendedor", StringComparison.OrdinalIgnoreCase));

        if (esSoloPdv)
        {
            return enlaceLimpio.StartsWith("/punto-de-venta", StringComparison.OrdinalIgnoreCase) ||
                   enlaceLimpio.StartsWith("punto-de-venta", StringComparison.OrdinalIgnoreCase);
        }

        var esSoloTesorero = roles.All(r => string.Equals(r, "Tesorero", StringComparison.OrdinalIgnoreCase) ||
                                           string.Equals(r, "Tesorera", StringComparison.OrdinalIgnoreCase));

        if (esSoloTesorero)
        {
            return enlaceLimpio.StartsWith("/gestion-financiera", StringComparison.OrdinalIgnoreCase) ||
                   enlaceLimpio.StartsWith("gestion-financiera", StringComparison.OrdinalIgnoreCase) ||
                   enlaceLimpio.StartsWith("/punto-de-venta", StringComparison.OrdinalIgnoreCase) ||
                   enlaceLimpio.StartsWith("punto-de-venta", StringComparison.OrdinalIgnoreCase);
        }

        var esSoloSecretaria = roles.All(r => string.Equals(r, "Secretaria", StringComparison.OrdinalIgnoreCase) ||
                                              string.Equals(r, "Secretaría", StringComparison.OrdinalIgnoreCase));

        if (esSoloSecretaria)
        {
            return !enlaceLimpio.StartsWith("/gestion-financiera", StringComparison.OrdinalIgnoreCase) &&
                   !enlaceLimpio.StartsWith("gestion-financiera", StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    private HashSet<Guid> LeidasDe(string usuario)
    {
        var clave = usuario.Trim().ToLowerInvariant();
        if (!_leidasPorUsuario.TryGetValue(clave, out var leidas))
        {
            leidas = new HashSet<Guid>();
            _leidasPorUsuario[clave] = leidas;
        }

        return leidas;
    }

    #region Persistencia en Almacenamiento

    private class EstadoPersistidoNotificaciones
    {
        public List<Notificacion> Notificaciones { get; set; } = new();
        public List<string> ClavesUsadas { get; set; } = new();
        public Dictionary<string, List<Guid>> LeidasPorUsuario { get; set; } = new();
    }

    private void CargarDesdeAlmacenamiento()
    {
        try
        {
            if (!File.Exists(_rutaArchivoAlmacenamiento)) return;

            var json = File.ReadAllText(_rutaArchivoAlmacenamiento);
            if (string.IsNullOrWhiteSpace(json)) return;

            var estado = JsonSerializer.Deserialize<EstadoPersistidoNotificaciones>(json);
            if (estado == null) return;

            lock (_sync)
            {
                _notificaciones.Clear();
                if (estado.Notificaciones != null)
                {
                    _notificaciones.AddRange(estado.Notificaciones);
                }

                _clavesUsadas.Clear();
                if (estado.ClavesUsadas != null)
                {
                    foreach (var c in estado.ClavesUsadas)
                    {
                        if (!string.IsNullOrWhiteSpace(c)) _clavesUsadas.Add(c);
                    }
                }

                _leidasPorUsuario.Clear();
                if (estado.LeidasPorUsuario != null)
                {
                    foreach (var kvp in estado.LeidasPorUsuario)
                    {
                        var clave = kvp.Key.Trim().ToLowerInvariant();
                        _leidasPorUsuario[clave] = new HashSet<Guid>(kvp.Value);
                    }
                }
            }
        }
        catch
        {
            // Si el archivo está dañado o inaccesible, se inicia en memoria sin interrumpir el arranque
        }
    }

    private void GuardarEnAlmacenamientoDesacoplado()
    {
        // Se ejecuta en segundo plano para no demorar la respuesta de peticiones
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                EstadoPersistidoNotificaciones estado;
                lock (_sync)
                {
                    estado = new EstadoPersistidoNotificaciones
                    {
                        Notificaciones = _notificaciones.Take(MaximoGuardadas).ToList(),
                        ClavesUsadas = _clavesUsadas.Take(1000).ToList(),
                        LeidasPorUsuario = _leidasPorUsuario.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.ToList())
                    };
                }

                var json = JsonSerializer.Serialize(estado, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var dir = Path.GetDirectoryName(_rutaArchivoAlmacenamiento);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var rutaTemp = _rutaArchivoAlmacenamiento + ".tmp";
                File.WriteAllText(rutaTemp, json);
                File.Move(rutaTemp, _rutaArchivoAlmacenamiento, overwrite: true);
            }
            catch
            {
                // Silencioso ante concurrencia de archivo
            }
        });
    }

    #endregion
}

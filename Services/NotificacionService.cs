using Asoagro.Models;

namespace Asoagro.Services;

// Guarda los avisos del sistema y los entrega a todas las pantallas conectadas.
// Se registra como Singleton: todos los navegadores conectados a este servidor comparten la misma lista.
public class NotificacionService
{
    private const int MaximoGuardadas = 200;

    private readonly object _sync = new();
    private readonly List<Notificacion> _notificaciones = new();
    private readonly HashSet<string> _clavesUsadas = new(StringComparer.OrdinalIgnoreCase);

    // Clave: correo del usuario. Valor: ids de los avisos que ya leyó.
    private readonly Dictionary<string, HashSet<Guid>> _leidasPorUsuario = new(StringComparer.OrdinalIgnoreCase);

    // Cada pestaña abierta se suscribe aquí para actualizar su campana y mostrar el aviso emergente.
    public event Action<Notificacion>? NotificacionPublicada;

    // Devuelve false si el aviso ya existía (misma ClaveUnica), para no repetirlo.
    public bool Publicar(Notificacion notificacion)
    {
        lock (_sync)
        {
            if (notificacion.ClaveUnica is not null && !_clavesUsadas.Add(notificacion.ClaveUnica))
            {
                return false;
            }

            _notificaciones.Insert(0, notificacion);

            if (_notificaciones.Count > MaximoGuardadas)
            {
                _notificaciones.RemoveRange(MaximoGuardadas, _notificaciones.Count - MaximoGuardadas);
            }
        }

        // Se avisa fuera del candado para no bloquear a las demás pantallas
        NotificacionPublicada?.Invoke(notificacion);
        return true;
    }

    public IReadOnlyList<Notificacion> ObtenerVisiblesPara(IReadOnlyCollection<string> roles)
    {
        lock (_sync)
        {
            return _notificaciones.Where(n => EsVisible(n, roles)).ToList();
        }
    }

    public int ContarNoLeidas(string usuario, IReadOnlyCollection<string> roles)
    {
        lock (_sync)
        {
            var leidas = LeidasDe(usuario);
            return _notificaciones.Count(n => EsVisible(n, roles) && !leidas.Contains(n.Id));
        }
    }

    public bool EsLeida(string usuario, Guid id)
    {
        lock (_sync)
        {
            return LeidasDe(usuario).Contains(id);
        }
    }

    public void MarcarComoLeida(string usuario, Guid id)
    {
        lock (_sync)
        {
            LeidasDe(usuario).Add(id);
        }
    }

    public void MarcarTodasComoLeidas(string usuario, IReadOnlyCollection<string> roles)
    {
        lock (_sync)
        {
            var leidas = LeidasDe(usuario);

            foreach (var notificacion in _notificaciones.Where(n => EsVisible(n, roles)))
            {
                leidas.Add(notificacion.Id);
            }
        }
    }

    // Sin roles de destino, el aviso lo ve cualquier usuario que inició sesión
       public static bool EsVisible(Notificacion notificacion, IReadOnlyCollection<string> roles)
    {
        if (notificacion.RolesDestino.Count == 0) return true;

        return notificacion.RolesDestino.Any(rol =>
            roles.Any(r => string.Equals(r, rol, StringComparison.OrdinalIgnoreCase)));
    }

        // El aviso siempre se ve, pero su enlace solo se puede abrir si el usuario tiene uno de los roles de RolesEnlace.
    public static bool PuedeAbrirEnlace(Notificacion notificacion, IReadOnlyCollection<string> roles)
    {
        if (string.IsNullOrWhiteSpace(notificacion.Enlace)) return false;
        if (notificacion.RolesEnlace.Count == 0) return true;

        return notificacion.RolesEnlace.Any(rol =>
            roles.Any(r => string.Equals(r, rol, StringComparison.OrdinalIgnoreCase)));
    }

    // Debe llamarse siempre dentro del candado
    private HashSet<Guid> LeidasDe(string usuario)
    {
        if (!_leidasPorUsuario.TryGetValue(usuario, out var leidas))
        {
            leidas = new HashSet<Guid>();
            _leidasPorUsuario[usuario] = leidas;
        }

        return leidas;
    }
}
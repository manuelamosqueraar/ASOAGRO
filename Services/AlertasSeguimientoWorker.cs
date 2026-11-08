using Asoagro.Models;

namespace Asoagro.Services;

public class AlertasSeguimientoWorker : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);
    private static readonly string[] RolesDirectivos = ["Secretaria", "Secretaría", "Administrador", "Admin", "Presidenta", "Presidente"];

    private readonly ISeguimientoAsociadoService _seguimientos;
    private readonly NotificacionService _notificaciones;
    private readonly ILogger<AlertasSeguimientoWorker> _logger;

    public AlertasSeguimientoWorker(
        ISeguimientoAsociadoService seguimientos,
        NotificacionService notificaciones,
        ILogger<AlertasSeguimientoWorker> logger)
    {
        _seguimientos = seguimientos;
        _notificaciones = notificaciones;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            RevisarVencimientos();

            using var temporizador = new PeriodicTimer(Intervalo);
            while (await temporizador.WaitForNextTickAsync(stoppingToken))
            {
                RevisarVencimientos();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Cierre normal del servicio en segundo plano.
        }
    }

    private void RevisarVencimientos()
    {
        try
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var nuevos = 0;
            var ultimasGestiones = _seguimientos.ObtenerTodos()
                .GroupBy(seguimiento => seguimiento.AsociadoId)
                .Select(grupo => grupo
                    .OrderByDescending(seguimiento => seguimiento.FechaGestion)
                    .ThenByDescending(seguimiento => seguimiento.Id)
                    .First());

            foreach (var gestion in ultimasGestiones)
            {
                if (gestion.ProximaGestion is not DateOnly proxima)
                {
                    continue;
                }

                var diasRestantes = proxima.DayNumber - hoy.DayNumber;
                var umbral = ObtenerUmbral(diasRestantes);
                if (umbral is null || gestion.Asociado is null)
                {
                    continue;
                }

                var asociado = gestion.Asociado;
                var creado = _notificaciones.Publicar(new Notificacion
                {
                    Titulo = CrearTitulo(asociado.NombreCompleto, diasRestantes),
                    Descripcion = $"{NombreContacto(asociado)} · próxima gestión: {proxima:dd/MM/yyyy}.",
                    Etiqueta = diasRestantes < 0 ? "Vencido" : "Seguimiento",
                    Enlace = "/seguimiento-asociados",
                    Icono = "alerta",
                    Categoria = CategoriaNotificacion.Seguimiento,
                    Nivel = diasRestantes <= 1 ? NivelNotificacion.Urgente : NivelNotificacion.Advertencia,
                    RolesDestino = RolesDirectivos,
                    RolesEnlace = RolesDirectivos,
                    ClaveUnica = $"seguimiento|{gestion.AsociadoId}|{proxima:yyyy-MM-dd}|{umbral}"
                });

                if (creado)
                {
                    nuevos++;
                }
            }

            if (nuevos > 0)
            {
                _logger.LogInformation("Se crearon {Cantidad} avisos de próximos contactos y seguimientos vencidos.", nuevos);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible revisar los vencimientos de seguimiento de asociados.");
        }
    }

    private static int? ObtenerUmbral(int diasRestantes) => diasRestantes switch
    {
        < 0 => -1,
        0 => 0,
        1 => 1,
        <= 7 => 7,
        _ => null
    };

    private static string CrearTitulo(string nombre, int diasRestantes) => diasRestantes switch
    {
        < 0 => $"Contacto vencido: {nombre}",
        0 => $"Contacto programado para hoy: {nombre}",
        1 => $"Contacto programado para mañana: {nombre}",
        _ => $"Contacto programado en {diasRestantes} días: {nombre}"
    };

    private static string NombreContacto(Asociado? asociado) =>
        string.IsNullOrWhiteSpace(asociado?.Telefono) ? "Teléfono sin registrar" : asociado.Telefono;
}

using Asoagro.Models;

namespace Asoagro.Services;

// Revisa los documentos cada hora y crea un aviso la primera vez que cada documento
// entra en los umbrales de 30, 15 o 7 días, o cuando ya venció.
public class AlertasVencimientoWorker : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    // Solo estos cargos reciben los avisos de vencimientos documentales
    private static readonly string[] RolesVencimientos = new[]
    {
        "Presidenta", "Presidente", "Secretaria", "Secretaría", "Administrador", "Admin"
    };

    private readonly IAlertaDocumentoService _alertas;
    private readonly IDocumentoInstitucionalService _documentosInstitucionales;
    private readonly NotificacionService _notificaciones;
    private readonly ILogger<AlertasVencimientoWorker> _logger;

    public AlertasVencimientoWorker(
        IAlertaDocumentoService alertas,
        IDocumentoInstitucionalService documentosInstitucionales,
        NotificacionService notificaciones,
        ILogger<AlertasVencimientoWorker> logger)
    {
        _alertas = alertas;
        _documentosInstitucionales = documentosInstitucionales;
        _notificaciones = notificaciones;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Espera unos segundos a que la app termine de arrancar y revisa una vez
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
            var nuevos = 0;

            foreach (var alerta in _alertas.ObtenerAlertasPendientes(DateTime.Today))
            {
                var umbral = ObtenerUmbral(alerta.DiasRestantes);
                if (umbral is null) continue;

                var creado = _notificaciones.Publicar(new Notificacion
                {
                    Titulo = $"{alerta.TipoDocumento} {DescribirPlazo(alerta.DiasRestantes)}",
                    Descripcion = $"{alerta.NombreAsociado} · fecha de vencimiento: {alerta.FechaVencimiento:dd/MM/yyyy}.",
                    Etiqueta = alerta.DiasRestantes < 0 ? "Vencido" : "Por vencer",
                    Enlace = "/alertas-documentos",
                    Icono = "alerta",
                    Categoria = CategoriaNotificacion.Documentos,
                    Nivel = ObtenerNivel(umbral.Value),
                    RolesDestino = RolesVencimientos,
                    ClaveUnica = $"documento|{alerta.NombreAsociado}|{alerta.TipoDocumento}|{alerta.FechaVencimiento:yyyy-MM-dd}|{umbral}"
                });

                if (creado) nuevos++;
            }

            foreach (var documento in _documentosInstitucionales.ObtenerTodos())
            {
                if (documento.FechaVencimiento is not DateTime fechaVencimiento
                    || documento.Estado == EstadosDocumento.Anulado
                    || documento.Estado == EstadosDocumento.Derogado)
                {
                    continue;
                }

                var fecha = fechaVencimiento.Date;
                var diasRestantes = (fecha - DateTime.Today).Days;
                var umbral = ObtenerUmbral(diasRestantes);
                if (umbral is null) continue;

                var asociado = string.IsNullOrWhiteSpace(documento.NombreAsociado)
                    ? "Documento institucional"
                    : documento.NombreAsociado;
                var creado = _notificaciones.Publicar(new Notificacion
                {
                    Titulo = $"{documento.Titulo} {DescribirPlazo(diasRestantes)}",
                    Descripcion = $"{documento.Tipo} · {asociado} · fecha de vencimiento: {fecha:dd/MM/yyyy}.",
                    Etiqueta = diasRestantes < 0 ? "Vencido" : "Por vencer",
                    Enlace = "/actas-estatutos/documentos",
                    Icono = "alerta",
                    Categoria = CategoriaNotificacion.Documentos,
                    Nivel = ObtenerNivel(umbral.Value),
                    RolesDestino = new[] { "Presidenta", "Presidente", "Administrador", "Admin", "Secretaria", "Secretaría", "Tesorero", "Tesorera" },
                    RolesEnlace = new[] { "Presidenta", "Presidente" },
                    ClaveUnica = $"acta|{documento.Id:N}|{fecha:yyyy-MM-dd}|{umbral}"
                });

                if (creado) nuevos++;
            }

            if (nuevos > 0)
            {
                _logger.LogInformation("Se crearon {Cantidad} avisos de vencimiento de documentos.", nuevos);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible revisar los vencimientos de documentos.");
        }
    }

    // Umbral en el que cae el documento: 0 = vencido, 7, 15 o 30 días. Null = todavía falta más de 30 días.
    private static int? ObtenerUmbral(int dias)
    {
        if (dias < 0) return 0;
        if (dias <= 7) return 7;
        if (dias <= 15) return 15;
        if (dias <= 30) return 30;
        return null;
    }

    private static NivelNotificacion ObtenerNivel(int umbral) => umbral switch
    {
        <= 7 => NivelNotificacion.Urgente,
        15 => NivelNotificacion.Advertencia,
        _ => NivelNotificacion.Informativa
    };

    private static string DescribirPlazo(int dias) => dias switch
    {
        < 0 => "ya venció",
        0 => "vence hoy",
        1 => "vence mañana",
        _ => $"vence en {dias} días"
    };
}
using Asoagro.Models;

namespace Asoagro.Services;

public class InMemoryAlertaDocumentoService : IAlertaDocumentoService
{
    private readonly IDocumentoAsociacionService _asociacionService;

    public InMemoryAlertaDocumentoService(IDocumentoAsociacionService asociacionService)
    {
        _asociacionService = asociacionService;
    }

    public IReadOnlyCollection<AlertaDocumento> ObtenerAlertasPendientes(DateTime fechaReferencia)
    {
        var alertas = _asociacionService.ObtenerAlertas(fechaReferencia);
        return alertas.Select(a => new AlertaDocumento
        {
            NombreAsociado = a.NombreAsociacion,
            TipoDocumento = $"{a.TipoDocumento}{(string.IsNullOrWhiteSpace(a.Titulo) || a.Titulo == a.TipoDocumento ? "" : $" - {a.Titulo}")}",
            FechaVencimiento = a.FechaVencimiento,
            DiasRestantes = a.DiasRestantes,
            MensajeAlerta = a.NivelAlerta
        }).ToList();
    }
}

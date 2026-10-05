using Asoagro.Models;

namespace Asoagro.Services;

public class InMemoryAlertaDocumentoService : IAlertaDocumentoService
{
    public IReadOnlyCollection<AlertaDocumento> ObtenerAlertasPendientes(DateTime fechaReferencia)
    {
        return new List<AlertaDocumento>
        {
            CrearAlerta("Carlos Pérez", "Certificado de Tradición", fechaReferencia, 5),
            CrearAlerta("Ana Gómez", "Buenas Prácticas Agrícolas (BPA)", fechaReferencia, 12),
            CrearAlerta("Luis Martínez", "Registro ICA", fechaReferencia, 25)
        };
    }

    private static AlertaDocumento CrearAlerta(string asociado, string tipoDocumento, DateTime fechaReferencia, int diasRestantes)
    {
        return new AlertaDocumento
        {
            NombreAsociado = asociado,
            TipoDocumento = tipoDocumento,
            FechaVencimiento = fechaReferencia.Date.AddDays(diasRestantes),
            DiasRestantes = diasRestantes,
            MensajeAlerta = ObtenerMensaje(diasRestantes)
        };
    }

    private static string ObtenerMensaje(int diasRestantes)
    {
        if (diasRestantes <= 7) return "Alerta crítica: vence en menos de 7 días";
        if (diasRestantes <= 15) return "Advertencia: vence en menos de 15 días";
        return "Aviso preventivo: vence en menos de 30 días";
    }
}

using Asoagro.Models;

namespace Asoagro.Services;

public interface IAlertaDocumentoService
{
    IReadOnlyCollection<AlertaDocumento> ObtenerAlertasPendientes(DateTime fechaReferencia);
}

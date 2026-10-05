using Asoagro.Models;
using Asoagro.Services;
using Microsoft.AspNetCore.Mvc;

namespace Asoagro.Controllers;

[Route("api/alertas-documentos")]
[ApiController]
public class AlertasDocumentosController : ControllerBase
{
    private readonly IAlertaDocumentoService alertaDocumentoService;

    public AlertasDocumentosController(IAlertaDocumentoService alertaDocumentoService)
    {
        this.alertaDocumentoService = alertaDocumentoService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<AlertaDocumento>> ObtenerAlertas()
    {
        return Ok(alertaDocumentoService.ObtenerAlertasPendientes(DateTime.UtcNow));
    }
}

using Asoagro.Services;
using Microsoft.AspNetCore.Mvc;

namespace Asoagro.Controllers;

[Route("api/documentos-institucionales")]
[ApiController]
public class DocumentosInstitucionalesController : ControllerBase
{
    private readonly IDocumentoInstitucionalService _service;

    public DocumentosInstitucionalesController(IDocumentoInstitucionalService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}/descargar")]
    public IActionResult DescargarArchivo(Guid id)
    {
        var doc = _service.ObtenerPorId(id);
        if (doc?.Archivo is null)
        {
            return NotFound("El documento no cuenta con archivo adjunto.");
        }

        var usuario = User.Identity?.Name ?? "Presidente / Usuario";
        _service.RegistrarDescarga(id, usuario);

        return File(doc.Archivo.Contenido, doc.Archivo.TipoMime, doc.Archivo.Nombre);
    }
}

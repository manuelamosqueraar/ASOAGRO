using Microsoft.AspNetCore.Mvc;
using Asoagro.Models;
using Asoagro.Services;

namespace Asoagro.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AsociadosController : ControllerBase
    {
        private readonly IAsociadoService asociadoService;

        public AsociadosController(IAsociadoService asociadoService)
        {
            this.asociadoService = asociadoService;
        }

        [HttpPost]
        public IActionResult RegistrarAsociado([FromBody] Asociado asociado)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var resultado = asociadoService.Registrar(asociado);

            if (!resultado.Exitoso)
            {
                return Conflict(new ApiError { Code = "asociado_duplicado", Message = resultado.Mensaje });
            }

            return Ok(new { mensaje = resultado.Mensaje, data = resultado.Asociado });
        }

        [HttpGet]
        public ActionResult<IEnumerable<Asociado>> ObtenerAsociados()
        {
            return Ok(asociadoService.ObtenerTodos());
        }
    }
}

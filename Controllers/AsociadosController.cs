using Microsoft.AspNetCore.Mvc;
using Asoagro.Models;

namespace Asoagro.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AsociadosController : ControllerBase
    {
        private static readonly List<Asociado> _listaAsociados = new();

        [HttpPost]
        public IActionResult RegistrarAsociado([FromBody] Asociado asociado)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            asociado.Id = _listaAsociados.Count + 1;
            asociado.FechaRegistro = DateTime.UtcNow;
            _listaAsociados.Add(asociado);

            return Ok(new { mensaje = "Asociado registrado exitosamente en el servidor de Asoagro", data = asociado });
        }

        [HttpGet]
        public ActionResult<IEnumerable<Asociado>> ObtenerAsociados()
        {
            return Ok(_listaAsociados);
        }
    }
}
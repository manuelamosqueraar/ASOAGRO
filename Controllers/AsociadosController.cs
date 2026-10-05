using Microsoft.AspNetCore.Mvc;
using Asoagro.Models;

namespace Asoagro.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AsociadosController : ControllerBase
    {
        private static readonly List<Asociado> _listaAsociados = new();
        private static readonly object _lock = new();

        [HttpPost]
        public IActionResult RegistrarAsociado([FromBody] Asociado asociado)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            lock (_lock)
            {
                if (_listaAsociados.Any(a => a.Cedula == asociado.Cedula))
                {
                    return Conflict(new { mensaje = "Ya existe un asociado registrado con esa cédula." });
                }

                asociado.Id = _listaAsociados.Count + 1;
                asociado.FechaRegistro = DateTime.UtcNow;
                _listaAsociados.Add(asociado);
            }

            return Ok(new { mensaje = "Asociado registrado exitosamente en el servidor de Asoagro", data = asociado });
        }

        [HttpGet]
        public ActionResult<IEnumerable<Asociado>> ObtenerAsociados()
        {
            lock (_lock)
            {
                return Ok(_listaAsociados.ToList());
            }
        }
    }
}

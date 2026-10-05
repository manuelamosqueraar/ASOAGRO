using Asoagro.Models;
using Asoagro.Configuration;
using Microsoft.Extensions.Options;

namespace Asoagro.Services;

public class InMemoryAsociadoService : IAsociadoService
{
    private readonly AsoagroOptions options;
    private readonly List<Asociado> asociados = new();
    private readonly object syncRoot = new();

    public InMemoryAsociadoService(IOptions<AsoagroOptions> options)
    {
        this.options = options.Value;
    }

    public IReadOnlyCollection<Asociado> ObtenerTodos()
    {
        lock (syncRoot)
        {
            return asociados.ToList();
        }
    }

    public ResultadoRegistroAsociado Registrar(Asociado asociado)
    {
        lock (syncRoot)
        {
            asociado.Cedula = asociado.Cedula.Trim();
            asociado.NombreCompleto = asociado.NombreCompleto.Trim();
            asociado.FincaVereda = asociado.FincaVereda.Trim();
            asociado.CultivoPrincipal = asociado.CultivoPrincipal.Trim();

            if (asociados.Any(a => a.Cedula == asociado.Cedula))
            {
                return new ResultadoRegistroAsociado
                {
                    Exitoso = false,
                    Mensaje = "Ya existe un asociado registrado con esa cédula."
                };
            }

            asociado.Id = asociados.Count + 1;
            asociado.FechaRegistro = DateTime.UtcNow;
            asociado.FechaActualizacion = asociado.FechaRegistro;
            asociado.CreadoPor = options.Audit.DefaultUser;
            asociado.ActualizadoPor = options.Audit.DefaultUser;
            asociados.Add(asociado);

            return new ResultadoRegistroAsociado
            {
                Exitoso = true,
                Mensaje = "Asociado registrado exitosamente en el servidor de Asoagro",
                Asociado = asociado
            };
        }
    }
}

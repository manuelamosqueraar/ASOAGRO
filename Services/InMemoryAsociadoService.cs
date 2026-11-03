using Asoagro.Models;
using Asoagro.Configuration;
using Microsoft.Extensions.Options;

namespace Asoagro.Services;

public class InMemoryAsociadoService : IAsociadoService
{
    private readonly AsoagroOptions options;
    private readonly List<Asociado> asociados =
    [
        new Asociado
        {
            Id = 1, Cedula = "1045123890", NombreCompleto = "Juan Antonio Pérez",
            FincaVereda = "Finca La Esperanza - Vereda El Tres", Telefono = "3104567890",
            Correo = "juan.perez@email.com", NombrePredio = "Finca La Esperanza",
            VeredaSector = "Vereda El Tres", Municipio = "Apartadó", HectareasPredio = 5,
            CultivoPrincipal = "Platano", TieneBPA = true, EsVictima = false,
            EstadoMembresia = "AlDia", FechaRegistro = DateTime.UtcNow.AddDays(-3)
        },
        new Asociado
        {
            Id = 2, Cedula = "43567890", NombreCompleto = "María Elena Garcés",
            FincaVereda = "Lote El Danubio - Sector Zungo", Telefono = "3201234567",
            Correo = "maria.garces@email.com", NombrePredio = "Lote El Danubio",
            VeredaSector = "Sector Zungo", Municipio = "Carepa", HectareasPredio = 3.5m,
            CultivoPrincipal = "Banano", TieneBPA = false, EsVictima = true,
            EstadoMembresia = "AlDia", FechaRegistro = DateTime.UtcNow.AddDays(-7)
        }
    ];
    private readonly object syncRoot = new();

    public InMemoryAsociadoService(IOptions<AsoagroOptions> options)
    {
        this.options = options.Value;
    }

    public bool UsaAlmacenamientoPermanente => false;

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

    public ResultadoRegistroAsociado Actualizar(Asociado asociado)
    {
        lock (syncRoot)
        {
            var registro = asociados.FirstOrDefault(a => a.Id == asociado.Id);
            if (registro is null)
            {
                return new ResultadoRegistroAsociado
                {
                    Exitoso = false,
                    Mensaje = "No se encontró el asociado que deseas actualizar."
                };
            }

            if (asociados.Any(a => a.Id != asociado.Id && a.Cedula == asociado.Cedula))
            {
                return new ResultadoRegistroAsociado
                {
                    Exitoso = false,
                    Mensaje = "Ya existe otro asociado registrado con esa cédula."
                };
            }

            registro.Cedula = asociado.Cedula.Trim();
            registro.NombreCompleto = asociado.NombreCompleto.Trim();
            registro.FincaVereda = asociado.FincaVereda.Trim();
            registro.Telefono = asociado.Telefono?.Trim();
            registro.Correo = asociado.Correo?.Trim();
            registro.NombrePredio = asociado.NombrePredio?.Trim();
            registro.VeredaSector = asociado.VeredaSector?.Trim();
            registro.Municipio = asociado.Municipio?.Trim();
            registro.HectareasPredio = asociado.HectareasPredio;
            registro.CultivoPrincipal = asociado.CultivoPrincipal.Trim();
            registro.TieneBPA = asociado.TieneBPA;
            registro.EsVictima = asociado.EsVictima;
            registro.EstadoMembresia = asociado.EstadoMembresia;
            registro.FechaActualizacion = DateTime.UtcNow;
            registro.ActualizadoPor = options.Audit.DefaultUser;

            return new ResultadoRegistroAsociado
            {
                Exitoso = true,
                Mensaje = "El expediente se actualizó correctamente.",
                Asociado = registro
            };
        }
    }
}

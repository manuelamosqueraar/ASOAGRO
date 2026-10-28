using Asoagro.Configuration;
using Asoagro.Data;
using Asoagro.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Asoagro.Services;

public class PostgresAsociadoService : IAsociadoService
{
    private readonly IDbContextFactory<AsoagroDbContext> dbContextFactory;
    private readonly AsoagroOptions options;

    public PostgresAsociadoService(
        IDbContextFactory<AsoagroDbContext> dbContextFactory,
        IOptions<AsoagroOptions> options)
    {
        this.dbContextFactory = dbContextFactory;
        this.options = options.Value;
    }

    public IReadOnlyCollection<Asociado> ObtenerTodos()
    {
        using var context = dbContextFactory.CreateDbContext();
        return context.Asociados
            .AsNoTracking()
            .OrderByDescending(a => a.FechaRegistro)
            .ToList();
    }

    public ResultadoRegistroAsociado Registrar(Asociado asociado)
    {
        using var context = dbContextFactory.CreateDbContext();

        asociado.Cedula = asociado.Cedula.Trim();
        asociado.NombreCompleto = asociado.NombreCompleto.Trim();
        asociado.FincaVereda = asociado.FincaVereda.Trim();
        asociado.CultivoPrincipal = asociado.CultivoPrincipal.Trim();
        asociado.Telefono = asociado.Telefono.Trim();
        asociado.Correo = asociado.Correo.Trim();
        asociado.NombrePredio = string.IsNullOrWhiteSpace(asociado.NombrePredio)
            ? asociado.FincaVereda
            : asociado.NombrePredio.Trim();
        asociado.VeredaSector = string.IsNullOrWhiteSpace(asociado.VeredaSector)
            ? asociado.FincaVereda
            : asociado.VeredaSector.Trim();
        asociado.Municipio = asociado.Municipio.Trim();

        if (context.Asociados.Any(a => a.Cedula == asociado.Cedula))
        {
            return new ResultadoRegistroAsociado
            {
                Exitoso = false,
                Mensaje = "Ya existe un asociado registrado con esa cédula."
            };
        }

        asociado.Id = 0;
        asociado.FechaRegistro = DateTime.UtcNow;
        asociado.FechaActualizacion = asociado.FechaRegistro;
        asociado.CreadoPor = options.Audit.DefaultUser;
        asociado.ActualizadoPor = options.Audit.DefaultUser;

        context.Asociados.Add(asociado);
        context.SaveChanges();

        return new ResultadoRegistroAsociado
        {
            Exitoso = true,
            Mensaje = "Asociado registrado exitosamente en el servidor de Asoagro",
            Asociado = asociado
        };
    }
}


using Asoagro.Data;
using Asoagro.Models;
using Microsoft.EntityFrameworkCore;

namespace Asoagro.Services;

public class PostgresSeguimientoAsociadoService(
    IDbContextFactory<AsoagroDbContext> dbContextFactory) : ISeguimientoAsociadoService
{
    public bool UsaAlmacenamientoPermanente => true;

    public IReadOnlyCollection<SeguimientoAsociado> ObtenerTodos()
    {
        using var context = dbContextFactory.CreateDbContext();
        return context.SeguimientosAsociados
            .AsNoTracking()
            .Include(seguimiento => seguimiento.Asociado)
            .OrderByDescending(seguimiento => seguimiento.FechaGestion)
            .ToList();
    }

    public ResultadoRegistroSeguimientoAsociado Registrar(SeguimientoAsociado seguimiento)
    {
        using var context = dbContextFactory.CreateDbContext();
        if (!context.Asociados.Any(asociado => asociado.Id == seguimiento.AsociadoId))
        {
            return new ResultadoRegistroSeguimientoAsociado
            {
                Exitoso = false,
                Mensaje = "El asociado seleccionado ya no existe en el padrón."
            };
        }

        seguimiento.Id = 0;
        seguimiento.Asociado = null;
        seguimiento.FechaGestion = DateTime.UtcNow;
        context.SeguimientosAsociados.Add(seguimiento);
        context.SaveChanges();

        return new ResultadoRegistroSeguimientoAsociado
        {
            Exitoso = true,
            Mensaje = "La gestión quedó guardada en el historial."
        };
    }
}

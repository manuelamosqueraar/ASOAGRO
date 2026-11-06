using System.Collections.Concurrent;
using Asoagro.Data;
using Asoagro.Models;
using Microsoft.EntityFrameworkCore;

namespace Asoagro.Services;

public class PostgresSeguimientoAsociadoService(
    IDbContextFactory<AsoagroDbContext> dbContextFactory) : ISeguimientoAsociadoService
{
    private static readonly ConcurrentDictionary<int, string> CacheHoras = new();

    public bool UsaAlmacenamientoPermanente => true;

    public IReadOnlyCollection<SeguimientoAsociado> ObtenerTodos()
    {
        using var context = dbContextFactory.CreateDbContext();
        var lista = context.SeguimientosAsociados
            .AsNoTracking()
            .Include(seguimiento => seguimiento.Asociado)
            .OrderByDescending(seguimiento => seguimiento.FechaGestion)
            .ToList();

        foreach (var seg in lista)
        {
            if (CacheHoras.TryGetValue(seg.Id, out var hora))
            {
                seg.HoraReunion = hora;
            }
        }

        return lista;
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

        var hora = seguimiento.HoraReunion;
        seguimiento.Id = 0;
        seguimiento.Asociado = null;
        seguimiento.FechaGestion = DateTime.UtcNow;
        context.SeguimientosAsociados.Add(seguimiento);
        context.SaveChanges();

        if (!string.IsNullOrWhiteSpace(hora))
        {
            CacheHoras[seguimiento.Id] = hora;
            seguimiento.HoraReunion = hora;
        }

        return new ResultadoRegistroSeguimientoAsociado
        {
            Exitoso = true,
            Mensaje = "La gestión quedó guardada en el historial."
        };
    }
}

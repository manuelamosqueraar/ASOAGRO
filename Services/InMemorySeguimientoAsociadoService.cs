using Asoagro.Models;

namespace Asoagro.Services;

public class InMemorySeguimientoAsociadoService(IAsociadoService asociadoService)
    : ISeguimientoAsociadoService
{
    private readonly List<SeguimientoAsociado> seguimientos = new();
    private readonly object syncRoot = new();

    public bool UsaAlmacenamientoPermanente => false;

    public IReadOnlyCollection<SeguimientoAsociado> ObtenerTodos()
    {
        var asociados = asociadoService.ObtenerTodos().ToDictionary(asociado => asociado.Id);
        lock (syncRoot)
        {
            return seguimientos
                .Select(seguimiento => Copiar(seguimiento, asociados.GetValueOrDefault(seguimiento.AsociadoId)))
                .OrderByDescending(seguimiento => seguimiento.FechaGestion)
                .ToList();
        }
    }

    public ResultadoRegistroSeguimientoAsociado Registrar(SeguimientoAsociado seguimiento)
    {
        var asociado = asociadoService.ObtenerTodos()
            .FirstOrDefault(registro => registro.Id == seguimiento.AsociadoId);
        if (asociado is null)
        {
            return new ResultadoRegistroSeguimientoAsociado
            {
                Exitoso = false,
                Mensaje = "El asociado seleccionado ya no existe en el padrón."
            };
        }

        lock (syncRoot)
        {
            seguimiento.Id = seguimientos.Count == 0 ? 1 : seguimientos.Max(registro => registro.Id) + 1;
            seguimiento.FechaGestion = DateTime.UtcNow;
            seguimientos.Add(Copiar(seguimiento, asociado));
        }

        return new ResultadoRegistroSeguimientoAsociado
        {
            Exitoso = true,
            Mensaje = "La gestión quedó guardada en el historial temporal de esta sesión."
        };
    }

    private static SeguimientoAsociado Copiar(SeguimientoAsociado seguimiento, Asociado? asociado) => new()
    {
        Id = seguimiento.Id,
        AsociadoId = seguimiento.AsociadoId,
        TipoGestion = seguimiento.TipoGestion,
        FechaGestion = seguimiento.FechaGestion,
        Descripcion = seguimiento.Descripcion,
        ProximaGestion = seguimiento.ProximaGestion,
        HoraReunion = seguimiento.HoraReunion,
        GestionadoPor = seguimiento.GestionadoPor,
        Asociado = asociado
    };
}

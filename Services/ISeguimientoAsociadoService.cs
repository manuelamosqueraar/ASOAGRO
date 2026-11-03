using Asoagro.Models;

namespace Asoagro.Services;

public interface ISeguimientoAsociadoService
{
    bool UsaAlmacenamientoPermanente { get; }

    IReadOnlyCollection<SeguimientoAsociado> ObtenerTodos();

    ResultadoRegistroSeguimientoAsociado Registrar(SeguimientoAsociado seguimiento);
}

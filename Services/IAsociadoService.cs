using Asoagro.Models;

namespace Asoagro.Services;

public interface IAsociadoService
{
    bool UsaAlmacenamientoPermanente { get; }

    IReadOnlyCollection<Asociado> ObtenerTodos();

    ResultadoRegistroAsociado Registrar(Asociado asociado);

    ResultadoRegistroAsociado Actualizar(Asociado asociado);
}

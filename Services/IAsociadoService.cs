using Asoagro.Models;

namespace Asoagro.Services;

public interface IAsociadoService
{
    IReadOnlyCollection<Asociado> ObtenerTodos();

    ResultadoRegistroAsociado Registrar(Asociado asociado);
}

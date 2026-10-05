using Asoagro.Models;

namespace Asoagro.Services;

public class ResultadoRegistroAsociado
{
    public bool Exitoso { get; init; }

    public string Mensaje { get; init; } = string.Empty;

    public Asociado? Asociado { get; init; }
}

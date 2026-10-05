namespace Asoagro.Models;

public class ApiError
{
    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public IDictionary<string, string[]>? Details { get; init; }
}

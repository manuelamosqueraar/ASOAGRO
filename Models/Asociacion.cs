namespace Asoagro.Models;

// Cada asociación es un espacio independiente de consulta y control.
public class Asociacion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Nit { get; set; }
    public string? Municipio { get; set; }
    public bool Activo { get; set; } = true;
}
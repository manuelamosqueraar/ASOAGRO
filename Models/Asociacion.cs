namespace Asoagro.Models;

// Cada asociación es un espacio institucional independiente de consulta y control.
public class Asociacion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Nit { get; set; }
    public string? Municipio { get; set; }
    public string? RepresentanteLegal { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

namespace Asoagro.Models;

public class AsociacionEntity
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

    public List<Asociado> Asociados { get; set; } = new();
    public List<Documento> Documentos { get; set; } = new();
    public List<Acta> Actas { get; set; } = new();
}


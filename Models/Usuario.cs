namespace Asoagro.Models;

public class Usuario
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string DocumentoId { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? ContrasenaHash { get; set; }
    public string Rol { get; set; } = "directora";
    public int? AsociacionId { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime? UltimoAcceso { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public Asociacion? Asociacion { get; set; }
}


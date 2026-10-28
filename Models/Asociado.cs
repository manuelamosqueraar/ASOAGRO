using System.ComponentModel.DataAnnotations;

namespace Asoagro.Models;

public class Asociado
{
    public int Id { get; set; }

    [Required(ErrorMessage = "La cédula es obligatoria.")]
    public string Cedula { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    public string NombreCompleto { get; set; } = string.Empty;

    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? NombrePredio { get; set; }
    public string? VeredaSector { get; set; }
    public string? Municipio { get; set; }
    public decimal? HectareasPredio { get; set; }

    [Required(ErrorMessage = "La finca o vereda es obligatoria.")]
    public string FincaVereda { get; set; } = string.Empty;

    [Required(ErrorMessage = "El cultivo principal es obligatorio.")]
    public string CultivoPrincipal { get; set; } = string.Empty;

    public bool TieneBPA { get; set; }
    public bool EsVictima { get; set; }
    public string EstadoMembresia { get; set; } = "Activo";

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
    public string CreadoPor { get; set; } = "Sistema";
    public string? ActualizadoPor { get; set; }
}

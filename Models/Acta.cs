using System.ComponentModel.DataAnnotations;

namespace Asoagro.Models;

public class Acta
{
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    public string Tipo { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [Required]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string ArchivoNombre { get; set; } = string.Empty;
}


using System.ComponentModel.DataAnnotations;

namespace Asoagro.Models;

public class TipoDocumento
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Nombre { get; set; } = string.Empty;
}


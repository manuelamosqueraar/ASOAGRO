using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asoagro.Models;

public class TipoDocumento
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    public string? Categoria { get; set; }

    public bool RequiereVencimiento { get; set; }

    public bool AplicaAAsociado { get; set; }

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;

    // Propiedad de compatibilidad para código que esperaba Codigo:
    [NotMapped]
    public string Codigo
    {
        get => Id.ToString();
        set { }
    }
}

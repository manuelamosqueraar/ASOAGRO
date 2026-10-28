using System.ComponentModel.DataAnnotations;

namespace Asoagro.Models;

public class Documento
{
    public int Id { get; set; }

    public int AsociadoId { get; set; }

    public int? TipoDocumentoId { get; set; }

    [Required]
    [StringLength(20)]
    public string DocumentoAsociado { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string TipoDocumento { get; set; } = string.Empty;

    [StringLength(255)]
    public string ArchivoNombre { get; set; } = string.Empty;

    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;

    public DateTime? FechaVencimiento { get; set; }

    [StringLength(30)]
    public string Estado { get; set; } = "Vigente";

    public Asociado? Asociado { get; set; }

    public TipoDocumento? TipoDocumentoNav { get; set; }
}


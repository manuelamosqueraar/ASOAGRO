using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asoagro.Models;

public class Documento
{
    public int Id { get; set; }

    public int AsociacionId { get; set; }

    public int? AsociadoId { get; set; }

    public int TipoDocumentoId { get; set; }

    [StringLength(50)]
    public string? NumeroDocumento { get; set; }

    [StringLength(150)]
    public string? EntidadEmisora { get; set; }

    public DateTime? FechaExpedicion { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    [StringLength(30)]
    public string Estado { get; set; } = "Vigente";

    [StringLength(255)]
    public string? ArchivoNombre { get; set; }

    [StringLength(500)]
    public string? ArchivoRuta { get; set; }

    public string? Observaciones { get; set; }

    public int? SubidoPor { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime? ActualizadoEn { get; set; }

    // Propiedades de compatibilidad para código y vistas existentes:
    [NotMapped]
    public string DocumentoAsociado
    {
        get => NumeroDocumento ?? string.Empty;
        set => NumeroDocumento = value;
    }

    [NotMapped]
    public string TipoDocumento
    {
        get => TipoDocumentoNav?.Nombre ?? string.Empty;
        set { /* Compatibilidad */ }
    }

    [NotMapped]
    public DateTime FechaEmision
    {
        get => FechaExpedicion ?? CreadoEn;
        set => FechaExpedicion = value;
    }

    public AsociacionEntity? Asociacion { get; set; }

    public Asociado? Asociado { get; set; }

    public TipoDocumento? TipoDocumentoNav { get; set; }
}


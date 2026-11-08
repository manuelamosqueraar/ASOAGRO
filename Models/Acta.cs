using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asoagro.Models;

public class Acta
{
    public int Id { get; set; }

    public int AsociacionId { get; set; }

    [StringLength(50)]
    public string? NumeroActa { get; set; }

    [Required]
    [StringLength(60)]
    public string Tipo { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    public DateTime FechaActa { get; set; } = DateTime.UtcNow;

    public string? Resumen { get; set; }

    [StringLength(255)]
    public string? ArchivoNombre { get; set; }

    [StringLength(500)]
    public string? ArchivoRuta { get; set; }

    public int? SubidoPor { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    // Propiedades de compatibilidad para código existente:
    [NotMapped]
    public DateTime Fecha
    {
        get => FechaActa;
        set => FechaActa = value;
    }

    [NotMapped]
    public string Descripcion
    {
        get => Resumen ?? string.Empty;
        set => Resumen = value;
    }

    public AsociacionEntity? Asociacion { get; set; }
}


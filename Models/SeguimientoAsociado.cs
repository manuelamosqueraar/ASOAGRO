namespace Asoagro.Models;

public class SeguimientoAsociado
{
    public int Id { get; set; }
    public int AsociadoId { get; set; }
    public string TipoGestion { get; set; } = string.Empty;
    public DateTime FechaGestion { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateOnly? ProximaGestion { get; set; }
    public string HoraReunion { get; set; } = string.Empty;
    public string GestionadoPor { get; set; } = string.Empty;
    public Asociado? Asociado { get; set; }
}

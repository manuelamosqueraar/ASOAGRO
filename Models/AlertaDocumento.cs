namespace Asoagro.Models;

public class AlertaDocumento
{
    public string NombreAsociado { get; set; } = string.Empty;

    public string TipoDocumento { get; set; } = string.Empty;

    public DateTime FechaVencimiento { get; set; }

    public int DiasRestantes { get; set; }

    public string MensajeAlerta { get; set; } = string.Empty;
}

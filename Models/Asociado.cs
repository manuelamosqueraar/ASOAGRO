using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    public int AsociacionId { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
    public string CreadoPor { get; set; } = "Sistema";
    public string? ActualizadoPor { get; set; }

    public AsociacionEntity? Asociacion { get; set; }
    public List<AsociadoCultivoEntity> AsociadoCultivos { get; set; } = new();
    public List<AsociadoGrupoVulnerableEntity> AsociadoGruposVulnerables { get; set; } = new();
    public List<CuotaAsociadoEntity> CuotasAsociados { get; set; } = new();

    [NotMapped]
    public string TipoDocumentoIdentidad { get; set; } = "CC";

    [NotMapped]
    public string? AsociacionPertenece { get; set; }

    [NotMapped]
    public List<AfiliacionAsociado> Afiliaciones { get; set; } = new();

    [NotMapped]
    public bool AutorizacionDatosPersonales { get; set; } = true;

    [NotMapped]
    public DateTime? FechaAutorizacionHabeasData { get; set; } = DateTime.Now;

    [NotMapped]
    public string? TenenciaTerreno { get; set; }

    [NotMapped]
    public string? TipoActividadProductiva { get; set; }

    [NotMapped]
    public string? ProduccionAproximada { get; set; }

    [NotMapped]
    public string? CedulaArchivoNombre { get; set; }

    [NotMapped]
    public string? CedulaArchivoTamano { get; set; }

    [NotMapped]
    public string? CedulaArchivoUrl { get; set; }

    [NotMapped]
    public decimal? HectareasLineaProductiva { get; set; }

    [NotMapped]
    public List<string> CultivosSeleccionados { get; set; } = new();

    [NotMapped]
    public string? CertificadoBpaNombre { get; set; }

    [NotMapped]
    public string? CertificadoBpaTamano { get; set; }

    [NotMapped]
    public string? CertificadoBpaUrl { get; set; }

    [NotMapped]
    public List<string> GruposVulnerables { get; set; } = new();

    [NotMapped]
    public Dictionary<string, string> SoportesGruposVulnerables { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [NotMapped]
    public Dictionary<string, string> SoportesGruposUrls { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [NotMapped]
    public List<CuotaAporteAsociado> CuotasAportes { get; set; } = new();
}

public class SoporteCuotaAsociado
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CuotaId { get; set; } = string.Empty;
    public string TipoCuota { get; set; } = string.Empty;
    public string Periodicidad { get; set; } = string.Empty;
    public string ArchivoNombre { get; set; } = string.Empty;
    public string ArchivoTamano { get; set; } = string.Empty;
    public string ArchivoUrl { get; set; } = string.Empty;
    public DateTime FechaCarga { get; set; } = DateTime.Now;

    public SoporteCuotaAsociado Clonar() => new()
    {
        Id = Id,
        CuotaId = CuotaId,
        TipoCuota = TipoCuota,
        Periodicidad = Periodicidad,
        ArchivoNombre = ArchivoNombre,
        ArchivoTamano = ArchivoTamano,
        ArchivoUrl = ArchivoUrl,
        FechaCarga = FechaCarga
    };
}

public class CuotaAporteAsociado
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TipoCuota { get; set; } = "Sostenimiento";
    public decimal ValorCuota { get; set; }
    public string Periodicidad { get; set; } = "Mensual";
    public DateTime? FechaVencimiento { get; set; } = DateTime.Today.AddMonths(1);
    public int CantidadCuotasPendientes { get; set; }
    public decimal ValorTotalPendiente { get; set; }
    public decimal ValorTotalPagado { get; set; }
    public bool PagadoEditadoManualmente { get; set; }
    public string EstadoPago { get; set; } = "Por liquidar";
    public List<SoporteCuotaAsociado> Soportes { get; set; } = new();

    public decimal TotalEstablecido => CantidadCuotasPendientes > 0
        ? ValorCuota * CantidadCuotasPendientes
        : ValorCuota;

    public void RecalcularAutomaticamente()
    {
        if (ValorCuota < 0) ValorCuota = 0;
        if (CantidadCuotasPendientes < 0) CantidadCuotasPendientes = 0;

        if (!PagadoEditadoManualmente)
        {
            ValorTotalPagado = CantidadCuotasPendientes == 0 && ValorCuota > 0
                ? ValorCuota
                : 0m;
        }

        if (ValorTotalPagado < 0) ValorTotalPagado = 0;

        ValorTotalPendiente = Math.Max(0m, TotalEstablecido - ValorTotalPagado);

        ActualizarEstadoYSoportes();
    }

    public void ActualizarEstadoYSoportes()
    {
        if (ValorTotalPagado < 0) ValorTotalPagado = 0;
        ValorTotalPendiente = Math.Max(0m, TotalEstablecido - ValorTotalPagado);

        EstadoPago = CalcularEstadoAutomatico();

        foreach (var soporte in Soportes)
        {
            soporte.CuotaId = Id;
            soporte.TipoCuota = TipoCuota;
            soporte.Periodicidad = Periodicidad;
        }
    }

    public string CalcularEstadoAutomatico()
    {
        var vencido = FechaVencimiento.HasValue && FechaVencimiento.Value.Date < DateTime.Today;

        if (ValorTotalPendiente <= 0 && CantidadCuotasPendientes == 0 && ValorTotalPagado > 0)
        {
            return "Pagado · Al día";
        }

        if (vencido && (ValorTotalPendiente > 0 || CantidadCuotasPendientes > 0))
        {
            return "Vencido · En mora";
        }

        if (ValorTotalPagado > 0 && (ValorTotalPendiente > 0 || CantidadCuotasPendientes > 0))
        {
            return "Pago parcial";
        }

        if (ValorTotalPendiente > 0 || CantidadCuotasPendientes > 0)
        {
            return "Pendiente de pago";
        }

        if (ValorCuota > 0 && CantidadCuotasPendientes == 0 && ValorTotalPagado <= 0)
        {
            return "Pendiente de pago";
        }

        return "Por liquidar";
    }

    public CuotaAporteAsociado Clonar() => new()
    {
        Id = Id,
        TipoCuota = TipoCuota,
        ValorCuota = ValorCuota,
        Periodicidad = Periodicidad,
        FechaVencimiento = FechaVencimiento,
        CantidadCuotasPendientes = CantidadCuotasPendientes,
        ValorTotalPendiente = ValorTotalPendiente,
        ValorTotalPagado = ValorTotalPagado,
        PagadoEditadoManualmente = PagadoEditadoManualmente,
        EstadoPago = EstadoPago,
        Soportes = Soportes.Select(s => s.Clonar()).ToList()
    };
}


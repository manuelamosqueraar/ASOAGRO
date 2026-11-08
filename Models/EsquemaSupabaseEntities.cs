namespace Asoagro.Models;

public class CultivoEntity
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public class GrupoVulnerableEntity
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public class TipoCuotaEntity
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool PermitePeriodicidad { get; set; } = true;
    public bool Activo { get; set; } = true;
}

public class AsociadoCultivoEntity
{
    public int Id { get; set; }
    public int AsociadoId { get; set; }
    public int CultivoId { get; set; }
    public decimal? Hectareas { get; set; }
    public bool TieneBpa { get; set; }
    public string? CertificadoBpaNombre { get; set; }
    public string? CertificadoBpaRuta { get; set; }
    public DateOnly? BpaFechaVencimiento { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public Asociado? Asociado { get; set; }
    public CultivoEntity? Cultivo { get; set; }
}

public class AsociadoGrupoVulnerableEntity
{
    public int Id { get; set; }
    public int AsociadoId { get; set; }
    public int GrupoId { get; set; }
    public string? ArchivoNombre { get; set; }
    public string? ArchivoRuta { get; set; }
    public string? Observaciones { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public Asociado? Asociado { get; set; }
    public GrupoVulnerableEntity? GrupoVulnerable { get; set; }
}

public class CuotaAsociadoEntity
{
    public int Id { get; set; }
    public int AsociadoId { get; set; }
    public int TipoCuotaId { get; set; }
    public string? Periodicidad { get; set; }
    public DateOnly? PeriodoDesde { get; set; }
    public DateOnly? PeriodoHasta { get; set; }
    public decimal Valor { get; set; }
    public decimal ValorPagado { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public DateOnly? FechaPago { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string? Motivo { get; set; }
    public int? RegistradoPor { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public Asociado? Asociado { get; set; }
    public TipoCuotaEntity? TipoCuota { get; set; }
}

public class UsuarioEntity
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string DocumentoId { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? ContrasenaHash { get; set; }
    public string Rol { get; set; } = "Consulta";
    public int? AsociacionId { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime? UltimoAcceso { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public AsociacionEntity? Asociacion { get; set; }
}

public class PermisoModuloEntity
{
    public int Id { get; set; }
    public string Rol { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public bool PuedeVer { get; set; }
    public bool PuedeCrear { get; set; }
    public bool PuedeEditar { get; set; }
    public bool PuedeEliminar { get; set; }
}

public class AuditoriaEntity
{
    public long Id { get; set; }
    public int? UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Tabla { get; set; }
    public int? RegistroId { get; set; }
    public string? Detalle { get; set; }
    public string? Ip { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}

public class HistorialDocumentoEntity
{
    public int Id { get; set; }
    public int DocumentoId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? EstadoAnterior { get; set; }
    public string? EstadoNuevo { get; set; }
    public DateOnly? VencAnterior { get; set; }
    public DateOnly? VencNuevo { get; set; }
    public int? UsuarioId { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public Documento? Documento { get; set; }
}

public class CostoGastoEntity
{
    public int Id { get; set; }
    public int AsociacionId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public decimal Valor { get; set; }
    public string? Proveedor { get; set; }
    public string? NumeroSoporte { get; set; }
    public string? ArchivoNombre { get; set; }
    public string? ArchivoRuta { get; set; }
    public int? RegistradoPor { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public AsociacionEntity? Asociacion { get; set; }
}

public class ProductoEntity
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = "Kilos";
    public string Categoria { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public class VentaEntity
{
    public int Id { get; set; }
    public int AsociacionId { get; set; }
    public int? AsociadoId { get; set; }
    public DateOnly FechaVenta { get; set; }
    public string? Comprador { get; set; }
    public decimal ValorTotal { get; set; }
    public string EstadoDespacho { get; set; } = "Pendiente";
    public DateOnly? FechaDespacho { get; set; }
    public string EstadoPago { get; set; } = "Pendiente";
    public decimal ValorPagado { get; set; }
    public DateOnly? FechaPago { get; set; }
    public string? Observaciones { get; set; }
    public int? RegistradoPor { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public AsociacionEntity? Asociacion { get; set; }
    public Asociado? Asociado { get; set; }
    public List<VentaDetalleEntity> Detalles { get; set; } = new();
}

public class VentaDetalleEntity
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int ProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal? ValorTotal { get; set; }

    public VentaEntity? Venta { get; set; }
    public ProductoEntity? Producto { get; set; }
}

public class VistaAlertaVencimiento
{
    public int? DocumentoId { get; set; }
    public int? AsociacionId { get; set; }
    public string? Asociacion { get; set; }
    public int? AsociadoId { get; set; }
    public string? TipoDocumento { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public int? DiasRestantes { get; set; }
    public string? Semaforo { get; set; }
}

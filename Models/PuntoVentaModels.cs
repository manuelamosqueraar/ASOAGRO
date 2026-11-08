using System;
using System.Collections.Generic;

namespace Asoagro.Models;

public class ProductoPdv
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Plátano";
    public string AsociacionOrigen { get; set; } = "ASOAGRO - Asociación Agropecuaria de Urabá";
    public string UnidadMedida { get; set; } = "Kilos";
    public decimal PrecioUnitario { get; set; }
    public decimal CostoPromedio { get; set; }
    public DateTime? VigenciaPrecio { get; set; }
    public decimal StockDisponible { get; set; }
    public decimal StockMinimo { get; set; } = 10;
    public bool Activo { get; set; } = true;
    public string? Descripcion { get; set; }
}

public class ItemVentaPdv
{
    public Guid ProductoId { get; set; }
    public string CodigoProducto { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string AsociacionOrigen { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = "Kilos";
    public decimal Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Subtotal => Math.Max(0, (Cantidad * PrecioUnitario) - Descuento);
}

public class VentaPdv
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NumeroTicket { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string ClienteNombre { get; set; } = "Cliente de Mostrador";
    public string ClienteDocumento { get; set; } = "222222222222";
    public string ClienteTelefono { get; set; } = string.Empty;
    public string MetodoPago { get; set; } = "Efectivo"; // "Efectivo", "Transferencia", "Tarjeta", "Crédito Solidario"
    public string AsociacionOrigenPrincipal { get; set; } = "ASOAGRO - Asociación Agropecuaria de Urabá";
    public decimal Total { get; set; }
    public decimal DescuentoAutorizado { get; set; }
    public string? MotivoDescuento { get; set; }
    public string? AutorizadoPor { get; set; }
    public decimal MontoRecibido { get; set; }
    public decimal Cambio => MontoRecibido > Total ? MontoRecibido - Total : 0;
    public string AtendidoPor { get; set; } = "Vendedor FAAUA";
    public string Estado { get; set; } = "Completada"; // "Completada", "Anulada"
    public string? MotivoAnulacion { get; set; }
    public string? SoportePagoNombre { get; set; }
    public byte[]? SoportePagoContenido { get; set; }
    public List<ItemVentaPdv> Items { get; set; } = new();
}

public class RecepcionProductoPdv
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductoId { get; set; }
    public string CodigoProducto { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public string AsociacionOrigen { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = "Kilos";
    public DateTime FechaRecepcion { get; set; } = DateTime.Now;
    public string ProveedorNombre { get; set; } = string.Empty; // Finca o productor
    public decimal CostoUnitario { get; set; }
    public decimal CostoTotal => Cantidad * CostoUnitario;
    public string? NumeroComprobante { get; set; } // Remisión o Factura
    public string? SoporteNombre { get; set; }
    public byte[]? SoporteContenido { get; set; }
    public string? Observaciones { get; set; }
    public string RegistradoPor { get; set; } = "Administrador PDV";
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}

public class CierreCajaPdv
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime FechaCierre { get; set; } = DateTime.Now;
    public string Turno { get; set; } = "Jornada Completa"; // "Mañana", "Tarde", "Jornada Completa"
    public string Responsable { get; set; } = "Administrador PDV";
    public decimal TotalVentasEfectivo { get; set; }
    public decimal TotalVentasTransferencia { get; set; }
    public decimal TotalVentasTarjeta { get; set; }
    public decimal TotalVentasGeneral { get; set; }
    public int CantidadVentas { get; set; }
    public decimal EfectivoEsperado { get; set; }
    public decimal EfectivoContado { get; set; }
    public decimal Diferencia => EfectivoContado - EfectivoEsperado;
    public string? Observaciones { get; set; }
    public string Estado => Math.Abs(Diferencia) < 0.01m ? "Cuadrado" : (Diferencia < 0 ? "Diferencia Faltante" : "Diferencia Sobrante");
}

public class ResumenPdv
{
    public decimal TotalVentasHoy { get; set; }
    public int CantidadTransaccionesHoy { get; set; }
    public decimal TotalEfectivoHoy { get; set; }
    public decimal TotalTransferenciasHoy { get; set; }
    public decimal TotalMes { get; set; }
    public int TotalProductos { get; set; }
    public int ProductosBajoStock { get; set; }
    public decimal TotalCostoInventario { get; set; }
}

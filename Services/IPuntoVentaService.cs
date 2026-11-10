using System;
using System.Collections.Generic;
using Asoagro.Models;

namespace Asoagro.Services;

public interface IPuntoVentaService
{
    IReadOnlyList<ProductoPdv> ObtenerProductos(string? asociacion = null, string? categoria = null, bool incluirInactivos = true);
    ProductoPdv? ObtenerProductoPorId(Guid id);
    void GuardarProducto(ProductoPdv producto);
    bool RegistrarProducto(ProductoPdv producto, string usuario, string rol, out string mensajeError);
    bool ActualizarProducto(ProductoPdv producto, string usuario, string rol, out string mensajeError);
    bool CambiarEstadoProducto(Guid productoId, string nuevoEstado, string usuario, string rol, out string mensajeError);
    bool ActualizarStock(Guid productoId, decimal nuevoStock);
    bool AjustarInventario(Guid productoId, decimal nuevoStock, string motivo, string autorizadoPor, string usuario);
    IReadOnlyList<AjusteInventarioPdv> ObtenerAjustesInventario(Guid? productoId = null);
    IReadOnlyList<MovimientoKardexPdv> ObtenerKardexProducto(Guid productoId);
    IReadOnlyList<MovimientoKardexPdv> ObtenerKardexGeneral(DateTime? desde = null, DateTime? hasta = null, Guid? productoId = null, string? asociacion = null);

    ResultadoRegistroVenta RegistrarVenta(VentaPdv venta, string usuario, string rol);
    bool AnularVenta(Guid ventaId, string motivo, string usuario, string rol);
    IReadOnlyList<VentaPdv> ObtenerVentas(string? asociacion = null, DateTime? desde = null, DateTime? hasta = null);

    ResultadoRegistroVenta RegistrarRecepcion(RecepcionProductoPdv recepcion, string usuario, string rol);
    IReadOnlyList<RecepcionProductoPdv> ObtenerRecepciones(string? asociacion = null);

    bool RegistrarCierreCaja(CierreCajaPdv cierre, string usuario, string rol);
    IReadOnlyList<CierreCajaPdv> ObtenerCierresCaja();

    ResumenPdv ObtenerResumen(string? asociacion = null);
}

public class ResultadoRegistroVenta
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public VentaPdv? Venta { get; set; }

    public static ResultadoRegistroVenta Ok(VentaPdv v, string msg) =>
        new() { Exitoso = true, Venta = v, Mensaje = msg };

    public static ResultadoRegistroVenta Error(string msg) =>
        new() { Exitoso = false, Mensaje = msg };
}

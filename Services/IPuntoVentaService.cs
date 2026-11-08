using System;
using System.Collections.Generic;
using Asoagro.Models;

namespace Asoagro.Services;

public interface IPuntoVentaService
{
    IReadOnlyList<ProductoPdv> ObtenerProductos(string? asociacion = null, string? categoria = null);
    ProductoPdv? ObtenerProductoPorId(Guid id);
    void GuardarProducto(ProductoPdv producto);
    bool ActualizarStock(Guid productoId, decimal nuevoStock);
    bool AjustarInventario(Guid productoId, decimal nuevoStock, string motivo, string autorizadoPor, string usuario);

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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Asoagro.Models;

namespace Asoagro.Services;

public class InMemoryPuntoVentaService : IPuntoVentaService
{
    private static readonly ConcurrentDictionary<Guid, ProductoPdv> Productos = new();
    private static readonly ConcurrentDictionary<Guid, VentaPdv> Ventas = new();
    private static readonly ConcurrentDictionary<Guid, RecepcionProductoPdv> Recepciones = new();
    private static readonly ConcurrentDictionary<Guid, CierreCajaPdv> CierresCaja = new();
    private static readonly object SyncLock = new();
    private static int SiguienteConsecutivo = 1001;

    static InMemoryPuntoVentaService()
    {
        // Productos del catálogo federado ASOAGRO
        var catalogo = new List<ProductoPdv>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Codigo = "PLA-EXP-01",
                Nombre = "Plátano Hartón Verde Tipo Exportación",
                Categoria = "Plátano",
                AsociacionOrigen = "ASOAGRO - Asociación Agropecuaria de Urabá",
                UnidadMedida = "Kilos",
                PrecioUnitario = 2400m,
                CostoPromedio = 1750m,
                VigenciaPrecio = DateTime.Today.AddMonths(2),
                StockDisponible = 1500m,
                StockMinimo = 200m,
                Descripcion = "Plátano seleccionado de primera calidad con calibración óptima para exportación y mercado mayorista."
            },
            new()
            {
                Id = Guid.NewGuid(),
                Codigo = "PLA-MAD-02",
                Nombre = "Plátano Maduro de Primera (Consumo Nacional)",
                Categoria = "Plátano",
                AsociacionOrigen = "ASOPLÁTANO - Asociación de Productores de Plátano de Urabá",
                UnidadMedida = "Cajas (20 kg)",
                PrecioUnitario = 42000m,
                CostoPromedio = 31000m,
                VigenciaPrecio = DateTime.Today.AddMonths(1),
                StockDisponible = 80m,
                StockMinimo = 15m,
                Descripcion = "Cajas de plátano en sazón maduro empacado en finca para distribución a tiendas y cadenas."
            },
            new()
            {
                Id = Guid.NewGuid(),
                Codigo = "CAC-GRA-01",
                Nombre = "Cacao Fino de Aroma en Grano Fermentado y Seco",
                Categoria = "Cacao",
                AsociacionOrigen = "ASOCACAO - Asociación de Cacaoteros de Urabá",
                UnidadMedida = "Kilos",
                PrecioUnitario = 16500m,
                CostoPromedio = 13200m,
                VigenciaPrecio = DateTime.Today.AddMonths(3),
                StockDisponible = 620m,
                StockMinimo = 100m,
                Descripcion = "Grano de cacao con fermentación superior al 80% y humedad controlada del 7%."
            },
            new()
            {
                Id = Guid.NewGuid(),
                Codigo = "MIE-ART-01",
                Nombre = "Miel de Abejas Pura Artesanal",
                Categoria = "Apicultura",
                AsociacionOrigen = "ASOPROMAGRO - Asociación de Productores Agropecuarios de Apartadó",
                UnidadMedida = "Frascos (500 g)",
                PrecioUnitario = 18000m,
                CostoPromedio = 12500m,
                VigenciaPrecio = DateTime.Today.AddMonths(6),
                StockDisponible = 45m,
                StockMinimo = 10m,
                Descripcion = "Miel multifloral 100% natural cosechada en colmenas de bosque protegido en Urabá."
            },
            new()
            {
                Id = Guid.NewGuid(),
                Codigo = "MAR-FRP-01",
                Nombre = "Maracuyá Fresco Seleccionado",
                Categoria = "Frutas",
                AsociacionOrigen = "COAGROURABÁ - Cooperativa Agropecuaria de Urabá",
                UnidadMedida = "Kilos",
                PrecioUnitario = 4800m,
                CostoPromedio = 3400m,
                VigenciaPrecio = DateTime.Today.AddMonths(1),
                StockDisponible = 340m,
                StockMinimo = 50m,
                Descripcion = "Fruta de maracuyá madura con alto rendimiento de pulpa y grados Brix certificados."
            },
            new()
            {
                Id = Guid.NewGuid(),
                Codigo = "ABO-ORG-01",
                Nombre = "Abono Orgánico Compostado (Enmienda de Suelos)",
                Categoria = "Insumos",
                AsociacionOrigen = "ASOGANADEROS - Asociación de Ganaderos y Productores de Urabá",
                UnidadMedida = "Bultos (50 kg)",
                PrecioUnitario = 28000m,
                CostoPromedio = 19500m,
                VigenciaPrecio = DateTime.Today.AddMonths(4),
                StockDisponible = 120m,
                StockMinimo = 20m,
                Descripcion = "Compost pecuario enriquecido con microorganismos benéficos para recuperación de suelos."
            }
        };

        foreach (var p in catalogo)
        {
            Productos[p.Id] = p;
        }

        // Semilla de recepciones de cosecha
        var p1 = catalogo[0];
        var p2 = catalogo[1];
        var p3 = catalogo[2];
        var fechaReciente = DateTime.Today.AddDays(-2);

        var r1 = new RecepcionProductoPdv
        {
            Id = Guid.NewGuid(),
            ProductoId = p1.Id,
            CodigoProducto = p1.Codigo,
            ProductoNombre = p1.Nombre,
            AsociacionOrigen = p1.AsociacionOrigen,
            Cantidad = 500m,
            UnidadMedida = p1.UnidadMedida,
            FechaRecepcion = fechaReciente,
            ProveedorNombre = "Finca La Esperanza - Carlos Julio Ramírez",
            CostoUnitario = p1.CostoPromedio,
            NumeroComprobante = "REM-0452-ASO",
            SoporteNombre = "Remision_Cosecha_Esperanza.pdf",
            Observaciones = "Cosecha entregada en punto de acopio con calibración óptima y pesaje certificado.",
            RegistradoPor = "Administrador PDV FAAUA",
            FechaRegistro = fechaReciente.AddHours(8)
        };

        var r2 = new RecepcionProductoPdv
        {
            Id = Guid.NewGuid(),
            ProductoId = p3.Id,
            CodigoProducto = p3.Codigo,
            ProductoNombre = p3.Nombre,
            AsociacionOrigen = p3.AsociacionOrigen,
            Cantidad = 250m,
            UnidadMedida = p3.UnidadMedida,
            FechaRecepcion = fechaReciente.AddDays(1),
            ProveedorNombre = "Cooperativa Cacaotera - Lote San Pedro",
            CostoUnitario = p3.CostoPromedio,
            NumeroComprobante = "FAC-PROV-9982",
            SoporteNombre = "Factura_Proveedor_Cacao_9982.pdf",
            Observaciones = "Grano fermentado con constancia de humedad al 7% y certificación de origen.",
            RegistradoPor = "Administrador PDV FAAUA",
            FechaRegistro = fechaReciente.AddDays(1).AddHours(9)
        };

        Recepciones[r1.Id] = r1;
        Recepciones[r2.Id] = r2;

        // Semilla de ventas recientes para arqueo de caja y reportes
        var hoy = DateTime.Today;

        var v1 = new VentaPdv
        {
            Id = Guid.NewGuid(),
            NumeroTicket = "TKT-1001",
            Fecha = hoy.AddHours(8).AddMinutes(30),
            ClienteNombre = "Distribuidora La Cosecha Urabá",
            ClienteDocumento = "900876543-1",
            ClienteTelefono = "3128904567",
            MetodoPago = "Transferencia",
            AsociacionOrigenPrincipal = p1.AsociacionOrigen,
            Total = 480000m,
            MontoRecibido = 480000m,
            AtendidoPor = "Carlos Mostrador FAAUA",
            Estado = "Completada",
            SoportePagoNombre = "Comprobante_Bancolombia_Trf_1001.pdf",
            Items = new()
            {
                new()
                {
                    ProductoId = p1.Id,
                    CodigoProducto = p1.Codigo,
                    NombreProducto = p1.Nombre,
                    Categoria = p1.Categoria,
                    AsociacionOrigen = p1.AsociacionOrigen,
                    UnidadMedida = p1.UnidadMedida,
                    Cantidad = 200m,
                    PrecioUnitario = p1.PrecioUnitario
                }
            }
        };

        var v2 = new VentaPdv
        {
            Id = Guid.NewGuid(),
            NumeroTicket = "TKT-1002",
            Fecha = hoy.AddHours(10).AddMinutes(15),
            ClienteNombre = "María Consuelo Gómez",
            ClienteDocumento = "32456789",
            ClienteTelefono = "3104561234",
            MetodoPago = "Efectivo",
            AsociacionOrigenPrincipal = p2.AsociacionOrigen,
            Total = 84000m,
            MontoRecibido = 100000m,
            AtendidoPor = "Carlos Mostrador FAAUA",
            Estado = "Completada",
            Items = new()
            {
                new()
                {
                    ProductoId = p2.Id,
                    CodigoProducto = p2.Codigo,
                    NombreProducto = p2.Nombre,
                    Categoria = p2.Categoria,
                    AsociacionOrigen = p2.AsociacionOrigen,
                    UnidadMedida = p2.UnidadMedida,
                    Cantidad = 2m,
                    PrecioUnitario = p2.PrecioUnitario
                }
            }
        };

        var v3 = new VentaPdv
        {
            Id = Guid.NewGuid(),
            NumeroTicket = "TKT-1003",
            Fecha = hoy.AddHours(12).AddMinutes(45),
            ClienteNombre = "Chocolates del Darién SAS",
            ClienteDocumento = "901234567-8",
            ClienteTelefono = "3189004523",
            MetodoPago = "Transferencia",
            AsociacionOrigenPrincipal = p3.AsociacionOrigen,
            Total = 495000m,
            MontoRecibido = 495000m,
            AtendidoPor = "Carlos Mostrador FAAUA",
            Estado = "Completada",
            SoportePagoNombre = "Transferencia_Chocolates_Darien.pdf",
            Items = new()
            {
                new()
                {
                    ProductoId = p3.Id,
                    CodigoProducto = p3.Codigo,
                    NombreProducto = p3.Nombre,
                    Categoria = p3.Categoria,
                    AsociacionOrigen = p3.AsociacionOrigen,
                    UnidadMedida = p3.UnidadMedida,
                    Cantidad = 30m,
                    PrecioUnitario = p3.PrecioUnitario
                }
            }
        };

        Ventas[v1.Id] = v1;
        Ventas[v2.Id] = v2;
        Ventas[v3.Id] = v3;
        SiguienteConsecutivo = 1004;

        // Semilla de cierre de caja previo
        var cierreAyer = new CierreCajaPdv
        {
            Id = Guid.NewGuid(),
            FechaCierre = hoy.AddDays(-1).AddHours(18),
            Turno = "Jornada Completa",
            Responsable = "Carlos Mostrador FAAUA",
            TotalVentasEfectivo = 320000m,
            TotalVentasTransferencia = 890000m,
            TotalVentasGeneral = 1210000m,
            CantidadVentas = 5,
            EfectivoEsperado = 320000m,
            EfectivoContado = 320000m,
            Observaciones = "Arqueo de caja conforme. Billetes y monedas verificados sin novedad."
        };
        CierresCaja[cierreAyer.Id] = cierreAyer;
    }

    public IReadOnlyList<ProductoPdv> ObtenerProductos(string? asociacion = null, string? categoria = null)
    {
        var q = Productos.Values.Where(p => p.Activo).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(asociacion))
        {
            q = q.Where(p => string.Equals(p.AsociacionOrigen, asociacion, StringComparison.OrdinalIgnoreCase) ||
                             p.AsociacionOrigen.Contains(asociacion, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            q = q.Where(p => string.Equals(p.Categoria, categoria, StringComparison.OrdinalIgnoreCase));
        }

        return q.OrderBy(p => p.Nombre).ToList();
    }

    public ProductoPdv? ObtenerProductoPorId(Guid id)
    {
        Productos.TryGetValue(id, out var p);
        return p;
    }

    public void GuardarProducto(ProductoPdv producto)
    {
        if (producto.Id == Guid.Empty)
        {
            producto.Id = Guid.NewGuid();
        }
        Productos[producto.Id] = producto;
    }

    public bool ActualizarStock(Guid productoId, decimal nuevoStock)
    {
        if (Productos.TryGetValue(productoId, out var prod))
        {
            prod.StockDisponible = Math.Max(0, nuevoStock);
            return true;
        }
        return false;
    }

    public bool AjustarInventario(Guid productoId, decimal nuevoStock, string motivo, string autorizadoPor, string usuario)
    {
        lock (SyncLock)
        {
            if (Productos.TryGetValue(productoId, out var prod))
            {
                var stockAnterior = prod.StockDisponible;
                prod.StockDisponible = Math.Max(0, nuevoStock);

                // Registramos auditoría en la descripción o historial del producto
                prod.Descripcion = $"{prod.Descripcion} [Ajuste inventario {DateTime.Now:dd/MM/yyyy HH:mm}: de {stockAnterior} a {nuevoStock} {prod.UnidadMedida}. Motivo: {motivo}. Autorizado por: {autorizadoPor}. Operador: {usuario}]";
                return true;
            }
            return false;
        }
    }

    public ResultadoRegistroVenta RegistrarVenta(VentaPdv venta, string usuario, string rol)
    {
        if (venta.Items == null || venta.Items.Count == 0)
        {
            return ResultadoRegistroVenta.Error("No se pueden registrar ventas sin artículos en el carrito.");
        }

        lock (SyncLock)
        {
            // Validar stock disponible
            foreach (var item in venta.Items)
            {
                if (Productos.TryGetValue(item.ProductoId, out var prod))
                {
                    if (prod.StockDisponible < item.Cantidad)
                    {
                        return ResultadoRegistroVenta.Error($"Stock insuficiente para {prod.Nombre}. Disponible: {prod.StockDisponible} {prod.UnidadMedida}, requerido: {item.Cantidad}.");
                    }
                }
            }

            // Descontar inventario
            foreach (var item in venta.Items)
            {
                if (Productos.TryGetValue(item.ProductoId, out var prod))
                {
                    prod.StockDisponible -= item.Cantidad;
                }
            }

            if (venta.Id == Guid.Empty) venta.Id = Guid.NewGuid();
            venta.NumeroTicket = $"TKT-{SiguienteConsecutivo++}";
            venta.Fecha = DateTime.Now;
            venta.AtendidoPor = usuario;
            venta.Estado = "Completada";
            venta.Total = venta.Items.Sum(i => i.Subtotal);

            if (venta.MontoRecibido < venta.Total && venta.MetodoPago == "Efectivo")
            {
                venta.MontoRecibido = venta.Total;
            }

            if (string.IsNullOrWhiteSpace(venta.AsociacionOrigenPrincipal) && venta.Items.Count > 0)
            {
                venta.AsociacionOrigenPrincipal = venta.Items[0].AsociacionOrigen;
            }

            Ventas[venta.Id] = venta;
            return ResultadoRegistroVenta.Ok(venta, $"Venta registrada exitosamente con ticket {venta.NumeroTicket}.");
        }
    }

    public bool AnularVenta(Guid ventaId, string motivo, string usuario, string rol)
    {
        lock (SyncLock)
        {
            if (!Ventas.TryGetValue(ventaId, out var venta))
            {
                return false;
            }

            if (venta.Estado == "Anulada")
            {
                return false;
            }

            venta.Estado = "Anulada";
            venta.MotivoAnulacion = $"{motivo} (Anulado por {usuario} - {rol} el {DateTime.Now:dd/MM/yyyy HH:mm})";

            // Reintegrar inventario
            foreach (var item in venta.Items)
            {
                if (Productos.TryGetValue(item.ProductoId, out var prod))
                {
                    prod.StockDisponible += item.Cantidad;
                }
            }

            return true;
        }
    }

    public IReadOnlyList<VentaPdv> ObtenerVentas(string? asociacion = null, DateTime? desde = null, DateTime? hasta = null)
    {
        var q = Ventas.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(asociacion))
        {
            q = q.Where(v => string.Equals(v.AsociacionOrigenPrincipal, asociacion, StringComparison.OrdinalIgnoreCase) ||
                             v.Items.Any(i => string.Equals(i.AsociacionOrigen, asociacion, StringComparison.OrdinalIgnoreCase)));
        }

        if (desde.HasValue)
        {
            q = q.Where(v => v.Fecha.Date >= desde.Value.Date);
        }

        if (hasta.HasValue)
        {
            q = q.Where(v => v.Fecha.Date <= hasta.Value.Date);
        }

        return q.OrderByDescending(v => v.Fecha).ToList();
    }

    public ResultadoRegistroVenta RegistrarRecepcion(RecepcionProductoPdv recepcion, string usuario, string rol)
    {
        if (recepcion.Cantidad <= 0)
        {
            return ResultadoRegistroVenta.Error("La cantidad recibida debe ser mayor a cero.");
        }

        lock (SyncLock)
        {
            if (recepcion.Id == Guid.Empty) recepcion.Id = Guid.NewGuid();
            recepcion.RegistradoPor = usuario;
            recepcion.FechaRegistro = DateTime.Now;

            // Incrementar inventario del producto
            if (Productos.TryGetValue(recepcion.ProductoId, out var prod))
            {
                prod.StockDisponible += recepcion.Cantidad;
                recepcion.ProductoNombre = prod.Nombre;
                recepcion.CodigoProducto = prod.Codigo;
                recepcion.UnidadMedida = prod.UnidadMedida;
                recepcion.AsociacionOrigen = prod.AsociacionOrigen;

                // Actualizar costo promedio ponderado si aplica
                if (recepcion.CostoUnitario > 0 && prod.StockDisponible > 0)
                {
                    var costoTotalPrevio = (prod.StockDisponible - recepcion.Cantidad) * prod.CostoPromedio;
                    var costoNuevoLote = recepcion.Cantidad * recepcion.CostoUnitario;
                    prod.CostoPromedio = Math.Round((costoTotalPrevio + costoNuevoLote) / prod.StockDisponible, 2);
                }
            }

            Recepciones[recepcion.Id] = recepcion;
            return ResultadoRegistroVenta.Ok(new VentaPdv(), $"Recepción de {recepcion.Cantidad} {recepcion.UnidadMedida} de {recepcion.ProductoNombre} registrada en inventario correctamente.");
        }
    }

    public IReadOnlyList<RecepcionProductoPdv> ObtenerRecepciones(string? asociacion = null)
    {
        var q = Recepciones.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(asociacion))
        {
            q = q.Where(r => string.Equals(r.AsociacionOrigen, asociacion, StringComparison.OrdinalIgnoreCase) ||
                             r.AsociacionOrigen.Contains(asociacion, StringComparison.OrdinalIgnoreCase));
        }

        return q.OrderByDescending(r => r.FechaRecepcion).ToList();
    }

    public bool RegistrarCierreCaja(CierreCajaPdv cierre, string usuario, string rol)
    {
        lock (SyncLock)
        {
            if (cierre.Id == Guid.Empty) cierre.Id = Guid.NewGuid();
            cierre.FechaCierre = DateTime.Now;
            cierre.Responsable = usuario;

            CierresCaja[cierre.Id] = cierre;
            return true;
        }
    }

    public IReadOnlyList<CierreCajaPdv> ObtenerCierresCaja()
    {
        return CierresCaja.Values.OrderByDescending(c => c.FechaCierre).ToList();
    }

    public ResumenPdv ObtenerResumen(string? asociacion = null)
    {
        var hoy = DateTime.Today;
        var primerDiaMes = new DateTime(hoy.Year, hoy.Month, 1);

        var ventasFiltradas = ObtenerVentas(asociacion);
        var ventasCompletadas = ventasFiltradas.Where(v => v.Estado == "Completada").ToList();

        var ventasHoy = ventasCompletadas.Where(v => v.Fecha.Date == hoy).ToList();
        var ventasMes = ventasCompletadas.Where(v => v.Fecha.Date >= primerDiaMes).ToList();

        var productos = ObtenerProductos(asociacion);

        return new ResumenPdv
        {
            TotalVentasHoy = ventasHoy.Sum(v => v.Total),
            CantidadTransaccionesHoy = ventasHoy.Count,
            TotalEfectivoHoy = ventasHoy.Where(v => v.MetodoPago == "Efectivo").Sum(v => v.Total),
            TotalTransferenciasHoy = ventasHoy.Where(v => v.MetodoPago == "Transferencia").Sum(v => v.Total),
            TotalMes = ventasMes.Sum(v => v.Total),
            TotalProductos = productos.Count,
            ProductosBajoStock = productos.Count(p => p.StockDisponible <= p.StockMinimo),
            TotalCostoInventario = productos.Sum(p => p.StockDisponible * p.CostoPromedio)
        };
    }
}

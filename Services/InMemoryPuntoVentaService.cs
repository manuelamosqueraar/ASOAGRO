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
    private static readonly ConcurrentDictionary<Guid, AjusteInventarioPdv> AjustesInventario = new();
    private static readonly object SyncLock = new();
    private static int SiguienteConsecutivo = 1004;
    private static int SiguienteConsecutivoRecepcion = 1003;
    private static int SiguienteConsecutivoAnulacion = 1001;

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
            ConsecutivoRecepcion = "REC-1001",
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
            EstadoRecepcion = "Ingresado al inventario",
            SoporteNombre = "Remision_Cosecha_Esperanza.pdf",
            Observaciones = "Cosecha entregada en punto de acopio con calibración óptima y pesaje certificado.",
            RegistradoPor = "Administrador PDV FAAUA",
            FechaRegistro = fechaReciente.AddHours(8)
        };

        var r2 = new RecepcionProductoPdv
        {
            Id = Guid.NewGuid(),
            ConsecutivoRecepcion = "REC-1002",
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
            EstadoRecepcion = "Ingresado al inventario",
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

    public IReadOnlyList<ProductoPdv> ObtenerProductos(string? asociacion = null, string? categoria = null, bool incluirInactivos = true)
    {
        var q = Productos.Values.AsEnumerable();

        if (!incluirInactivos)
        {
            q = q.Where(p => p.Activo && p.Estado != "Inactivo" && p.Estado != "Descontinuado");
        }

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
        ActualizarEstadoStock(producto);
        Productos[producto.Id] = producto;
    }

    public bool RegistrarProducto(ProductoPdv producto, string usuario, string rol, out string mensajeError)
    {
        mensajeError = string.Empty;

        if (string.IsNullOrWhiteSpace(producto.Nombre))
        {
            mensajeError = "El nombre del producto es obligatorio.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(producto.Codigo))
        {
            mensajeError = "El código único del producto es obligatorio.";
            return false;
        }

        var codigoLimpio = producto.Codigo.Trim();
        if (Productos.Values.Any(p => string.Equals(p.Codigo.Trim(), codigoLimpio, StringComparison.OrdinalIgnoreCase)))
        {
            mensajeError = $"El código '{codigoLimpio}' ya se encuentra asignado a otro producto en el catálogo.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(producto.Categoria))
        {
            mensajeError = "Debe seleccionar una categoría para el producto.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(producto.UnidadMedida))
        {
            mensajeError = "Debe especificar la unidad de venta o presentación.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(producto.AsociacionOrigen))
        {
            mensajeError = "Debe asociar el producto a una asociación proveedora registrada.";
            return false;
        }

        if (producto.PrecioUnitario < 0)
        {
            mensajeError = "El precio de venta en COP no puede ser negativo.";
            return false;
        }

        if (producto.CostoPromedio < 0)
        {
            mensajeError = "El costo de adquisición en COP no puede ser negativo.";
            return false;
        }

        if (producto.StockDisponible < 0)
        {
            mensajeError = "La cantidad en stock no puede ser negativa.";
            return false;
        }

        if (producto.StockMinimo < 0)
        {
            producto.StockMinimo = 0;
        }

        lock (SyncLock)
        {
            if (producto.Id == Guid.Empty) producto.Id = Guid.NewGuid();
            producto.Codigo = codigoLimpio.ToUpperInvariant();
            producto.FechaRegistro = DateTime.Now;
            producto.RegistradoPor = usuario;
            ActualizarEstadoStock(producto);

            Productos[producto.Id] = producto;

            // Si se ingresó con inventario inicial mayor a cero, registrar el movimiento de apertura en el Kardex
            if (producto.StockDisponible > 0)
            {
                var ajusteApertura = new AjusteInventarioPdv
                {
                    Id = Guid.NewGuid(),
                    ProductoId = producto.Id,
                    CodigoProducto = producto.Codigo,
                    NombreProducto = producto.Nombre,
                    Fecha = DateTime.Now,
                    StockAnterior = 0,
                    StockNuevo = producto.StockDisponible,
                    Motivo = "Inventario inicial de apertura al registrar producto en catálogo.",
                    AutorizadoPor = usuario,
                    Usuario = usuario
                };
                AjustesInventario[ajusteApertura.Id] = ajusteApertura;
            }

            return true;
        }
    }

    public bool ActualizarProducto(ProductoPdv producto, string usuario, string rol, out string mensajeError)
    {
        mensajeError = string.Empty;

        if (!Productos.TryGetValue(producto.Id, out var actual))
        {
            mensajeError = "El producto no fue encontrado en el catálogo.";
            return false;
        }

        var codigoLimpio = producto.Codigo.Trim().ToUpperInvariant();
        if (Productos.Values.Any(p => p.Id != producto.Id && string.Equals(p.Codigo.Trim(), codigoLimpio, StringComparison.OrdinalIgnoreCase)))
        {
            mensajeError = $"El código '{codigoLimpio}' ya está en uso por otro producto.";
            return false;
        }

        lock (SyncLock)
        {
            actual.Nombre = producto.Nombre.Trim();
            actual.Codigo = codigoLimpio;
            actual.Categoria = producto.Categoria;
            actual.Descripcion = producto.Descripcion;
            actual.UnidadMedida = producto.UnidadMedida;
            actual.AsociacionOrigen = producto.AsociacionOrigen;
            actual.ProductorOrigen = producto.ProductorOrigen;
            actual.MunicipioProcedencia = producto.MunicipioProcedencia;
            actual.VeredaProcedencia = producto.VeredaProcedencia;
            actual.VariedadClasificacion = producto.VariedadClasificacion;
            actual.NumeroLote = producto.NumeroLote;
            actual.FechaCosecha = producto.FechaCosecha;
            actual.FechaRecepcion = producto.FechaRecepcion;
            actual.PrecioUnitario = Math.Max(0, producto.PrecioUnitario);
            actual.CostoPromedio = Math.Max(0, producto.CostoPromedio);
            actual.VigenciaPrecio = producto.VigenciaPrecio;
            actual.StockMinimo = Math.Max(0, producto.StockMinimo);
            actual.StockReservado = Math.Max(0, producto.StockReservado);
            actual.Estado = producto.Estado;
            actual.Activo = producto.Estado != "Inactivo" && producto.Estado != "Descontinuado";
            actual.ImagenUrl = producto.ImagenUrl;
            actual.Observaciones = producto.Observaciones;
            actual.SoporteNombre = producto.SoporteNombre ?? actual.SoporteNombre;

            ActualizarEstadoStock(actual);
            return true;
        }
    }

    public bool CambiarEstadoProducto(Guid productoId, string nuevoEstado, string usuario, string rol, out string mensajeError)
    {
        mensajeError = string.Empty;
        if (!Productos.TryGetValue(productoId, out var prod))
        {
            mensajeError = "Producto no encontrado.";
            return false;
        }

        lock (SyncLock)
        {
            prod.Estado = nuevoEstado;
            prod.Activo = nuevoEstado != "Inactivo" && nuevoEstado != "Descontinuado";
            ActualizarEstadoStock(prod);
            return true;
        }
    }

    public bool ActualizarStock(Guid productoId, decimal nuevoStock)
    {
        if (Productos.TryGetValue(productoId, out var prod))
        {
            prod.StockDisponible = Math.Max(0, nuevoStock);
            ActualizarEstadoStock(prod);
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
                ActualizarEstadoStock(prod);

                var ajuste = new AjusteInventarioPdv
                {
                    Id = Guid.NewGuid(),
                    ProductoId = productoId,
                    CodigoProducto = prod.Codigo,
                    NombreProducto = prod.Nombre,
                    Fecha = DateTime.Now,
                    StockAnterior = stockAnterior,
                    StockNuevo = prod.StockDisponible,
                    Motivo = motivo.Trim(),
                    AutorizadoPor = autorizadoPor.Trim(),
                    Usuario = usuario.Trim()
                };

                AjustesInventario[ajuste.Id] = ajuste;
                return true;
            }
            return false;
        }
    }

    public IReadOnlyList<AjusteInventarioPdv> ObtenerAjustesInventario(Guid? productoId = null)
    {
        var q = AjustesInventario.Values.AsEnumerable();
        if (productoId.HasValue && productoId.Value != Guid.Empty)
        {
            q = q.Where(a => a.ProductoId == productoId.Value);
        }
        return q.OrderByDescending(a => a.Fecha).ToList();
    }

    public IReadOnlyList<MovimientoKardexPdv> ObtenerKardexProducto(Guid productoId)
    {
        return ObtenerKardexGeneral(productoId: productoId);
    }

    public IReadOnlyList<MovimientoKardexPdv> ObtenerKardexGeneral(DateTime? desde = null, DateTime? hasta = null, Guid? productoId = null, string? asociacion = null)
    {
        var lista = new List<MovimientoKardexPdv>();

        // 1. Recepciones
        var recepciones = Recepciones.Values.AsEnumerable();
        if (productoId.HasValue) recepciones = recepciones.Where(r => r.ProductoId == productoId.Value);
        if (!string.IsNullOrWhiteSpace(asociacion)) recepciones = recepciones.Where(r => string.Equals(r.AsociacionOrigen, asociacion, StringComparison.OrdinalIgnoreCase) || r.AsociacionOrigen.Contains(asociacion, StringComparison.OrdinalIgnoreCase));

        foreach (var r in recepciones)
        {
            Productos.TryGetValue(r.ProductoId, out var p);
            lista.Add(new MovimientoKardexPdv
            {
                Id = r.Id,
                Fecha = r.FechaRecepcion,
                ProductoId = r.ProductoId,
                CodigoProducto = r.CodigoProducto,
                NombreProducto = r.ProductoNombre,
                Categoria = p?.Categoria ?? "Cosechas",
                AsociacionOrigen = r.AsociacionOrigen,
                TipoMovimiento = "Recepción de Cosecha",
                DocumentoReferencia = string.IsNullOrWhiteSpace(r.ConsecutivoRecepcion) ? (r.NumeroComprobante ?? "REC-S/N") : r.ConsecutivoRecepcion,
                CantidadEntrada = r.Cantidad,
                CantidadSalida = 0,
                UnidadMedida = r.UnidadMedida,
                CostoOPrecioUnitario = r.CostoUnitario,
                Responsable = r.RegistradoPor,
                Observaciones = $"Proveedor: {r.ProveedorNombre}. Remisión: {r.NumeroComprobante ?? "N/A"}. {r.Observaciones}".Trim()
            });
        }

        // 2. Ventas
        var ventas = Ventas.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(asociacion)) ventas = ventas.Where(v => string.Equals(v.AsociacionOrigenPrincipal, asociacion, StringComparison.OrdinalIgnoreCase));

        foreach (var v in ventas)
        {
            foreach (var it in v.Items)
            {
                if (productoId.HasValue && it.ProductoId != productoId.Value) continue;

                if (v.Estado == "Completada")
                {
                    lista.Add(new MovimientoKardexPdv
                    {
                        Id = Guid.NewGuid(),
                        Fecha = v.Fecha,
                        ProductoId = it.ProductoId,
                        CodigoProducto = it.CodigoProducto,
                        NombreProducto = it.NombreProducto,
                        Categoria = it.Categoria,
                        AsociacionOrigen = it.AsociacionOrigen,
                        TipoMovimiento = "Venta en Mostrador",
                        DocumentoReferencia = v.NumeroTicket,
                        CantidadEntrada = 0,
                        CantidadSalida = it.Cantidad,
                        UnidadMedida = it.UnidadMedida,
                        CostoOPrecioUnitario = it.PrecioUnitario,
                        Responsable = v.AtendidoPor,
                        Observaciones = $"Venta mostrador a {v.ClienteNombre} (Doc: {v.ClienteDocumento}). Medio: {v.MetodoPago}"
                    });
                }
                else if (v.Estado == "Anulada")
                {
                    // Registro de venta original
                    lista.Add(new MovimientoKardexPdv
                    {
                        Id = Guid.NewGuid(),
                        Fecha = v.Fecha,
                        ProductoId = it.ProductoId,
                        CodigoProducto = it.CodigoProducto,
                        NombreProducto = it.NombreProducto,
                        Categoria = it.Categoria,
                        AsociacionOrigen = it.AsociacionOrigen,
                        TipoMovimiento = "Venta en Mostrador",
                        DocumentoReferencia = v.NumeroTicket,
                        CantidadEntrada = 0,
                        CantidadSalida = it.Cantidad,
                        UnidadMedida = it.UnidadMedida,
                        CostoOPrecioUnitario = it.PrecioUnitario,
                        Responsable = v.AtendidoPor,
                        Observaciones = $"Ticket original completado para {v.ClienteNombre}."
                    });

                    // Reingreso por anulación/devolución
                    lista.Add(new MovimientoKardexPdv
                    {
                        Id = Guid.NewGuid(),
                        Fecha = v.FechaAnulacion ?? v.Fecha.AddMinutes(5),
                        ProductoId = it.ProductoId,
                        CodigoProducto = it.CodigoProducto,
                        NombreProducto = it.NombreProducto,
                        Categoria = it.Categoria,
                        AsociacionOrigen = it.AsociacionOrigen,
                        TipoMovimiento = "Devolución / Anulación",
                        DocumentoReferencia = v.ConsecutivoAnulacion ?? $"ANUL-{v.NumeroTicket}",
                        CantidadEntrada = it.Cantidad,
                        CantidadSalida = 0,
                        UnidadMedida = it.UnidadMedida,
                        CostoOPrecioUnitario = it.PrecioUnitario,
                        Responsable = v.AnuladoPor ?? v.AtendidoPor,
                        Observaciones = $"Reintegro formal a inventario por devolución/anulación. Motivo: {v.MotivoAnulacion}"
                    });
                }
            }
        }

        // 3. Ajustes de inventario
        var ajustes = AjustesInventario.Values.AsEnumerable();
        if (productoId.HasValue) ajustes = ajustes.Where(a => a.ProductoId == productoId.Value);

        foreach (var a in ajustes)
        {
            Productos.TryGetValue(a.ProductoId, out var p);
            if (!string.IsNullOrWhiteSpace(asociacion) && p != null && !p.AsociacionOrigen.Contains(asociacion, StringComparison.OrdinalIgnoreCase)) continue;

            lista.Add(new MovimientoKardexPdv
            {
                Id = a.Id,
                Fecha = a.Fecha,
                ProductoId = a.ProductoId,
                CodigoProducto = a.CodigoProducto,
                NombreProducto = a.NombreProducto,
                Categoria = p?.Categoria ?? "Insumos",
                AsociacionOrigen = p?.AsociacionOrigen ?? "ASOAGRO",
                TipoMovimiento = "Ajuste de Inventario",
                DocumentoReferencia = $"AJU-{a.Fecha:yyMMdd}-{a.Id.ToString()[..4].ToUpper()}",
                CantidadEntrada = a.CantidadAjustada > 0 ? a.CantidadAjustada : 0,
                CantidadSalida = a.CantidadAjustada < 0 ? Math.Abs(a.CantidadAjustada) : 0,
                UnidadMedida = p?.UnidadMedida ?? "Unidades",
                CostoOPrecioUnitario = p?.CostoPromedio ?? 0,
                Responsable = a.Usuario,
                Observaciones = $"Ajuste físico: de {a.StockAnterior} a {a.StockNuevo}. Motivo: {a.Motivo}. Autorizado por: {a.AutorizadoPor}"
            });
        }

        // Filtro por fecha si aplica
        if (desde.HasValue) lista = lista.Where(m => m.Fecha.Date >= desde.Value.Date).ToList();
        if (hasta.HasValue) lista = lista.Where(m => m.Fecha.Date <= hasta.Value.Date).ToList();

        // Calcular saldo acumulado para orden cronológico si se consulta por producto específico
        if (productoId.HasValue)
        {
            var ordenadosAsc = lista.OrderBy(m => m.Fecha).ToList();
            decimal runningBalance = 0;
            foreach (var m in ordenadosAsc)
            {
                runningBalance += (m.CantidadEntrada - m.CantidadSalida);
                m.SaldoResultante = runningBalance;
            }
            return ordenadosAsc.OrderByDescending(m => m.Fecha).ToList();
        }

        return lista.OrderByDescending(m => m.Fecha).ToList();
    }

    private static void ActualizarEstadoStock(ProductoPdv prod)
    {
        if (prod.Estado == "Inactivo" || prod.Estado == "Descontinuado")
        {
            prod.Activo = false;
            return;
        }

        prod.Activo = true;
        if (prod.StockDisponible <= 0)
        {
            prod.Estado = "Agotado";
        }
        else if (prod.StockDisponible <= prod.StockMinimo)
        {
            prod.Estado = "Bajo Stock";
        }
        else
        {
            prod.Estado = "Disponible";
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

            // Validar coherencia de medios de pago
            if (venta.EsPagoMixto)
            {
                var sumaPagos = venta.PagosDetalle.Sum(p => p.Monto);
                if (Math.Abs(sumaPagos - venta.Total) > 0.01m)
                {
                    return ResultadoRegistroVenta.Error($"La suma de los medios de pago (${sumaPagos:N0} COP) no coincide con el total de la venta (${venta.Total:N0} COP).");
                }
                venta.MontoRecibido = venta.Total;
            }
            else
            {
                if (venta.MontoRecibido < venta.Total && venta.MetodoPago == "Efectivo")
                {
                    venta.MontoRecibido = venta.Total;
                }

                if (venta.PagosDetalle.Count == 0)
                {
                    venta.PagosDetalle.Add(new PagoDetallePdv
                    {
                        MetodoPago = venta.MetodoPago,
                        Monto = venta.Total,
                        Referencia = venta.ReferenciaPago
                    });
                }
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
            venta.FechaAnulacion = DateTime.Now;
            venta.AnuladoPor = usuario;
            venta.MotivoAnulacion = motivo;
            venta.ConsecutivoAnulacion = $"ANUL-{SiguienteConsecutivoAnulacion++}";

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
            if (string.IsNullOrWhiteSpace(recepcion.ConsecutivoRecepcion))
            {
                recepcion.ConsecutivoRecepcion = $"REC-{SiguienteConsecutivoRecepcion++}";
            }
            recepcion.EstadoRecepcion = "Ingresado al inventario";
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

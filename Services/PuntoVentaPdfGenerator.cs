using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Asoagro.Models;

namespace Asoagro.Services;

/// <summary>
/// Generador formal de comprobantes y recibos internos del Punto de Venta y Comercialización (PDF 1.4).
/// Usa nombres honestos y notas de soporte operativo sin pretender ser factura electrónica DIAN.
/// </summary>
public static class PuntoVentaPdfGenerator
{
    private static readonly CultureInfo CulturaCop = new("es-CO");

    /// <summary>
    /// Genera el Recibo Interno de Venta en mostrador.
    /// </summary>
    public static byte[] GenerarReciboVentaPdf(VentaPdv venta, string funcionarioResponsable = "Vendedor FAAUA")
    {
        var generadoEn = DateTime.Now;
        var ticket = string.IsNullOrWhiteSpace(venta.NumeroTicket) ? "TKT-S/N" : venta.NumeroTicket.Trim();
        var fechaOp = venta.Fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

        var ctx = new PdfInformeEngine.DocumentoInformeContexto
        {
            AreaInstitucional = "PUNTO DE VENTA SOLIDARIO · COMERCIALIZACIÓN AGROPECUARIA",
            TituloDocumento = "RECIBO INTERNO DE VENTA",
            SubtituloDocumento = $"Ticket No.: {ticket}  |  Fecha: {fechaOp}  |  Estado: {venta.Estado}  |  Medio: {venta.MetodoPago}",
            ReferenciaDerecha = $"Ticket: {ticket}",
            TituloCortoContinuacion = $"RECIBO INTERNO DE VENTA — TICKET {ticket}",
            NotaPieLinea1 = $"Soporte interno expedido por el sistema ASOAGRO el {generadoEn:dd/MM/yyyy a las HH:mm} · Atendido por: {ValorONoRegistrado(venta.AtendidoPor)}.",
            NotaPieLinea2 = "Documento de soporte interno para control de inventario y caja. No constituye factura electrónica ni documento equivalente DIAN conforme al Estatuto Tributario.",
            FechaGeneracion = generadoEn
        };

        ctx.IniciarDocumento();

        // SECCIÓN I: DATOS DE LA OPERACIÓN Y RESPONSABLE
        PdfInformeEngine.DibujarTituloSeccion(ctx, "I", "Datos Generales de la Operación y Responsables");

        var parrafoOp = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("El presente recibo certifica la transacción en mostrador solidario identificada con el ticket "),
            new(ticket, true),
            new(", registrada el "),
            new(fechaOp, true),
            new(" en el Punto de Venta de la Federación FAAUA - ASOAGRO. La operación fue atendida y registrada por "),
            new(ValorONoRegistrado(venta.AtendidoPor), true),
            new(", asociando como origen principal de los productos a "),
            new(ValorONoRegistrado(venta.AsociacionOrigenPrincipal), true),
            new(". El estado actual de la transacción es "),
            new(venta.Estado, true),
            new(".")
        };
        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoOp);

        // SECCIÓN II: DATOS DEL COMPRADOR O CLIENTE
        PdfInformeEngine.DibujarTituloSeccion(ctx, "II", "Identificación del Comprador o Cliente");

        var parrafoCliente = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("Cliente o adquirente: "),
            new(ValorONoRegistrado(venta.ClienteNombre), true),
            new("  |  Documento / NIT: "),
            new(ValorONoRegistrado(venta.ClienteDocumento), true),
            new("  |  Teléfono de contacto: "),
            new(ValorONoRegistrado(venta.ClienteTelefono), true),
            new(".")
        };
        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoCliente);

        // SECCIÓN III: DETALLE DE PRODUCTOS AGROPECUARIOS VENDIDOS
        PdfInformeEngine.DibujarTituloSeccion(ctx, "III", "Detalle de Productos, Cosechas y Precios");

        var columnasItems = new List<PdfInformeEngine.ColumnaTabla>
        {
            new() { Titulo = "Código", Ancho = 65.0 },
            new() { Titulo = "Producto Agropecuario", Ancho = 160.0 },
            new() { Titulo = "Cant.", Ancho = 45.0, AlinearDerecha = true },
            new() { Titulo = "Unidad", Ancho = 50.0 },
            new() { Titulo = "Precio Unit. COP", Ancho = 80.0, AlinearDerecha = true },
            new() { Titulo = "Desc. COP", Ancho = 55.0, AlinearDerecha = true },
            new() { Titulo = "Subtotal COP", Ancho = 80.0, AlinearDerecha = true }
        };

        var filasItems = new List<string[]>();
        foreach (var item in venta.Items)
        {
            filasItems.Add(new[]
            {
                ValorONoRegistrado(item.CodigoProducto),
                ValorONoRegistrado(item.NombreProducto),
                item.Cantidad.ToString("N0", CulturaCop),
                ValorONoRegistrado(item.UnidadMedida),
                $"${item.PrecioUnitario.ToString("N0", CulturaCop)}",
                item.Descuento > 0 ? $"${item.Descuento.ToString("N0", CulturaCop)}" : "$0",
                $"${item.Subtotal.ToString("N0", CulturaCop)}"
            });
        }

        PdfInformeEngine.DibujarTablaFormal(ctx, columnasItems, filasItems);

        // SECCIÓN IV: TOTALES Y CONDICIONES DE PAGO
        PdfInformeEngine.DibujarTituloSeccion(ctx, "IV", "Liquidación Total y Medios de Pago en COP");

        var subtotalBruto = venta.Items.Sum(i => i.Cantidad * i.PrecioUnitario);
        var descuentoTotal = venta.Items.Sum(i => i.Descuento) + venta.DescuentoAutorizado;
        var totalFinal = venta.Total;

        var parrafoTotales = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("Subtotal Bruto: "),
            new($"${subtotalBruto.ToString("N0", CulturaCop)} COP", true),
            new("  |  Descuentos Aplicados: "),
            new($"${descuentoTotal.ToString("N0", CulturaCop)} COP", true),
            new("  |  TOTAL NETO PAGADO: "),
            new($"${totalFinal.ToString("N0", CulturaCop)} COP", true),
            new(".")
        };
        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoTotales);

        var parrafoPagos = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("Medio(s) de pago registrado(s): "),
            new(venta.MetodoPago, true)
        };

        if (!string.IsNullOrWhiteSpace(venta.ReferenciaPago))
        {
            parrafoPagos.Add(new("  |  Referencia de comprobante / transacción: "));
            parrafoPagos.Add(new(venta.ReferenciaPago.Trim(), true));
        }

        if (venta.EsPagoMixto && venta.PagosDetalle.Count > 0)
        {
            parrafoPagos.Add(new("  |  Desglose mixto: "));
            parrafoPagos.Add(new(string.Join("; ", venta.PagosDetalle.Select(p => $"{p.MetodoPago}: ${p.Monto:N0} COP" + (!string.IsNullOrWhiteSpace(p.Referencia) ? $" (Ref: {p.Referencia})" : ""))), true));
        }

        parrafoPagos.Add(new(". Monto recibido: "));
        parrafoPagos.Add(new($"${venta.MontoRecibido.ToString("N0", CulturaCop)} COP", true));
        if (venta.Cambio > 0)
        {
            parrafoPagos.Add(new("  |  Cambio entregado: "));
            parrafoPagos.Add(new($"${venta.Cambio.ToString("N0", CulturaCop)} COP", true));
        }
        parrafoPagos.Add(new("."));
        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoPagos);

        // SECCIÓN V: CONSTANCIA Y FIRMA
        PdfInformeEngine.DibujarTituloSeccion(ctx, "V", "Constancia de Custodia y Entrega");
        var notasFinales = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("Este documento acredita la entrega física de los productos y el recaudo interno en la caja del punto de venta solidario de FAAUA. Los fondos son destinados a la liquidación de las cosechas de las asociaciones productoras de Urabá conforme al acuerdo de comercialización comunitaria vigente.")
        };
        PdfInformeEngine.DibujarParrafoMixto(ctx, notasFinales);

        DibujarFirmas(ctx, ValorONoRegistrado(venta.AtendidoPor), "Responsable Mostrador FAAUA", ValorONoRegistrado(venta.ClienteNombre), "Cliente Adquirente");

        PdfInformeEngine.DibujarPiePaginas(ctx);
        return PdfInformeEngine.CompilarPdf(ctx.Paginas);
    }

    /// <summary>
    /// Genera el Comprobante Interno de Recepción de Cosecha en centro de acopio/PDV.
    /// </summary>
    public static byte[] GenerarComprobanteRecepcionPdf(RecepcionProductoPdv recepcion, string funcionarioResponsable = "Receptor PDV FAAUA")
    {
        var generadoEn = DateTime.Now;
        var consecutivo = string.IsNullOrWhiteSpace(recepcion.ConsecutivoRecepcion) ? "REC-S/N" : recepcion.ConsecutivoRecepcion.Trim();
        var fechaRecep = recepcion.FechaRecepcion.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        var ctx = new PdfInformeEngine.DocumentoInformeContexto
        {
            AreaInstitucional = "PUNTO DE VENTA SOLIDARIO · RECEPCIÓN Y ACOPIO DE COSECHAS",
            TituloDocumento = "COMPROBANTE INTERNO DE RECEPCIÓN DE COSECHA",
            SubtituloDocumento = $"Consecutivo: {consecutivo}  |  Fecha: {fechaRecep}  |  Asociación: {recepcion.AsociacionOrigen}",
            ReferenciaDerecha = $"Recepción: {consecutivo}",
            TituloCortoContinuacion = $"COMPROBANTE DE RECEPCIÓN — {consecutivo}",
            NotaPieLinea1 = $"Acta interna de ingreso a inventario expedida el {generadoEn:dd/MM/yyyy a las HH:mm} · Registrado por: {ValorONoRegistrado(recepcion.RegistradoPor)}.",
            NotaPieLinea2 = "Comprobante de soporte interno para trazabilidad de inventarios de la red ASOAGRO-FAAUA. No constituye factura ni título valor.",
            FechaGeneracion = generadoEn
        };

        ctx.IniciarDocumento();

        // SECCIÓN I: DATOS DEL REGISTRO Y ASOCIACIÓN PROVEEDORA
        PdfInformeEngine.DibujarTituloSeccion(ctx, "I", "Identificación del Lote y Asociación Proveedora");

        var parrafoOrigen = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("Se deja constancia formal del ingreso físico a almacén e inventario bajo el registro "),
            new(consecutivo, true),
            new(", correspondiente a la remisión o soporte No. "),
            new(ValorONoRegistrado(recepcion.NumeroComprobante), true),
            new(", entregado por la asociación de base "),
            new(ValorONoRegistrado(recepcion.AsociacionOrigen), true),
            new(", a través del productor o transportador autorizado "),
            new(ValorONoRegistrado(recepcion.ProveedorNombre), true),
            new(" el día "),
            new(fechaRecep, true),
            new(".")
        };
        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoOrigen);

        // SECCIÓN II: DETALLE DEL PRODUCTO, VOLUMEN Y COSTO
        PdfInformeEngine.DibujarTituloSeccion(ctx, "II", "Detalle de Cosecha, Cantidad y Costo Registrado");

        var columnas = new List<PdfInformeEngine.ColumnaTabla>
        {
            new() { Titulo = "Código", Ancho = 75.0 },
            new() { Titulo = "Producto Agropecuario", Ancho = 180.0 },
            new() { Titulo = "Cant. Recibida", Ancho = 80.0, AlinearDerecha = true },
            new() { Titulo = "Unidad", Ancho = 55.0 },
            new() { Titulo = "Costo Unit. COP", Ancho = 85.0, AlinearDerecha = true },
            new() { Titulo = "Costo Total COP", Ancho = 75.0, AlinearDerecha = true }
        };

        var filas = new List<string[]>
        {
            new[]
            {
                ValorONoRegistrado(recepcion.CodigoProducto),
                ValorONoRegistrado(recepcion.ProductoNombre),
                recepcion.Cantidad.ToString("N0", CulturaCop),
                ValorONoRegistrado(recepcion.UnidadMedida),
                $"${recepcion.CostoUnitario.ToString("N0", CulturaCop)}",
                $"${recepcion.CostoTotal.ToString("N0", CulturaCop)}"
            }
        };

        PdfInformeEngine.DibujarTablaFormal(ctx, columnas, filas);

        // SECCIÓN III: CONTROL DE CALIDAD, OBSERVACIONES Y ESTADO
        PdfInformeEngine.DibujarTituloSeccion(ctx, "III", "Observaciones de Calidad, Pesaje y Trazabilidad");

        var obs = string.IsNullOrWhiteSpace(recepcion.Observaciones)
            ? "Cosecha recibida a satisfacción en punto de acopio con verificación visual de maduración y calibración estándar."
            : recepcion.Observaciones.Trim();

        var parrafoObs = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("Estado de recepción en inventario: "),
            new(recepcion.EstadoRecepcion, true),
            new(". Observaciones de pesaje y calidad: "),
            new(obs, false),
            new(" Soporte digital adjunto: "),
            new(string.IsNullOrWhiteSpace(recepcion.SoporteNombre) ? "Sin archivo digital adjunto" : recepcion.SoporteNombre.Trim(), true),
            new(".")
        };
        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoObs);

        // SECCIÓN IV: FIRMAS Y RESPONSABILIDADES
        PdfInformeEngine.DibujarTituloSeccion(ctx, "IV", "Responsables de Entrega y Recepción");

        DibujarFirmas(ctx, ValorONoRegistrado(recepcion.ProveedorNombre), "Productor / Transportador Entregante", ValorONoRegistrado(recepcion.RegistradoPor), "Responsable Recepción FAAUA");

        PdfInformeEngine.DibujarPiePaginas(ctx);
        return PdfInformeEngine.CompilarPdf(ctx.Paginas);
    }

    /// <summary>
    /// Genera el Comprobante Interno de Devolución o Anulación de una venta.
    /// </summary>
    public static byte[] GenerarComprobanteAnulacionPdf(VentaPdv venta, string funcionarioResponsable = "Administrador PDV FAAUA")
    {
        var generadoEn = DateTime.Now;
        var consecutivoAnul = string.IsNullOrWhiteSpace(venta.ConsecutivoAnulacion) ? "DEV-S/N" : venta.ConsecutivoAnulacion.Trim();
        var ticketOriginal = string.IsNullOrWhiteSpace(venta.NumeroTicket) ? "TKT-S/N" : venta.NumeroTicket.Trim();
        var fechaOpOriginal = venta.Fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        var fechaAnul = (venta.FechaAnulacion ?? DateTime.Now).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

        var ctx = new PdfInformeEngine.DocumentoInformeContexto
        {
            AreaInstitucional = "PUNTO DE VENTA SOLIDARIO · AUDITORÍA Y CONTROL INTERNO",
            TituloDocumento = "COMPROBANTE INTERNO DE DEVOLUCIÓN / ANULACIÓN",
            SubtituloDocumento = $"Anulación No.: {consecutivoAnul}  |  Ticket Original: {ticketOriginal}  |  Fecha Anulación: {fechaAnul}",
            ReferenciaDerecha = $"Anul.: {consecutivoAnul}",
            TituloCortoContinuacion = $"COMPROBANTE DE DEVOLUCIÓN — {consecutivoAnul}",
            NotaPieLinea1 = $"Comprobante de anulación emitido el {generadoEn:dd/MM/yyyy a las HH:mm} · Anulado por: {ValorONoRegistrado(venta.AnuladoPor)}.",
            NotaPieLinea2 = "Soporte interno de anulación de operación y reintegro de inventario en mostrador solidario ASOAGRO-FAAUA. No constituye nota crédito electrónica DIAN.",
            FechaGeneracion = generadoEn
        };

        ctx.IniciarDocumento();

        // SECCIÓN I: IDENTIFICACIÓN DE LA TRANSACCIÓN ANULADA
        PdfInformeEngine.DibujarTituloSeccion(ctx, "I", "Vínculo con la Transacción Original y Responsables");

        var parrafoOriginal = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("El presente comprobante acredita la anulación formal y reintegro al inventario de la transacción originada bajo el ticket "),
            new(ticketOriginal, true),
            new(", efectuada originalmente el "),
            new(fechaOpOriginal, true),
            new(" por el operador "),
            new(ValorONoRegistrado(venta.AtendidoPor), true),
            new(". La anulación fue ejecutada y autorizada el "),
            new(fechaAnul, true),
            new(" por el responsable "),
            new(ValorONoRegistrado(venta.AnuladoPor), true),
            new(" asignándole el consecutivo de control "),
            new(consecutivoAnul, true),
            new(".")
        };
        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoOriginal);

        // SECCIÓN II: MOTIVO OBLIGATORIO DE LA DEVOLUCIÓN / ANULACIÓN
        PdfInformeEngine.DibujarTituloSeccion(ctx, "II", "Causa y Justificación Estatutaria de la Devolución");

        var motivo = string.IsNullOrWhiteSpace(venta.MotivoAnulacion)
            ? "No se registró motivo explícito de anulación."
            : venta.MotivoAnulacion.Trim();

        var parrafoMotivo = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("Motivo formal registrado: "),
            new(motivo, true),
            new(". En cumplimiento con los protocolos de control y auditoría de ASOAGRO, la transacción original se conserva íntegra en la bitácora histórica y los productos han sido restituidos automáticamente a las existencias disponibles.")
        };
        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoMotivo);

        // SECCIÓN III: PRODUCTOS RESTITUIDOS AL INVENTARIO
        PdfInformeEngine.DibujarTituloSeccion(ctx, "III", "Productos y Cantidades Reintegradas al Stock");

        var columnas = new List<PdfInformeEngine.ColumnaTabla>
        {
            new() { Titulo = "Código", Ancho = 75.0 },
            new() { Titulo = "Producto Agropecuario", Ancho = 180.0 },
            new() { Titulo = "Cant. Reintegrada", Ancho = 80.0, AlinearDerecha = true },
            new() { Titulo = "Unidad", Ancho = 60.0 },
            new() { Titulo = "Valor Revertido COP", Ancho = 140.0, AlinearDerecha = true }
        };

        var filas = new List<string[]>();
        foreach (var item in venta.Items)
        {
            filas.Add(new[]
            {
                ValorONoRegistrado(item.CodigoProducto),
                ValorONoRegistrado(item.NombreProducto),
                item.Cantidad.ToString("N0", CulturaCop),
                ValorONoRegistrado(item.UnidadMedida),
                $"${item.Subtotal.ToString("N0", CulturaCop)}"
            });
        }
        PdfInformeEngine.DibujarTablaFormal(ctx, columnas, filas);

        // SECCIÓN IV: VALOR TOTAL Y REVERSIÓN DE FONDOS
        PdfInformeEngine.DibujarTituloSeccion(ctx, "IV", "Liquidación de Reintegro de Fondos en COP");

        var parrafoTotal = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("VALOR TOTAL ANULADO / REVERTIDO: "),
            new($"${venta.Total.ToString("N0", CulturaCop)} COP", true),
            new("  |  Medio original de pago: "),
            new(venta.MetodoPago, true),
            new(". Adquirente relacionado: "),
            new(ValorONoRegistrado(venta.ClienteNombre), true),
            new(" (Doc: "),
            new(ValorONoRegistrado(venta.ClienteDocumento), true),
            new(").")
        };
        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoTotal);

        // SECCIÓN V: FIRMAS DE RESPONSABILIDAD
        PdfInformeEngine.DibujarTituloSeccion(ctx, "V", "Constancia de Autorización y Cierre de Anulación");

        DibujarFirmas(ctx, ValorONoRegistrado(venta.AnuladoPor), "Autoriza Anulación", ValorONoRegistrado(venta.AtendidoPor), "Operador Mostrador FAAUA");

        PdfInformeEngine.DibujarPiePaginas(ctx);
        return PdfInformeEngine.CompilarPdf(ctx.Paginas);
    }

    private static void DibujarFirmas(PdfInformeEngine.DocumentoInformeContexto ctx, string responsable, string cargo1, string segundo, string cargo2)
    {
        ctx.AsegurarEspacio(70.0);
        ctx.CursorY -= 35.0;

        var wFirma = 190.0;
        var x1 = PdfInformeEngine.MarginX;
        var x2 = PdfInformeEngine.PageWidth - PdfInformeEngine.MarginX - wFirma;
        var yLinea = ctx.CursorY;

        PdfInformeEngine.LineaHorizontal(ctx.PaginaActual.Stream, x1, yLinea, wFirma, 0.8, 0.3, 0.35, 0.3);
        PdfInformeEngine.EscribirTexto(ctx.PaginaActual.Stream, true, 8.5, x1, yLinea - 12.0, responsable, 0.1, 0.15, 0.12);
        PdfInformeEngine.EscribirTexto(ctx.PaginaActual.Stream, false, 7.5, x1, yLinea - 22.0, cargo1, 0.35, 0.4, 0.38);

        if (!string.IsNullOrWhiteSpace(segundo))
        {
            PdfInformeEngine.LineaHorizontal(ctx.PaginaActual.Stream, x2, yLinea, wFirma, 0.8, 0.3, 0.35, 0.3);
            PdfInformeEngine.EscribirTexto(ctx.PaginaActual.Stream, true, 8.5, x2, yLinea - 12.0, segundo, 0.1, 0.15, 0.12);
            PdfInformeEngine.EscribirTexto(ctx.PaginaActual.Stream, false, 7.5, x2, yLinea - 22.0, cargo2, 0.35, 0.4, 0.38);
        }

        ctx.CursorY = yLinea - 30.0;
    }

    private static string ValorONoRegistrado(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? "No registrado" : valor.Trim();
}

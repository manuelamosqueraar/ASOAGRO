using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Asoagro.Models;

namespace Asoagro.Services;

/// <summary>
/// Generador formal de Informes y Comprobantes Internos de Operación Financiera y Balances Consolidados de Tesorería (PDF 1.4).
/// Diseñado con formato de informe institucional, párrafos naturales y tablas comparativas limpias.
/// </summary>
public static class ComprobanteFinancieroPdfGenerator
{
    private static readonly CultureInfo CulturaCop = new("es-CO");

    /// <summary>
    /// Genera el informe y comprobante individual de operación financiera (Venta, Ingreso, Costo/Gasto o Pago/Despacho).
    /// </summary>
    public static byte[] GenerarComprobantePdf(
        MovimientoFinanciero mov,
        AsociacionInstitucional? asoc = null,
        string funcionario = "Tesorera General FAAUA")
    {
        var generadoEn = DateTime.Now;
        var refCodigo = ValorONoRegistrado(mov.CodigoReferencia);

        string tituloDoc;
        string tipoOperacionNarrativo;

        if (mov.Tipo == TipoMovimientoFinanciero.Ingreso)
        {
            if (mov.TipoSoporte == TiposSoporteFinanciero.FacturaVentaAsociacion)
            {
                tituloDoc = "INFORME Y COMPROBANTE INTERNO DE VENTA DE PRODUCCIÓN";
                tipoOperacionNarrativo = "ingreso por venta de producción o cosecha";
            }
            else
            {
                tituloDoc = "INFORME Y COMPROBANTE INTERNO DE INGRESO Y RECAUDO";
                tipoOperacionNarrativo = "ingreso y recaudo asociativo";
            }
        }
        else if (mov.Tipo == TipoMovimientoFinanciero.CostoGasto)
        {
            tituloDoc = "INFORME Y COMPROBANTE INTERNO DE COSTO Y GASTO";
            tipoOperacionNarrativo = "costo o gasto operativo institucional";
        }
        else
        {
            tituloDoc = "INFORME Y COMPROBANTE DE PAGO Y DESPACHO";
            tipoOperacionNarrativo = "pago, liquidación o despacho";
        }

        var estadoMov = ValorONoRegistrado(mov.Estado);
        var fechaOp = mov.FechaMovimiento.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        var ctx = new PdfInformeEngine.DocumentoInformeContexto
        {
            AreaInstitucional = "TESORERÍA GENERAL · GESTIÓN FINANCIERA Y CONTROL INTERNO",
            TituloDocumento = tituloDoc,
            SubtituloDocumento = $"Consecutivo: {refCodigo}  |  Fecha de operación: {fechaOp}  |  Estado: {estadoMov}  |  Método: {ValorONoRegistrado(mov.MetodoPago)}",
            ReferenciaDerecha = $"Ref.: {refCodigo}",
            TituloCortoContinuacion = $"{tituloDoc} — REF. {refCodigo}",
            NotaPieLinea1 = $"Informe financiero generado por el sistema ASOAGRO el {generadoEn:dd/MM/yyyy a las HH:mm} · Consecutivo: {refCodigo}.",
            NotaPieLinea2 = "Comprobante interno de control administrativo. No sustituye la Factura Electrónica de Venta DIAN ni constituye validación legal externa.",
            FechaGeneracion = generadoEn
        };

        ctx.IniciarDocumento();

        // =====================================================================
        // SECCIÓN I: IDENTIFICACIÓN DE LA ASOCIACIÓN Y TERCERO INTERVINIENTE
        // =====================================================================
        PdfInformeEngine.DibujarTituloSeccion(ctx, "I", "Identificación de la Entidad Titular y del Tercero Interviniente");

        var nombreAsoc = ValorONoRegistrado(mov.NombreAsociacion);
        var nitAsoc = !string.IsNullOrWhiteSpace(mov.NitAsociacion)
            ? mov.NitAsociacion.Trim()
            : (asoc != null && !string.IsNullOrWhiteSpace(asoc.Nit) ? asoc.Nit.Trim() : "No registrado");
        var regimenAsoc = !string.IsNullOrWhiteSpace(mov.RegimenTributarioAsociacion)
            ? mov.RegimenTributarioAsociacion.Trim()
            : (asoc != null && !string.IsNullOrWhiteSpace(asoc.RegimenTributario) ? asoc.RegimenTributario.Trim() : "No registrado");
        var muniAsoc = asoc != null && !string.IsNullOrWhiteSpace(asoc.Municipio)
            ? $"{asoc.Municipio.Trim()}{(!string.IsNullOrWhiteSpace(asoc.Departamento) ? $", {asoc.Departamento.Trim()}" : "")}"
            : "No registrado";
        var obligacionFacturarTexto = mov.AsociacionObligadaAFacturar
            ? "Obligada a expedir factura electrónica"
            : "No obligada a facturar electrónicamente / Régimen especial agropecuario";

        var parrafoEntidad = new List<PdfInformeEngine.FragmentoTexto>
        {
            new($"El presente documento registra una operación de {tipoOperacionNarrativo} con fecha "),
            new(fechaOp, true),
            new(", correspondiente a la asociación titular "),
            new(nombreAsoc, true),
            new(", identificada con NIT "),
            new(nitAsoc, true),
            new(", con ubicación registrada en "),
            new(muniAsoc, true),
            new(". La entidad registra el régimen tributario "),
            new(regimenAsoc, true),
            new($" ({obligacionFacturarTexto}), vinculando esta transacción al proyecto o actividad "),
            new(ValorONoRegistrado(mov.ActividadOProyecto), true),
            new(".")
        };
        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoEntidad);

        var nombreTercero = ValorONoRegistrado(mov.BeneficiarioOProveedor);
        var docTercero = ValorONoRegistrado(mov.DocumentoIdentidadTercero);
        var calidadTercero = ValorONoRegistrado(mov.TipoTercero);

        var parrafoTercero = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("Interviene como tercero relacionado en la operación "),
            new(nombreTercero, true),
            new(", identificado con cédula o NIT "),
            new(docTercero, true),
            new(", actuando en calidad de "),
            new(calidadTercero, true),
            new(", bajo la modalidad o condición de pago "),
            new(ValorONoRegistrado(mov.MetodoPago), true),
            new(".")
        };
        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoTercero);

        // =====================================================================
        // SECCIÓN II: CONCEPTO, ORIGEN Y JUSTIFICACIÓN DE LA OPERACIÓN
        // =====================================================================
        PdfInformeEngine.DibujarTituloSeccion(ctx, "II", "Concepto, Origen y Justificación de la Operación");

        var parrafoConcepto = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("La operación se clasifica en la categoría "),
            new(ValorONoRegistrado(mov.Categoria), true),
            new(" y subtipo "),
            new(ValorONoRegistrado(mov.Subtipo), true),
            new(", con origen o motivo registrado como "),
            new(ValorONoRegistrado(mov.OrigenOMotivo), true),
            new(". La descripción detallada del movimiento en el sistema es la siguiente: "),
            new(ValorONoRegistrado(mov.Descripcion), true),
            new(".")
        };
        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoConcepto);

        if (mov.Tipo == TipoMovimientoFinanciero.CostoGasto)
        {
            var queSePago = !string.IsNullOrWhiteSpace(mov.QueSePago) ? mov.QueSePago.Trim() : ValorONoRegistrado(mov.Descripcion);
            var paraQue = ValorONoRegistrado(mov.ParaQueSePago);
            var actividadNecesidad = !string.IsNullOrWhiteSpace(mov.ActividadONecesidad)
                ? mov.ActividadONecesidad.Trim()
                : ValorONoRegistrado(mov.ActividadOProyecto);

            PdfInformeEngine.DibujarParrafoMixto(ctx, new[]
            {
                new PdfInformeEngine.FragmentoTexto("Respecto al detalle específico del desembolso o gasto, se deja constancia de que el bien o servicio pagado corresponde a: "),
                new PdfInformeEngine.FragmentoTexto(queSePago, true),
                new PdfInformeEngine.FragmentoTexto("; reconocido a favor de "),
                new PdfInformeEngine.FragmentoTexto(nombreTercero, true),
                new PdfInformeEngine.FragmentoTexto(" con la destinación o finalidad de: "),
                new PdfInformeEngine.FragmentoTexto(paraQue, true),
                new PdfInformeEngine.FragmentoTexto(", en atención a la actividad o necesidad institucional: "),
                new PdfInformeEngine.FragmentoTexto(actividadNecesidad, true),
                new PdfInformeEngine.FragmentoTexto(".")
            });
        }

        if (!string.IsNullOrWhiteSpace(mov.NotaObservacion))
        {
            PdfInformeEngine.DibujarParrafoMixto(ctx, new[]
            {
                new PdfInformeEngine.FragmentoTexto("Observaciones registradas en la operación: ", true),
                new PdfInformeEngine.FragmentoTexto(mov.NotaObservacion.Trim(), false)
            });
        }

        if (!string.IsNullOrWhiteSpace(mov.MotivoAnulacion))
        {
            PdfInformeEngine.DibujarParrafoMixto(ctx, new[]
            {
                new PdfInformeEngine.FragmentoTexto("Motivo de anulación registrado: ", true),
                new PdfInformeEngine.FragmentoTexto(mov.MotivoAnulacion.Trim(), false)
            });
        }

        // =====================================================================
        // SECCIÓN III: LIQUIDACIÓN DE VALORES EN PESOS COLOMBIANOS (COP)
        // =====================================================================
        PdfInformeEngine.DibujarTituloSeccion(ctx, "III", "Liquidación de Valores en Pesos Colombianos (COP)");

        var montoTexto = $"{mov.Monto.ToString("C0", CulturaCop)} COP";
        var pagadoTexto = $"{mov.ValorPagado.ToString("C0", CulturaCop)} COP";
        var saldoTexto = $"{mov.SaldoPendiente.ToString("C0", CulturaCop)} COP";
        var vencimientoOp = mov.FechaVencimiento.HasValue
            ? mov.FechaVencimiento.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            : "No registrado";

        PdfInformeEngine.DibujarParrafoMixto(ctx, new[]
        {
            new PdfInformeEngine.FragmentoTexto("El valor total de la operación asciende a "),
            new PdfInformeEngine.FragmentoTexto(montoTexto, true),
            new PdfInformeEngine.FragmentoTexto(", registrando un valor efectivamente pagado o recaudado de "),
            new PdfInformeEngine.FragmentoTexto(pagadoTexto, true),
            new PdfInformeEngine.FragmentoTexto(" y un saldo pendiente por liquidar de "),
            new PdfInformeEngine.FragmentoTexto(saldoTexto, true),
            new PdfInformeEngine.FragmentoTexto($", con estado actual "),
            new PdfInformeEngine.FragmentoTexto(estadoMov, true),
            new PdfInformeEngine.FragmentoTexto(".")
        });

        var columnasValores = new List<PdfInformeEngine.ColumnaTabla>
        {
            new() { Titulo = "Concepto de Liquidación", Ancho = 190.0, Negrita = true },
            new() { Titulo = "Condición / Detalle Registrado", Ancho = 186.0 },
            new() { Titulo = "Valor (COP)", Ancho = 140.0, AlinearDerecha = true, Negrita = true }
        };

        var filasValores = new List<string[]>
        {
            new[] { "Valor total de la operación", $"Categoría: {ValorONoRegistrado(mov.Categoria)}", montoTexto },
            new[] { "Valor pagado / recaudado", $"Método de pago: {ValorONoRegistrado(mov.MetodoPago)}", pagadoTexto },
            new[] { "Saldo pendiente", $"Estado: {estadoMov} · Vencimiento: {vencimientoOp}", saldoTexto }
        };

        PdfInformeEngine.DibujarTablaFormal(ctx, columnasValores, filasValores);

        // =====================================================================
        // SECCIÓN IV: SOPORTES DOCUMENTALES Y ALCANCE NORMATIVO
        // =====================================================================
        PdfInformeEngine.DibujarTituloSeccion(ctx, "IV", "Soportes Documentales Registrados y Alcance Administrativo");

        var parrafoSoporte = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("Como respaldo documental de la operación se registra el tipo de soporte "),
            new(ValorONoRegistrado(mov.TipoSoporte), true),
            new(", con número de factura o referencia "),
            new(ValorONoRegistrado(mov.NumeroFactura), true)
        };

        if (!string.IsNullOrWhiteSpace(mov.CufeCude))
        {
            parrafoSoporte.Add(new(" y código CUFE/CUDE "));
            parrafoSoporte.Add(new(mov.CufeCude.Trim(), true));
        }

        parrafoSoporte.Add(new(". A continuación se detalla el estado de los archivos asociados en el sistema:"));
        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoSoporte);

        var estadoArchivoPrincipal = !string.IsNullOrWhiteSpace(mov.ArchivoNombre)
            ? $"{mov.ArchivoNombre.Trim()}{(!string.IsNullOrWhiteSpace(mov.ArchivoTamano) ? $" ({mov.ArchivoTamano.Trim()})" : "")} — Estado: Almacenado en el sistema"
            : "No registrado";

        var estadoComprobantePago = !string.IsNullOrWhiteSpace(mov.ComprobantePagoNombre)
            ? $"{mov.ComprobantePagoNombre.Trim()} — Estado: Almacenado en el sistema"
            : "No registrado";

        PdfInformeEngine.DibujarItemVineta(ctx, "Soporte documental principal", estadoArchivoPrincipal);
        PdfInformeEngine.DibujarItemVineta(ctx, "Comprobante de pago anexo", estadoComprobantePago);

        ctx.CursorY -= 4.0;
        PdfInformeEngine.DibujarParrafo(
            ctx,
            "Aviso de control interno y cumplimiento tributario: Este documento es un comprobante e informe interno de control administrativo y asociativo emitido por el sistema ASOAGRO. Los archivos citados reflejan únicamente su almacenamiento digital en el sistema y no sustituyen la Factura Electrónica de Venta validada por la DIAN (Art. 616-1 del Estatuto Tributario) ni implican validación legal externa.",
            fontSize: 8.5,
            interlineado: 12.2,
            espacioDespues: 14.0);

        // =====================================================================
        // BLOQUE FORMAL DE FIRMAS Y RESPONSABLES
        // =====================================================================
        ctx.AsegurarEspacio(72.0);
        ctx.CursorY -= 26.0;

        var anchoFirma = (PdfInformeEngine.ContentWidth - 48.0) / 2.0;
        var xFirma1 = PdfInformeEngine.MarginX;
        var xFirma2 = PdfInformeEngine.MarginX + anchoFirma + 48.0;

        PdfInformeEngine.LineaHorizontal(ctx.PaginaActual.Stream, xFirma1, ctx.CursorY, anchoFirma, 0.7, 0.35, 0.40, 0.37);
        PdfInformeEngine.EscribirTexto(ctx.PaginaActual.Stream, true, 8.6, xFirma1, ctx.CursorY - 12.0, ValorONoRegistrado(funcionario), 0.08, 0.20, 0.13);
        PdfInformeEngine.EscribirTexto(ctx.PaginaActual.Stream, false, 7.8, xFirma1, ctx.CursorY - 22.5, "Tesorería y Control Financiero · ASOAGRO", 0.32, 0.38, 0.34);

        PdfInformeEngine.LineaHorizontal(ctx.PaginaActual.Stream, xFirma2, ctx.CursorY, anchoFirma, 0.7, 0.35, 0.40, 0.37);
        PdfInformeEngine.EscribirTexto(ctx.PaginaActual.Stream, true, 8.6, xFirma2, ctx.CursorY - 12.0, nombreTercero, 0.08, 0.20, 0.13);
        PdfInformeEngine.EscribirTexto(ctx.PaginaActual.Stream, false, 7.8, xFirma2, ctx.CursorY - 22.5, $"Tercero Interviniente ({calidadTercero}) · CC/NIT: {docTercero}", 0.32, 0.38, 0.34);

        ctx.CursorY -= 34.0;

        PdfInformeEngine.DibujarPiePaginas(ctx);
        return PdfInformeEngine.CompilarPdf(ctx.Paginas);
    }

    /// <summary>
    /// Genera el informe consolidado de Tesorería, Ingresos, Costos y Saldos para una asociación y período determinados.
    /// </summary>
    public static byte[] GenerarReporteConsolidadoPdf(
        ResumenFinanciero resumen,
        IEnumerable<MovimientoFinanciero> movimientos,
        string asociacion,
        string periodo,
        string funcionario = "Tesorera General FAAUA")
    {
        var generadoEn = DateTime.Now;
        var listaMovimientos = (movimientos ?? Enumerable.Empty<MovimientoFinanciero>()).ToList();
        var asocTexto = ValorONoRegistrado(asociacion);
        var periodoTexto = ValorONoRegistrado(periodo);
        var respTexto = ValorONoRegistrado(funcionario);

        var ctx = new PdfInformeEngine.DocumentoInformeContexto
        {
            AreaInstitucional = "TESORERÍA GENERAL · SUPERVISIÓN FINANCIERA Y BALANCES CONTABLES",
            TituloDocumento = "INFORME CONSOLIDADO DE TESORERÍA, INGRESOS, COSTOS Y SALDOS",
            SubtituloDocumento = $"Asociación: {asocTexto}  |  Período contable: {periodoTexto}  |  Responsable: {respTexto}",
            ReferenciaDerecha = $"Movimientos: {resumen.TotalMovimientos}",
            TituloCortoContinuacion = $"INFORME DE TESORERÍA — {asocTexto.ToUpperInvariant()} ({periodoTexto.ToUpperInvariant()})",
            NotaPieLinea1 = $"Informe consolidado generado por el sistema ASOAGRO el {generadoEn:dd/MM/yyyy a las HH:mm} · Responsable: {respTexto}.",
            NotaPieLinea2 = "Los saldos y movimientos reportados corresponden a los registros almacenados en el sistema para supervisión interna.",
            FechaGeneracion = generadoEn
        };

        ctx.IniciarDocumento();

        // =====================================================================
        // SECCIÓN I: SÍNTESIS EJECUTIVA DEL BALANCE FINANCIERO
        // =====================================================================
        PdfInformeEngine.DibujarTituloSeccion(ctx, "I", "Síntesis Ejecutiva del Balance Financiero");

        var ingresosTexto = $"{resumen.TotalIngresos.ToString("C0", CulturaCop)} COP";
        var costosTexto = $"{resumen.TotalCostosYGastos.ToString("C0", CulturaCop)} COP";
        var porCobrarTexto = $"{resumen.TotalCuentasPorCobrar.ToString("C0", CulturaCop)} COP";
        var porPagarTexto = $"{resumen.TotalCuentasPorPagar.ToString("C0", CulturaCop)} COP";
        var saldoNetoTexto = $"{resumen.SaldoNeto.ToString("C0", CulturaCop)} COP";

        PdfInformeEngine.DibujarParrafoMixto(ctx, new[]
        {
            new PdfInformeEngine.FragmentoTexto("El presente informe presenta el balance consolidado de tesorería correspondiente a "),
            new PdfInformeEngine.FragmentoTexto(asocTexto, true),
            new PdfInformeEngine.FragmentoTexto(" durante el período "),
            new PdfInformeEngine.FragmentoTexto(periodoTexto, true),
            new PdfInformeEngine.FragmentoTexto(", consolidando un total de "),
            new PdfInformeEngine.FragmentoTexto($"{resumen.TotalMovimientos} movimiento(s)", true),
            new PdfInformeEngine.FragmentoTexto(" registrados en el sistema. Durante este lapso se contabilizan ingresos totales por "),
            new PdfInformeEngine.FragmentoTexto(ingresosTexto, true),
            new PdfInformeEngine.FragmentoTexto(" y costos y gastos por "),
            new PdfInformeEngine.FragmentoTexto(costosTexto, true),
            new PdfInformeEngine.FragmentoTexto(", arrojando un saldo neto de "),
            new PdfInformeEngine.FragmentoTexto(saldoNetoTexto, true),
            new PdfInformeEngine.FragmentoTexto(". Así mismo, se registran cuentas por cobrar por "),
            new PdfInformeEngine.FragmentoTexto(porCobrarTexto, true),
            new PdfInformeEngine.FragmentoTexto(" y cuentas por pagar por "),
            new PdfInformeEngine.FragmentoTexto(porPagarTexto, true),
            new PdfInformeEngine.FragmentoTexto(".")
        });

        PdfInformeEngine.DibujarParrafoMixto(ctx, new[]
        {
            new PdfInformeEngine.FragmentoTexto("De acuerdo con su estado administrativo, los registros analizados se distribuyen en: "),
            new PdfInformeEngine.FragmentoTexto($"{resumen.CantidadPagados} pagado(s)", true),
            new PdfInformeEngine.FragmentoTexto(", "),
            new PdfInformeEngine.FragmentoTexto($"{resumen.CantidadValidados} validado(s)", true),
            new PdfInformeEngine.FragmentoTexto(", "),
            new PdfInformeEngine.FragmentoTexto($"{resumen.CantidadPendientesRevision} pendiente(s) de revisión", true),
            new PdfInformeEngine.FragmentoTexto(", "),
            new PdfInformeEngine.FragmentoTexto($"{resumen.CantidadObservados} observado(s)", true),
            new PdfInformeEngine.FragmentoTexto(" y "),
            new PdfInformeEngine.FragmentoTexto($"{resumen.CantidadAnulados} anulado(s)", true),
            new PdfInformeEngine.FragmentoTexto(".")
        });

        var columnasBalance = new List<PdfInformeEngine.ColumnaTabla>
        {
            new() { Titulo = "Indicador del Balance", Ancho = 180.0, Negrita = true },
            new() { Titulo = "Descripción del Rubro Contable", Ancho = 200.0 },
            new() { Titulo = "Valor Consolidado (COP)", Ancho = 136.0, AlinearDerecha = true, Negrita = true }
        };

        var filasBalance = new List<string[]>
        {
            new[] { "Total Ingresos", "Ventas de producción, recaudos y aportes registrados", ingresosTexto },
            new[] { "Total Costos y Gastos", "Insumos, logística, operación y gastos justificados", costosTexto },
            new[] { "Cuentas por Cobrar", "Saldos pendientes de recaudo a favor de la asociación", porCobrarTexto },
            new[] { "Cuentas por Pagar", "Obligaciones pendientes de pago con terceros", porPagarTexto },
            new[] { "Saldo Neto del Período", "Diferencia entre total de ingresos y total de costos/gastos", saldoNetoTexto }
        };

        PdfInformeEngine.DibujarTablaFormal(ctx, columnasBalance, filasBalance);

        // =====================================================================
        // SECCIÓN II: RELACIÓN DETALLADA DE MOVIMIENTOS FINANCIEROS
        // =====================================================================
        PdfInformeEngine.DibujarTituloSeccion(ctx, "II", "Relación Detallada de Movimientos Financieros");

        if (listaMovimientos.Count == 0)
        {
            PdfInformeEngine.DibujarParrafo(
                ctx,
                "No se registran movimientos financieros para los filtros seleccionados en este informe.");
        }
        else
        {
            PdfInformeEngine.DibujarParrafo(
                ctx,
                $"A continuación se presenta el listado detallado de los {listaMovimientos.Count} movimiento(s) financiero(s) incluidos en el período consultado:");

            var columnasMovs = new List<PdfInformeEngine.ColumnaTabla>
            {
                new() { Titulo = "Código", Ancho = 62.0, Negrita = true },
                new() { Titulo = "Fecha", Ancho = 54.0 },
                new() { Titulo = "Tipo y Categoría", Ancho = 104.0 },
                new() { Titulo = "Descripción / Concepto", Ancho = 114.0 },
                new() { Titulo = "Tercero y Soporte", Ancho = 100.0 },
                new() { Titulo = "Estado y Valor (COP)", Ancho = 82.0, AlinearDerecha = true, Negrita = true }
            };

            var filasMovs = new List<string[]>();
            foreach (var m in listaMovimientos)
            {
                var tipoLegible = m.Tipo switch
                {
                    TipoMovimientoFinanciero.Ingreso => "Ingreso",
                    TipoMovimientoFinanciero.CostoGasto => "Costo / Gasto",
                    _ => "Pago / Despacho"
                };

                var soporteAdjunto = !string.IsNullOrWhiteSpace(m.ArchivoNombre)
                    ? $"Soporte: {m.ArchivoNombre.Trim()} (Almacenado)"
                    : "Sin archivo adjunto";

                filasMovs.Add(new[]
                {
                    ValorONoRegistrado(m.CodigoReferencia),
                    m.FechaMovimiento.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    $"{tipoLegible} · {ValorONoRegistrado(m.Categoria)}",
                    ValorONoRegistrado(m.Descripcion),
                    $"{ValorONoRegistrado(m.BeneficiarioOProveedor)} ({soporteAdjunto})",
                    $"{m.Monto.ToString("C0", CulturaCop)} ({ValorONoRegistrado(m.Estado)})"
                });
            }

            PdfInformeEngine.DibujarTablaFormal(ctx, columnasMovs, filasMovs, fontSizeCeldas: 8.0);
        }

        PdfInformeEngine.DibujarPiePaginas(ctx);
        return PdfInformeEngine.CompilarPdf(ctx.Paginas);
    }

    private static string ValorONoRegistrado(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? "No registrado" : valor.Trim();
}

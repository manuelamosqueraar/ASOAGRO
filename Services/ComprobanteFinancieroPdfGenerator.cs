using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Asoagro.Models;

namespace Asoagro.Services;

/// <summary>
/// Generador formal en C# puro de Comprobantes Internos de Venta, Egresos, Recibos y Balances de Tesorería (PDF 1.4).
/// Incluye rótulo normativo explícito sobre control interno y diferenciación con factura electrónica DIAN.
/// </summary>
public static class ComprobanteFinancieroPdfGenerator
{
    private const double PageWidth = 612.0;  // Carta (8.5 in)
    private const double PageHeight = 792.0; // Carta (11.0 in)
    private const double MarginX = 40.0;
    private const double ContentWidth = PageWidth - (MarginX * 2.0);
    private const double BottomLimit = 55.0;

    private sealed class PdfPageBuilder
    {
        public StringBuilder Stream { get; } = new();
    }

    /// <summary>
    /// Genera el comprobante individual de operación financiera (Venta, Ingreso, Gasto, Costo o Pago).
    /// </summary>
    public static byte[] GenerarComprobantePdf(MovimientoFinanciero mov, AsociacionInstitucional? asoc = null, string funcionario = "Tesorera General FAAUA")
    {
        var paginas = new List<PdfPageBuilder>();
        var pagina = new PdfPageBuilder();
        paginas.Add(pagina);
        var s = pagina.Stream;
        double cursorY = PageHeight - MarginX;

        // 1. Franja superior institucional
        s.AppendLine("q 0.04 0.18 0.11 rg");
        s.AppendLine($"{MarginX:F2} {cursorY - 24:F2} {ContentWidth:F2} 24 re f Q");

        EscribirTexto(s, MarginX + 12, cursorY - 16, "ASOAGRO · FEDERACIÓN AGRARIA DE URABÁ (FAAUA) · GESTIÓN DE TESORERÍA", 8.5, true, 0.73, 0.91, 0.48);
        EscribirTexto(s, PageWidth - MarginX - 110, cursorY - 16, $"REF: {mov.CodigoReferencia}", 8.5, true, 1.0, 1.0, 1.0);
        cursorY -= 36.0;

        // 2. Encabezado principal del documento
        string tituloDoc;
        string subtituloDoc;

        if (mov.Tipo == TipoMovimientoFinanciero.Ingreso)
        {
            if (mov.TipoSoporte == TiposSoporteFinanciero.FacturaVentaAsociacion)
            {
                tituloDoc = "COMPROBANTE INTERNO DE VENTA DE PRODUCCIÓN";
                subtituloDoc = "Registro y control de comercialización agropecuaria (Documento interno asociativo)";
            }
            else
            {
                tituloDoc = "COMPROBANTE INTERNO DE INGRESO / RECAUDO";
                subtituloDoc = "Recibo oficial de fondos, cuotas o comercialización asociativa";
            }
        }
        else if (mov.Tipo == TipoMovimientoFinanciero.CostoGasto)
        {
            tituloDoc = "COMPROBANTE INTERNO DE COSTO Y GASTO";
            subtituloDoc = "Soporte justificado de desembolso, adquisición y necesidad institucional";
        }
        else
        {
            tituloDoc = "COMPROBANTE DE PAGO Y DESPACHO";
            subtituloDoc = "Liquidación y constancia de egreso, flete o amortización";
        }

        s.AppendLine("q 0.95 0.97 0.95 rg 0.09 0.38 0.23 RG 1.5 w");
        s.AppendLine($"{MarginX:F2} {cursorY - 50:F2} {ContentWidth:F2} 50 re B Q");

        EscribirTexto(s, MarginX + 14, cursorY - 18, tituloDoc, 12.0, true, 0.04, 0.18, 0.11);
        EscribirTexto(s, MarginX + 14, cursorY - 32, subtituloDoc, 7.8, false, 0.3, 0.4, 0.35);
        EscribirTexto(s, MarginX + 14, cursorY - 44, $"Fecha de Operación: {mov.FechaMovimiento:dd/MM/yyyy}  |  Estado: {mov.Estado.ToUpperInvariant()}  |  Método: {mov.MetodoPago}", 7.2, true, 0.09, 0.38, 0.23);

        cursorY -= 60.0;

        // 3. Aviso normativo obligatorio sobre Facturación y DIAN
        s.AppendLine("q 0.99 0.96 0.91 rg 0.85 0.65 0.2 RG 1 w");
        s.AppendLine($"{MarginX:F2} {cursorY - 32:F2} {ContentWidth:F2} 32 re B Q");

        EscribirTexto(s, MarginX + 10, cursorY - 12, "AVISO DE CONTROL INTERNO Y CUMPLIMIENTO TRIBUTARIO (DIAN):", 6.8, true, 0.55, 0.35, 0.05);
        EscribirTexto(s, MarginX + 10, cursorY - 21, "Este documento es un comprobante interno de control administrativo y asociativo emitido por ASOAGRO.", 6.4, false, 0.35, 0.25, 0.1);
        EscribirTexto(s, MarginX + 10, cursorY - 28, "No sustituye la Factura Electrónica de Venta validada por la DIAN (Art. 616-1 E.T.) salvo integración legal habilitada.", 6.4, false, 0.35, 0.25, 0.1);

        cursorY -= 40.0;

        // 4. Datos de la Asociación Titular y Régimen
        DibujarSeccionTitulo(s, MarginX, cursorY, "1. ASOCIACIÓN PRODUCTORA TITULAR DE LA CUENTA");
        cursorY -= 14.0;

        var nitAsoc = !string.IsNullOrWhiteSpace(mov.NitAsociacion) ? mov.NitAsociacion : (asoc?.Nit ?? "En trámite ante DIAN");
        var regimenAsoc = !string.IsNullOrWhiteSpace(mov.RegimenTributarioAsociacion) ? mov.RegimenTributarioAsociacion : (asoc?.RegimenTributario ?? "No responsable de IVA");
        var muniAsoc = asoc != null ? $"{asoc.Municipio}, {asoc.Departamento}" : "Urabá Antioqueño";

        DibujarFila2Col(s, MarginX, cursorY, "Nombre de la Asociación:", mov.NombreAsociacion, "NIT Institucional:", nitAsoc);
        cursorY -= 16.0;
        DibujarFila2Col(s, MarginX, cursorY, "Municipio y Región:", muniAsoc, "Régimen Tributario:", regimenAsoc);
        cursorY -= 16.0;
        DibujarFila2Col(s, MarginX, cursorY, "Obligación de Facturar:", mov.AsociacionObligadaAFacturar ? "Obligada a expedir factura electrónica" : "No obligada / Régimen especial agropecuario", "Proyecto o Actividad:", mov.ActividadOProyecto);
        cursorY -= 22.0;

        // 5. Tercero Relacionado (Proveedor, Cliente, Asociado)
        DibujarSeccionTitulo(s, MarginX, cursorY, "2. TERCERO INTERVINIENTE EN LA OPERACIÓN");
        cursorY -= 14.0;

        var nombreTercero = !string.IsNullOrWhiteSpace(mov.BeneficiarioOProveedor) ? mov.BeneficiarioOProveedor : "No especificado";
        var docTercero = !string.IsNullOrWhiteSpace(mov.DocumentoIdentidadTercero) ? mov.DocumentoIdentidadTercero : "Sin documento registrado";

        DibujarFila2Col(s, MarginX, cursorY, "Nombre o Razón Social:", nombreTercero, "Cédula o NIT del Tercero:", docTercero);
        cursorY -= 16.0;
        DibujarFila2Col(s, MarginX, cursorY, "Calidad del Tercero:", mov.TipoTercero, "Condición de Pago:", mov.MetodoPago);
        cursorY -= 22.0;

        // 6. Detalle del Concepto, Origen y Justificación del Gasto/Venta
        DibujarSeccionTitulo(s, MarginX, cursorY, "3. CONCEPTO, ORIGEN Y JUSTIFICACIÓN DE LA OPERACIÓN");
        cursorY -= 14.0;

        DibujarFila2Col(s, MarginX, cursorY, "Categoría / Subtipo:", $"{mov.Categoria} · {mov.Subtipo}", "Origen o Motivo:", string.IsNullOrWhiteSpace(mov.OrigenOMotivo) ? "Operación regular" : mov.OrigenOMotivo);
        cursorY -= 16.0;
        DibujarFila1Col(s, MarginX, cursorY, "Descripción de la Operación:", mov.Descripcion);
        cursorY -= 16.0;

        // Si es Costo o Gasto: Qué se pagó, a quién, para qué y de qué actividad proviene
        if (mov.Tipo == TipoMovimientoFinanciero.CostoGasto)
        {
            s.AppendLine("q 0.96 0.98 0.96 rg 0.7 0.85 0.7 RG 0.8 w");
            s.AppendLine($"{MarginX:F2} {cursorY - 48:F2} {ContentWidth:F2} 48 re B Q");

            EscribirTexto(s, MarginX + 10, cursorY - 12, "DETALLE ESPECÍFICO DEL DESEMBOLSO / GASTO:", 7.0, true, 0.09, 0.38, 0.23);
            EscribirTexto(s, MarginX + 10, cursorY - 22, $"• ¿Qué se pagó?: {Truncar(mov.QueSePago ?? mov.Descripcion, 75)}", 6.8, false, 0.1, 0.1, 0.1);
            EscribirTexto(s, MarginX + 10, cursorY - 32, $"• ¿A quién y Para qué?: {Truncar(nombreTercero, 35)} — {Truncar(mov.ParaQueSePago ?? "Destinación de apoyo agropecuario", 45)}", 6.8, false, 0.1, 0.1, 0.1);
            EscribirTexto(s, MarginX + 10, cursorY - 42, $"• Actividad o necesidad institucional: {Truncar(mov.ActividadONecesidad ?? mov.ActividadOProyecto, 75)}", 6.8, false, 0.1, 0.1, 0.1);

            cursorY -= 54.0;
        }

        // 7. Soportes Documentales y Facturación Anexa
        DibujarSeccionTitulo(s, MarginX, cursorY, "4. SOPORTES DOCUMENTALES Y FACTURACIÓN");
        cursorY -= 14.0;

        var detalleSoporte = !string.IsNullOrWhiteSpace(mov.ArchivoNombre) ? $"{mov.TipoSoporte} ({mov.ArchivoNombre})" : mov.TipoSoporte;
        DibujarFila2Col(s, MarginX, cursorY, "Tipo de Soporte Custodiado:", detalleSoporte, "Factura N° / Referencia:", string.IsNullOrWhiteSpace(mov.NumeroFactura) ? "Soporte interno sin nro. comercial" : mov.NumeroFactura);
        cursorY -= 16.0;

        if (!string.IsNullOrWhiteSpace(mov.CufeCude))
        {
            DibujarFila1Col(s, MarginX, cursorY, "Código CUFE / CUDE DIAN:", mov.CufeCude);
            cursorY -= 16.0;
        }

        if (!string.IsNullOrWhiteSpace(mov.ComprobantePagoNombre))
        {
            DibujarFila1Col(s, MarginX, cursorY, "Comprobante de Pago Anexo:", $"{mov.ComprobantePagoNombre} (Custodiado en archivo digital)");
            cursorY -= 16.0;
        }

        cursorY -= 8.0;

        // 8. Resumen de Valores en Pesos Colombianos (COP)
        s.AppendLine("q 0.08 0.33 0.2 rg");
        s.AppendLine($"{MarginX:F2} {cursorY - 44:F2} {ContentWidth:F2} 44 re f Q");

        EscribirTexto(s, MarginX + 16, cursorY - 16, "VALOR TOTAL DE LA OPERACIÓN (COP):", 8.2, true, 0.73, 0.91, 0.48);
        EscribirTexto(s, MarginX + 16, cursorY - 34, $"{mov.Monto.ToString("C0", new CultureInfo("es-CO"))} COP", 15.0, true, 1.0, 1.0, 1.0);

        EscribirTexto(s, PageWidth - MarginX - 220, cursorY - 16, $"Valor Pagado / Recaudado: {mov.ValorPagado.ToString("C0", new CultureInfo("es-CO"))}", 8.0, false, 0.9, 0.95, 0.9);
        EscribirTexto(s, PageWidth - MarginX - 220, cursorY - 30, $"Saldo Pendiente: {mov.SaldoPendiente.ToString("C0", new CultureInfo("es-CO"))}", 8.5, true, mov.SaldoPendiente > 0 ? 1.0 : 0.7, mov.SaldoPendiente > 0 ? 0.8 : 1.0, 0.7);

        cursorY -= 56.0;

        // 9. Bloque de Firmas y Responsables
        cursorY -= 16.0;
        double firmaWidth = (ContentWidth - 40.0) / 2.0;

        // Firma Tesorera
        s.AppendLine($"q 0.4 0.4 0.4 RG 0.8 w {MarginX:F2} {cursorY:F2} m {MarginX + firmaWidth:F2} {cursorY:F2} l S Q");
        EscribirTexto(s, MarginX, cursorY - 12, Truncar(funcionario, 36), 7.8, true, 0.1, 0.15, 0.12);
        EscribirTexto(s, MarginX, cursorY - 22, "Tesorería y Control Financiero", 6.8, false, 0.35, 0.4, 0.35);
        EscribirTexto(s, MarginX, cursorY - 30, $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}", 6.0, false, 0.5, 0.5, 0.5);

        // Firma Tercero / Beneficiario
        double xFirma2 = MarginX + firmaWidth + 40.0;
        s.AppendLine($"q 0.4 0.4 0.4 RG 0.8 w {xFirma2:F2} {cursorY:F2} m {xFirma2 + firmaWidth:F2} {cursorY:F2} l S Q");
        EscribirTexto(s, xFirma2, cursorY - 12, Truncar(nombreTercero, 36), 7.8, true, 0.1, 0.15, 0.12);
        EscribirTexto(s, xFirma2, cursorY - 22, $"Beneficiario / Tercero Interviniente ({mov.TipoTercero})", 6.8, false, 0.35, 0.4, 0.35);
        EscribirTexto(s, xFirma2, cursorY - 30, $"NIT / CC: {docTercero}", 6.0, false, 0.5, 0.5, 0.5);

        // Pie de página con folio y código
        s.AppendLine("q 0.85 0.85 0.85 RG 0.5 w");
        s.AppendLine($"{MarginX:F2} {BottomLimit + 14:F2} m {PageWidth - MarginX:F2} {BottomLimit + 14:F2} l S Q");
        EscribirTexto(s, MarginX, BottomLimit + 4, $"ASOAGRO · Sistema Integrado de Gestión Financiera y Documental · Consecutivo {mov.CodigoReferencia}", 6.2, false, 0.45, 0.5, 0.45);
        EscribirTexto(s, PageWidth - MarginX - 80, BottomLimit + 4, "Página 1 de 1", 6.2, false, 0.45, 0.5, 0.45);

        return CompilarDocumentoPdf(paginas);
    }

    /// <summary>
    /// Genera el reporte consolidado de Tesorería para una asociación y periodo determinados.
    /// </summary>
    public static byte[] GenerarReporteConsolidadoPdf(
        ResumenFinanciero resumen, 
        IEnumerable<MovimientoFinanciero> movimientos, 
        string asociacion, 
        string periodo, 
        string funcionario = "Tesorera General FAAUA")
    {
        var paginas = new List<PdfPageBuilder>();
        var pagina = new PdfPageBuilder();
        paginas.Add(pagina);
        var s = pagina.Stream;
        double cursorY = PageHeight - MarginX;

        // Franja institucional superior
        s.AppendLine("q 0.04 0.18 0.11 rg");
        s.AppendLine($"{MarginX:F2} {cursorY - 24:F2} {ContentWidth:F2} 24 re f Q");
        EscribirTexto(s, MarginX + 12, cursorY - 16, "ASOAGRO · FEDERACIÓN AGRARIA DE URABÁ · REPORTE FINANCIERO Y BALANCES", 8.5, true, 0.73, 0.91, 0.48);
        EscribirTexto(s, PageWidth - MarginX - 120, cursorY - 16, $"EMISIÓN: {DateTime.Now:dd/MM/yyyy}", 8.5, true, 1.0, 1.0, 1.0);
        cursorY -= 36.0;

        // Encabezado
        s.AppendLine("q 0.95 0.97 0.95 rg 0.09 0.38 0.23 RG 1.5 w");
        s.AppendLine($"{MarginX:F2} {cursorY - 44:F2} {ContentWidth:F2} 44 re B Q");
        EscribirTexto(s, MarginX + 14, cursorY - 16, "INFORME DE TESORERÍA, INGRESOS, COSTOS Y SALDOS", 11.5, true, 0.04, 0.18, 0.11);
        EscribirTexto(s, MarginX + 14, cursorY - 30, $"Asociación: {asociacion}  |  Periodo Contable: {periodo}", 7.8, false, 0.25, 0.35, 0.3);
        EscribirTexto(s, MarginX + 14, cursorY - 40, $"Generado por: {funcionario}  |  Movimientos analizados: {resumen.TotalMovimientos}", 7.0, true, 0.09, 0.38, 0.23);
        cursorY -= 54.0;

        // Tarjetas de balance (4 bloques)
        double cardW = (ContentWidth - 18.0) / 4.0;
        var culturaCop = new CultureInfo("es-CO");

        // 1. Ingresos
        DibujarMiniCard(s, MarginX, cursorY, cardW, "TOTAL INGRESOS", resumen.TotalIngresos.ToString("C0", culturaCop), 0.08, 0.45, 0.2);
        // 2. Costos y Gastos
        DibujarMiniCard(s, MarginX + cardW + 6, cursorY, cardW, "COSTOS Y GASTOS", resumen.TotalCostosYGastos.ToString("C0", culturaCop), 0.75, 0.15, 0.15);
        // 3. Cuentas por Cobrar
        DibujarMiniCard(s, MarginX + (cardW * 2) + 12, cursorY, cardW, "POR COBRAR", resumen.TotalCuentasPorCobrar.ToString("C0", culturaCop), 0.8, 0.5, 0.05);
        // 4. Saldo Neto
        DibujarMiniCard(s, MarginX + (cardW * 3) + 18, cursorY, cardW, "SALDO NETO", resumen.SaldoNeto.ToString("C0", culturaCop), resumen.SaldoNeto >= 0 ? 0.08 : 0.75, resumen.SaldoNeto >= 0 ? 0.45 : 0.15, 0.2);

        cursorY -= 48.0;

        // Tabla de Movimientos
        s.AppendLine("q 0.09 0.38 0.23 rg");
        s.AppendLine($"{MarginX:F2} {cursorY - 16:F2} {ContentWidth:F2} 16 re f Q");
        EscribirTexto(s, MarginX + 4, cursorY - 11, "CÓDIGO", 6.8, true, 1, 1, 1);
        EscribirTexto(s, MarginX + 68, cursorY - 11, "FECHA", 6.8, true, 1, 1, 1);
        EscribirTexto(s, MarginX + 125, cursorY - 11, "TIPO / CATEGORÍA", 6.8, true, 1, 1, 1);
        EscribirTexto(s, MarginX + 270, cursorY - 11, "TERCERO / PROVEEDOR", 6.8, true, 1, 1, 1);
        EscribirTexto(s, MarginX + 410, cursorY - 11, "ESTADO", 6.8, true, 1, 1, 1);
        EscribirTexto(s, MarginX + 465, cursorY - 11, "VALOR (COP)", 6.8, true, 1, 1, 1);
        cursorY -= 20.0;

        var lista = movimientos.Take(26).ToList();
        foreach (var m in lista)
        {
            if (cursorY < BottomLimit + 35.0) break;

            var esIngreso = m.Tipo == TipoMovimientoFinanciero.Ingreso;
            var r = esIngreso ? 0.05 : 0.65;
            var g = esIngreso ? 0.45 : 0.15;
            var b = esIngreso ? 0.15 : 0.15;

            EscribirTexto(s, MarginX + 4, cursorY, m.CodigoReferencia, 6.8, true, 0.1, 0.15, 0.12);
            EscribirTexto(s, MarginX + 68, cursorY, m.FechaMovimiento.ToString("dd/MM/yyyy"), 6.8, false, 0.3, 0.3, 0.3);
            EscribirTexto(s, MarginX + 125, cursorY, Truncar($"{m.Tipo}: {m.Categoria}", 28), 6.5, false, 0.15, 0.2, 0.15);
            EscribirTexto(s, MarginX + 270, cursorY, Truncar(m.BeneficiarioOProveedor ?? "-", 26), 6.5, false, 0.25, 0.25, 0.25);
            EscribirTexto(s, MarginX + 410, cursorY, Truncar(m.Estado, 12), 6.5, true, 0.3, 0.3, 0.3);
            EscribirTexto(s, MarginX + 465, cursorY, m.Monto.ToString("C0", culturaCop), 6.8, true, r, g, b);

            s.AppendLine($"q 0.9 0.9 0.9 RG 0.5 w {MarginX:F2} {cursorY - 3:F2} m {PageWidth - MarginX:F2} {cursorY - 3:F2} l S Q");
            cursorY -= 14.0;
        }

        // Pie de página
        s.AppendLine("q 0.85 0.85 0.85 RG 0.5 w");
        s.AppendLine($"{MarginX:F2} {BottomLimit + 14:F2} m {PageWidth - MarginX:F2} {BottomLimit + 14:F2} l S Q");
        EscribirTexto(s, MarginX, BottomLimit + 4, "ASOAGRO · Informe consolidado para supervisión de Tesorería, Presidencia y Órganos de Control", 6.2, false, 0.45, 0.5, 0.45);
        EscribirTexto(s, PageWidth - MarginX - 80, BottomLimit + 4, "Página 1 de 1", 6.2, false, 0.45, 0.5, 0.45);

        return CompilarDocumentoPdf(paginas);
    }

    private static void DibujarMiniCard(StringBuilder s, double x, double y, double w, string label, string val, double r, double g, double b)
    {
        s.AppendLine("q 0.98 0.98 0.98 rg 0.88 0.9 0.88 RG 0.8 w");
        s.AppendLine($"{x:F2} {y - 36:F2} {w:F2} 36 re B Q");
        EscribirTexto(s, x + 6, y - 12, label, 6.0, true, 0.4, 0.45, 0.4);
        EscribirTexto(s, x + 6, y - 26, val, 9.5, true, r, g, b);
    }

    private static void DibujarSeccionTitulo(StringBuilder s, double x, double y, string titulo)
    {
        EscribirTexto(s, x, y, titulo, 7.5, true, 0.09, 0.38, 0.23);
        s.AppendLine($"q 0.8 0.88 0.8 RG 0.8 w {x:F2} {y - 3:F2} m {PageWidth - MarginX:F2} {y - 3:F2} l S Q");
    }

    private static void DibujarFila2Col(StringBuilder s, double x, double y, string etq1, string val1, string etq2, string val2)
    {
        double colW = ContentWidth / 2.0;
        EscribirTexto(s, x, y, etq1.ToUpperInvariant(), 6.2, true, 0.45, 0.5, 0.45);
        EscribirTexto(s, x, y - 8.5, Truncar(val1, 52), 7.4, false, 0.1, 0.15, 0.12);

        EscribirTexto(s, x + colW, y, etq2.ToUpperInvariant(), 6.2, true, 0.45, 0.5, 0.45);
        EscribirTexto(s, x + colW, y - 8.5, Truncar(val2, 52), 7.4, false, 0.1, 0.15, 0.12);
    }

    private static void DibujarFila1Col(StringBuilder s, double x, double y, string etq, string val)
    {
        EscribirTexto(s, x, y, etq.ToUpperInvariant(), 6.2, true, 0.45, 0.5, 0.45);
        EscribirTexto(s, x, y - 8.5, Truncar(val, 110), 7.4, false, 0.1, 0.15, 0.12);
    }

    private static void EscribirTexto(StringBuilder s, double x, double y, string texto, double size, bool negrita, double r, double g, double b)
    {
        var textoLimpio = LimpiarTextoPdf(texto);
        var fuente = negrita ? "/F2" : "/F1";
        s.AppendLine($"BT {fuente} {size:F2} Tf {r:F3} {g:F3} {b:F3} rg 1 0 0 1 {x:F2} {y:F2} Tm ({textoLimpio}) Tj ET");
    }

    private static string Truncar(string texto, int maxLongitud)
    {
        if (string.IsNullOrEmpty(texto)) return "-";
        return texto.Length <= maxLongitud ? texto : texto.Substring(0, maxLongitud - 3) + "...";
    }

    private static string LimpiarTextoPdf(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";

        var sb = new StringBuilder(texto.Length);
        foreach (var ch in texto)
        {
            switch (ch)
            {
                case '\\': sb.Append("\\\\"); break;
                case '(': sb.Append("\\("); break;
                case ')': sb.Append("\\)"); break;
                case 'á': sb.Append('a'); break;
                case 'é': sb.Append('e'); break;
                case 'í': sb.Append('i'); break;
                case 'ó': sb.Append('o'); break;
                case 'ú': sb.Append('u'); break;
                case 'Á': sb.Append('A'); break;
                case 'É': sb.Append('E'); break;
                case 'Í': sb.Append('I'); break;
                case 'Ó': sb.Append('O'); break;
                case 'Ú': sb.Append('U'); break;
                case 'ñ': sb.Append('n'); break;
                case 'Ñ': sb.Append('N'); break;
                case '¿': case '¡': break;
                case '°': sb.Append('o'); break;
                default:
                    if (ch >= 32 && ch <= 126) sb.Append(ch);
                    else sb.Append(' ');
                    break;
            }
        }
        return sb.ToString();
    }

    private static byte[] CompilarDocumentoPdf(List<PdfPageBuilder> paginas)
    {
        using var ms = new System.IO.MemoryStream();
        using var writer = new System.IO.StreamWriter(ms, Encoding.ASCII);

        var offsets = new List<long>();
        void RegistrarObjeto(int id) => offsets.Add(ms.Position);

        writer.Write("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
        writer.Flush();

        // 1. Catálogo
        RegistrarObjeto(1);
        writer.Write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        writer.Flush();

        // 2. Colección de Páginas
        RegistrarObjeto(2);
        var kids = new StringBuilder();
        for (int i = 0; i < paginas.Count; i++)
        {
            kids.Append($"{5 + (i * 2)} 0 R ");
        }
        writer.Write($"2 0 obj\n<< /Type /Pages /Kids [ {kids}] /Count {paginas.Count} >>\nendobj\n");
        writer.Flush();

        // 3. Fuente Helvetica Normal
        RegistrarObjeto(3);
        writer.Write("3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
        writer.Flush();

        // 4. Fuente Helvetica Bold
        RegistrarObjeto(4);
        writer.Write("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");
        writer.Flush();

        // Páginas y Streams
        for (int i = 0; i < paginas.Count; i++)
        {
            var pageObjId = 5 + (i * 2);
            var contentObjId = pageObjId + 1;
            var streamBytes = Encoding.ASCII.GetBytes(paginas[i].Stream.ToString());

            // Page Object
            RegistrarObjeto(pageObjId);
            writer.Write($"{pageObjId} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [ 0 0 {PageWidth:F2} {PageHeight:F2} ] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjId} 0 R >>\nendobj\n");
            writer.Flush();

            // Content Stream Object
            RegistrarObjeto(contentObjId);
            writer.Write($"{contentObjId} 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n");
            writer.Flush();
            ms.Write(streamBytes, 0, streamBytes.Length);
            writer.Write("\nendstream\nendobj\n");
            writer.Flush();
        }

        // XRef
        var xrefPosition = ms.Position;
        var totalObjects = 5 + (paginas.Count * 2);
        writer.Write($"xref\n0 {totalObjects}\n0000000000 65535 f \n");
        foreach (var off in offsets)
        {
            writer.Write($"{off:D10} 00000 n \n");
        }

        // Trailer
        writer.Write($"trailer\n<< /Size {totalObjects} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF\n");
        writer.Flush();

        return ms.ToArray();
    }
}


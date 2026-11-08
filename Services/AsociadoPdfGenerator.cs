using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Asoagro.Models;

namespace Asoagro.Services;

/// <summary>
/// Motor tipográfico y de maquetación formal en PDF 1.4 (WinAnsi / Helvetica) para informes institucionales de ASOAGRO.
/// Evita el uso de recuadros por campo y prioriza secciones claras, redacción fluida en párrafos y tablas formales para listados.
/// </summary>
internal static class PdfInformeEngine
{
    public const double PageWidth = 612.0;   // 8.5 in (Carta)
    public const double PageHeight = 792.0;  // 11.0 in (Carta)
    public const double MarginX = 48.0;
    public const double ContentWidth = PageWidth - (MarginX * 2.0); // 516.0 pt
    public const double BottomLimit = 62.0;

    public sealed class PdfPageBuilder
    {
        public StringBuilder Stream { get; } = new();
    }

    public readonly record struct FragmentoTexto(string Texto, bool Negrita = false);

    public sealed class ColumnaTabla
    {
        public string Titulo { get; init; } = string.Empty;
        public double Ancho { get; init; }
        public bool AlinearDerecha { get; init; }
        public bool Negrita { get; init; }
    }

    public sealed class DocumentoInformeContexto
    {
        public List<PdfPageBuilder> Paginas { get; } = new();
        public PdfPageBuilder PaginaActual { get; private set; } = null!;
        public double CursorY { get; set; }

        public string AreaInstitucional { get; init; } = "SECRETARÍA GENERAL · GESTIÓN INSTITUCIONAL";
        public string TituloDocumento { get; init; } = "INFORME INSTITUCIONAL";
        public string SubtituloDocumento { get; init; } = string.Empty;
        public string ReferenciaDerecha { get; init; } = string.Empty;
        public string TituloCortoContinuacion { get; init; } = "INFORME INSTITUCIONAL";
        public string NotaPieLinea1 { get; init; } = string.Empty;
        public string NotaPieLinea2 { get; init; } = "La información y referencias documentales contenidas en este informe corresponden a los datos almacenados en el sistema.";
        public DateTime FechaGeneracion { get; init; } = DateTime.Now;

        public void IniciarDocumento()
        {
            NuevaPagina(esPrimera: true);
        }

        public void NuevaPagina(bool esPrimera)
        {
            PaginaActual = new PdfPageBuilder();
            Paginas.Add(PaginaActual);

            if (esPrimera)
            {
                DibujarEncabezadoPrincipal(PaginaActual.Stream, this);
                CursorY = PageHeight - 134.0;
            }
            else
            {
                DibujarEncabezadoContinuacion(PaginaActual.Stream, this);
                CursorY = PageHeight - 78.0;
            }
        }

        public bool AsegurarEspacio(double altoRequerido)
        {
            if (CursorY - altoRequerido < BottomLimit)
            {
                NuevaPagina(esPrimera: false);
                return true;
            }
            return false;
        }
    }

    public static void DibujarEncabezadoPrincipal(StringBuilder sb, DocumentoInformeContexto ctx)
    {
        // Franja superior institucional discreta y elegante
        RectanguloRelleno(sb, MarginX, PageHeight - 42.0, ContentWidth, 3.0, 0.08, 0.33, 0.18);

        // Membrete institucional izquierdo
        EscribirTexto(sb, true, 8.8, MarginX, PageHeight - 56.0, "ASOAGRO · FEDERACIÓN DE ASOCIACIONES AGROPECUARIAS DEL URABÁ (FAAUA)", 0.08, 0.33, 0.18);
        EscribirTexto(sb, false, 7.8, MarginX, PageHeight - 67.5, ctx.AreaInstitucional, 0.32, 0.38, 0.34);

        // Metadatos derechos alineados al margen derecho
        var textoFecha = $"Fecha de generación: {ctx.FechaGeneracion:dd/MM/yyyy HH:mm}";
        EscribirTextoDerecha(sb, false, 7.8, PageWidth - MarginX, PageHeight - 56.0, textoFecha, 0.28, 0.34, 0.30);

        if (!string.IsNullOrWhiteSpace(ctx.ReferenciaDerecha))
        {
            EscribirTextoDerecha(sb, true, 8.0, PageWidth - MarginX, PageHeight - 67.5, ctx.ReferenciaDerecha, 0.08, 0.33, 0.18);
        }

        // Línea divisoria fina entre membrete y título
        LineaHorizontal(sb, MarginX, PageHeight - 75.0, ContentWidth, 0.5, 0.80, 0.85, 0.82);

        // Título formal del informe
        EscribirTexto(sb, true, 13.0, MarginX, PageHeight - 93.0, ctx.TituloDocumento, 0.06, 0.20, 0.12);

        if (!string.IsNullOrWhiteSpace(ctx.SubtituloDocumento))
        {
            var lineasSub = EnvolverTextoPorAncho(ctx.SubtituloDocumento, ContentWidth, 8.6, false);
            var ySub = PageHeight - 106.5;
            foreach (var linea in lineasSub.Take(2))
            {
                EscribirTexto(sb, false, 8.6, MarginX, ySub, linea, 0.24, 0.30, 0.26);
                ySub -= 10.5;
            }
        }

        // Doble línea formal de cierre de encabezado
        LineaHorizontal(sb, MarginX, PageHeight - 118.0, ContentWidth, 1.0, 0.08, 0.33, 0.18);
        LineaHorizontal(sb, MarginX, PageHeight - 120.5, ContentWidth, 0.4, 0.72, 0.80, 0.75);
    }

    public static void DibujarEncabezadoContinuacion(StringBuilder sb, DocumentoInformeContexto ctx)
    {
        RectanguloRelleno(sb, MarginX, PageHeight - 40.0, ContentWidth, 2.0, 0.08, 0.33, 0.18);
        EscribirTexto(sb, true, 8.0, MarginX, PageHeight - 52.0, $"ASOAGRO · {ctx.TituloCortoContinuacion}", 0.08, 0.33, 0.18);
        EscribirTextoDerecha(sb, false, 7.6, PageWidth - MarginX, PageHeight - 52.0, $"Generado: {ctx.FechaGeneracion:dd/MM/yyyy HH:mm}", 0.35, 0.40, 0.37);
        LineaHorizontal(sb, MarginX, PageHeight - 58.0, ContentWidth, 0.6, 0.78, 0.83, 0.80);
    }

    public static void DibujarPiePaginas(DocumentoInformeContexto ctx)
    {
        var total = ctx.Paginas.Count;
        for (var i = 0; i < total; i++)
        {
            var sb = ctx.Paginas[i].Stream;
            LineaHorizontal(sb, MarginX, 46.0, ContentWidth, 0.6, 0.78, 0.83, 0.80);

            var linea1 = !string.IsNullOrWhiteSpace(ctx.NotaPieLinea1)
                ? ctx.NotaPieLinea1
                : $"ASOAGRO · Federación de Asociaciones Agropecuarias del Urabá (FAAUA) · Generado el {ctx.FechaGeneracion:dd/MM/yyyy a las HH:mm}.";

            EscribirTexto(sb, false, 7.2, MarginX, 34.5, linea1, 0.36, 0.42, 0.38);
            EscribirTexto(sb, false, 6.9, MarginX, 24.0, ctx.NotaPieLinea2, 0.45, 0.50, 0.47);
            EscribirTextoDerecha(sb, true, 8.0, PageWidth - MarginX, 30.0, $"Página {i + 1} de {total}", 0.08, 0.33, 0.18);
        }
    }

    public static void DibujarTituloSeccion(DocumentoInformeContexto ctx, string romanoONumero, string titulo)
    {
        // Evitar títulos huérfanos al final de la página (se requiere espacio para el título + al menos 3 líneas de contenido)
        ctx.AsegurarEspacio(64.0);
        ctx.CursorY -= 6.0;

        var yTexto = ctx.CursorY - 12.0;
        RectanguloRelleno(ctx.PaginaActual.Stream, MarginX, yTexto - 1.5, 3.0, 11.5, 0.08, 0.33, 0.18);
        EscribirTexto(
            ctx.PaginaActual.Stream,
            true,
            10.0,
            MarginX + 8.0,
            yTexto,
            $"{romanoONumero}. {titulo.ToUpperInvariant()}",
            0.08, 0.33, 0.18);

        var yLinea = yTexto - 5.0;
        LineaHorizontal(ctx.PaginaActual.Stream, MarginX, yLinea, ContentWidth, 0.6, 0.76, 0.83, 0.78);
        ctx.CursorY = yLinea - 11.0;
    }

    public static void DibujarSubtituloSeccion(DocumentoInformeContexto ctx, string subtitulo)
    {
        ctx.AsegurarEspacio(34.0);
        ctx.CursorY -= 2.0;
        EscribirTexto(ctx.PaginaActual.Stream, true, 9.0, MarginX, ctx.CursorY - 9.0, subtitulo, 0.12, 0.28, 0.19);
        ctx.CursorY -= 14.5;
    }

    public static void DibujarParrafo(DocumentoInformeContexto ctx, string texto, double fontSize = 9.3, double interlineado = 13.6, double espacioDespues = 6.5)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        DibujarParrafoMixto(ctx, new[] { new FragmentoTexto(texto, false) }, fontSize, interlineado, espacioDespues);
    }

    public static void DibujarParrafoMixto(
        DocumentoInformeContexto ctx,
        IEnumerable<FragmentoTexto> fragmentos,
        double fontSize = 9.3,
        double interlineado = 13.6,
        double espacioDespues = 6.5,
        double sangriaIzquierda = 0.0)
    {
        var anchoDisponible = ContentWidth - sangriaIzquierda - 2.0;
        var lineas = EnvolverFragmentosPorAncho(fragmentos, anchoDisponible, fontSize);
        if (lineas.Count == 0) return;

        // Si el párrafo tiene pocas líneas y no cabe completo, o si no caben al menos 2 líneas, saltar de página
        var altoMinimo = Math.Min(lineas.Count, 2) * interlineado;
        ctx.AsegurarEspacio(altoMinimo + 4.0);

        foreach (var linea in lineas)
        {
            ctx.AsegurarEspacio(interlineado + 2.0);
            var xActual = MarginX + sangriaIzquierda;
            var yTexto = ctx.CursorY - fontSize;

            foreach (var seg in linea)
            {
                EscribirTexto(
                    ctx.PaginaActual.Stream,
                    seg.Negrita,
                    fontSize,
                    xActual,
                    yTexto,
                    seg.Texto,
                    seg.Negrita ? 0.07 : 0.14,
                    seg.Negrita ? 0.16 : 0.19,
                    seg.Negrita ? 0.11 : 0.16);
                xActual += MedirAnchoTexto(seg.Texto, fontSize, seg.Negrita);
            }

            ctx.CursorY -= interlineado;
        }

        ctx.CursorY -= espacioDespues;
    }

    public static void DibujarItemVineta(
        DocumentoInformeContexto ctx,
        string etiquetaNegrita,
        string valorDetalle,
        double fontSize = 9.1)
    {
        var fragmentos = new List<FragmentoTexto>
        {
            new("• ", true),
            new($"{etiquetaNegrita}: ", true),
            new(string.IsNullOrWhiteSpace(valorDetalle) ? "No registrado" : valorDetalle, false)
        };
        DibujarParrafoMixto(ctx, fragmentos, fontSize, interlineado: 13.0, espacioDespues: 3.5, sangriaIzquierda: 6.0);
    }

    public static void DibujarResumenParesEnLineas(
        DocumentoInformeContexto ctx,
        IReadOnlyList<(string Etiqueta, string Valor)> pares)
    {
        if (pares.Count == 0) return;

        var colWidth = (ContentWidth - 18.0) / 2.0;
        for (var i = 0; i < pares.Count; i += 2)
        {
            var izq = pares[i];
            var tieneDer = i + 1 < pares.Count;
            var der = tieneDer ? pares[i + 1] : default;

            var fragIzq = new[]
            {
                new FragmentoTexto($"{izq.Etiqueta}: ", true),
                new FragmentoTexto(string.IsNullOrWhiteSpace(izq.Valor) ? "No registrado" : izq.Valor, false)
            };
            var lineasIzq = EnvolverFragmentosPorAncho(fragIzq, colWidth - 4.0, 8.9);

            var lineasDer = new List<List<FragmentoTexto>>();
            if (tieneDer)
            {
                var fragDer = new[]
                {
                    new FragmentoTexto($"{der.Etiqueta}: ", true),
                    new FragmentoTexto(string.IsNullOrWhiteSpace(der.Valor) ? "No registrado" : der.Valor, false)
                };
                lineasDer = EnvolverFragmentosPorAncho(fragDer, colWidth - 4.0, 8.9);
            }

            var maxLineas = Math.Max(lineasIzq.Count, lineasDer.Count);
            var altoFila = (maxLineas * 12.2) + 3.0;
            ctx.AsegurarEspacio(altoFila);

            for (var l = 0; l < maxLineas; l++)
            {
                var yLinea = ctx.CursorY - 8.9 - (l * 12.2);
                if (l < lineasIzq.Count)
                {
                    var x = MarginX;
                    foreach (var seg in lineasIzq[l])
                    {
                        EscribirTexto(ctx.PaginaActual.Stream, seg.Negrita, 8.9, x, yLinea, seg.Texto,
                            seg.Negrita ? 0.10 : 0.16, seg.Negrita ? 0.22 : 0.20, seg.Negrita ? 0.15 : 0.18);
                        x += MedirAnchoTexto(seg.Texto, 8.9, seg.Negrita);
                    }
                }

                if (tieneDer && l < lineasDer.Count)
                {
                    var x = MarginX + colWidth + 18.0;
                    foreach (var seg in lineasDer[l])
                    {
                        EscribirTexto(ctx.PaginaActual.Stream, seg.Negrita, 8.9, x, yLinea, seg.Texto,
                            seg.Negrita ? 0.10 : 0.16, seg.Negrita ? 0.22 : 0.20, seg.Negrita ? 0.15 : 0.18);
                        x += MedirAnchoTexto(seg.Texto, 8.9, seg.Negrita);
                    }
                }
            }

            ctx.CursorY -= altoFila;
        }

        ctx.CursorY -= 4.0;
    }

    public static void DibujarTablaFormal(
        DocumentoInformeContexto ctx,
        IReadOnlyList<ColumnaTabla> columnas,
        IReadOnlyList<string[]> filas,
        double fontSizeCeldas = 8.3)
    {
        if (columnas.Count == 0 || filas.Count == 0) return;

        const double paddingX = 5.0;
        const double lineHeightCelda = 11.0;

        // Pre-calcular las líneas de encabezado
        var encabezadosEnvueltos = columnas
            .Select(c => EnvolverTextoPorAncho(c.Titulo, Math.Max(20.0, c.Ancho - (paddingX * 2.0)), 7.8, true))
            .ToList();
        var maxLineasCab = encabezadosEnvueltos.Max(l => Math.Max(1, l.Count));
        var altoCabecera = (maxLineasCab * 10.0) + 8.0;

        // Medir la primera fila para evitar que el encabezado de la tabla quede huérfano
        var primeraFilaLineas = MedirLineasFila(columnas, filas[0], paddingX, fontSizeCeldas);
        var altoPrimeraFila = (primeraFilaLineas.Max(l => Math.Max(1, l.Count)) * lineHeightCelda) + 7.5;

        ctx.AsegurarEspacio(altoCabecera + altoPrimeraFila + 6.0);

        void PintarCabeceraTabla()
        {
            var yTop = ctx.CursorY;
            var yBottom = yTop - altoCabecera;

            RectanguloRelleno(ctx.PaginaActual.Stream, MarginX, yBottom, ContentWidth, altoCabecera, 0.93, 0.96, 0.94);
            LineaHorizontal(ctx.PaginaActual.Stream, MarginX, yTop, ContentWidth, 0.9, 0.08, 0.33, 0.18);
            LineaHorizontal(ctx.PaginaActual.Stream, MarginX, yBottom, ContentWidth, 0.7, 0.08, 0.33, 0.18);

            var xCol = MarginX;
            for (var c = 0; c < columnas.Count; c++)
            {
                var col = columnas[c];
                var lineasCol = encabezadosEnvueltos[c];
                for (var l = 0; l < lineasCol.Count; l++)
                {
                    var yTexto = yTop - 10.5 - (l * 10.0);
                    if (col.AlinearDerecha)
                    {
                        EscribirTextoDerecha(ctx.PaginaActual.Stream, true, 7.8, xCol + col.Ancho - paddingX, yTexto, lineasCol[l], 0.08, 0.28, 0.16);
                    }
                    else
                    {
                        EscribirTexto(ctx.PaginaActual.Stream, true, 7.8, xCol + paddingX, yTexto, lineasCol[l], 0.08, 0.28, 0.16);
                    }
                }
                xCol += col.Ancho;
            }

            ctx.CursorY = yBottom;
        }

        PintarCabeceraTabla();

        for (var idxFila = 0; idxFila < filas.Count; idxFila++)
        {
            var celdasEnvueltas = MedirLineasFila(columnas, filas[idxFila], paddingX, fontSizeCeldas);
            var maxLineasFila = celdasEnvueltas.Max(l => Math.Max(1, l.Count));
            var altoFila = (maxLineasFila * lineHeightCelda) + 7.5;

            if (ctx.CursorY - altoFila < BottomLimit)
            {
                ctx.NuevaPagina(esPrimera: false);
                PintarCabeceraTabla();
            }

            var yTopFila = ctx.CursorY;
            var yBottomFila = yTopFila - altoFila;

            if (idxFila % 2 == 1)
            {
                RectanguloRelleno(ctx.PaginaActual.Stream, MarginX, yBottomFila, ContentWidth, altoFila, 0.98, 0.99, 0.98);
            }

            var xCol = MarginX;
            for (var c = 0; c < columnas.Count; c++)
            {
                var col = columnas[c];
                var lineasCelda = celdasEnvueltas[c];
                for (var l = 0; l < lineasCelda.Count; l++)
                {
                    var yTexto = yTopFila - 11.0 - (l * lineHeightCelda);
                    if (col.AlinearDerecha)
                    {
                        EscribirTextoDerecha(
                            ctx.PaginaActual.Stream,
                            col.Negrita,
                            fontSizeCeldas,
                            xCol + col.Ancho - paddingX,
                            yTexto,
                            lineasCelda[l],
                            0.12, 0.17, 0.14);
                    }
                    else
                    {
                        EscribirTexto(
                            ctx.PaginaActual.Stream,
                            col.Negrita,
                            fontSizeCeldas,
                            xCol + paddingX,
                            yTexto,
                            lineasCelda[l],
                            0.12, 0.17, 0.14);
                    }
                }
                xCol += col.Ancho;
            }

            LineaHorizontal(ctx.PaginaActual.Stream, MarginX, yBottomFila, ContentWidth, 0.4, 0.84, 0.88, 0.85);
            ctx.CursorY = yBottomFila;
        }

        ctx.CursorY -= 8.0;
    }

    private static List<List<string>> MedirLineasFila(
        IReadOnlyList<ColumnaTabla> columnas,
        string[] valoresFila,
        double paddingX,
        double fontSize)
    {
        var resultado = new List<List<string>>(columnas.Count);
        for (var c = 0; c < columnas.Count; c++)
        {
            var texto = c < valoresFila.Length && !string.IsNullOrWhiteSpace(valoresFila[c])
                ? valoresFila[c]
                : "No registrado";
            var anchoMax = Math.Max(18.0, columnas[c].Ancho - (paddingX * 2.0) - 2.0);
            resultado.Add(EnvolverTextoPorAncho(texto, anchoMax, fontSize, columnas[c].Negrita));
        }
        return resultado;
    }

    public static List<string> EnvolverTextoPorAncho(string texto, double maxWidthPt, double fontSize, bool negrita)
    {
        var lineasFragmentos = EnvolverFragmentosPorAncho(
            new[] { new FragmentoTexto(string.IsNullOrWhiteSpace(texto) ? "No registrado" : texto, negrita) },
            maxWidthPt,
            fontSize);

        return lineasFragmentos
            .Select(l => string.Concat(l.Select(s => s.Texto)))
            .ToList();
    }

    public static List<List<FragmentoTexto>> EnvolverFragmentosPorAncho(
        IEnumerable<FragmentoTexto> fragmentos,
        double maxWidthPt,
        double fontSize)
    {
        var tokens = new List<(string Palabra, bool Negrita, bool EspacioAntes)>();

        foreach (var frag in fragmentos)
        {
            if (string.IsNullOrEmpty(frag.Texto)) continue;
            var normalizado = frag.Texto.Replace("\r", " ").Replace("\n", " ");
            var empiezaConEspacio = normalizado.StartsWith(' ');
            var terminaConEspacio = normalizado.EndsWith(' ');
            var partes = normalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < partes.Length; i++)
            {
                var espacioAntes = i > 0 || empiezaConEspacio || (tokens.Count > 0 && tokens[^1].Palabra.EndsWith(' '));
                var SubPartes = PartirPalabraLarga(partes[i], maxWidthPt, fontSize, frag.Negrita);
                for (var s = 0; s < SubPartes.Count; s++)
                {
                    tokens.Add((SubPartes[s], frag.Negrita, s == 0 && espacioAntes));
                }
            }

            if (terminaConEspacio && tokens.Count > 0)
            {
                var ult = tokens[^1];
                tokens[^1] = (ult.Palabra + " ", ult.Negrita, ult.EspacioAntes);
            }
        }

        var lineas = new List<List<FragmentoTexto>>();
        var lineaActual = new List<FragmentoTexto>();
        var anchoActual = 0.0;

        foreach (var tk in tokens)
        {
            var palabraLimpia = tk.Palabra.TrimEnd(' ');
            if (palabraLimpia.Length == 0) continue;

            var necesitaEspacio = lineaActual.Count > 0 && tk.EspacioAntes;
            var textoConEspacio = necesitaEspacio ? " " + palabraLimpia : palabraLimpia;
            var anchoToken = MedirAnchoTexto(textoConEspacio, fontSize, tk.Negrita);

            if (lineaActual.Count > 0 && anchoActual + anchoToken > maxWidthPt)
            {
                lineas.Add(CompactarSegmentosLinea(lineaActual));
                lineaActual = new List<FragmentoTexto> { new(palabraLimpia, tk.Negrita) };
                anchoActual = MedirAnchoTexto(palabraLimpia, fontSize, tk.Negrita);
            }
            else
            {
                lineaActual.Add(new FragmentoTexto(textoConEspacio, tk.Negrita));
                anchoActual += anchoToken;
            }
        }

        if (lineaActual.Count > 0)
        {
            lineas.Add(CompactarSegmentosLinea(lineaActual));
        }

        return lineas;
    }

    private static List<FragmentoTexto> CompactarSegmentosLinea(List<FragmentoTexto> segmentos)
    {
        var resultado = new List<FragmentoTexto>();
        foreach (var seg in segmentos)
        {
            if (resultado.Count > 0 && resultado[^1].Negrita == seg.Negrita)
            {
                resultado[^1] = new FragmentoTexto(resultado[^1].Texto + seg.Texto, seg.Negrita);
            }
            else
            {
                resultado.Add(seg);
            }
        }
        return resultado;
    }

    private static List<string> PartirPalabraLarga(string palabra, double maxWidthPt, double fontSize, bool negrita)
    {
        if (MedirAnchoTexto(palabra, fontSize, negrita) <= maxWidthPt)
        {
            return new List<string> { palabra };
        }

        var partes = new List<string>();
        var actual = new StringBuilder();
        foreach (var ch in palabra)
        {
            actual.Append(ch);
            if (MedirAnchoTexto(actual.ToString(), fontSize, negrita) > maxWidthPt - 4.0 && actual.Length > 1)
            {
                var ultimo = actual[^1];
                actual.Length -= 1;
                partes.Add(actual.ToString());
                actual.Clear();
                actual.Append(ultimo);
            }
            else if (ch is '_' or '-' or '/' or '.' && MedirAnchoTexto(actual.ToString(), fontSize, negrita) >= maxWidthPt * 0.65)
            {
                partes.Add(actual.ToString());
                actual.Clear();
            }
        }

        if (actual.Length > 0)
        {
            partes.Add(actual.ToString());
        }

        return partes;
    }

    public static double MedirAnchoTexto(string texto, double fontSize, bool negrita)
    {
        if (string.IsNullOrEmpty(texto)) return 0.0;
        var sumaMilesimas = 0;
        foreach (var ch in texto)
        {
            sumaMilesimas += ObtenerAnchoCaracterMilesimas(ch, negrita);
        }
        return (sumaMilesimas / 1000.0) * fontSize;
    }

    private static int ObtenerAnchoCaracterMilesimas(char ch, bool negrita)
    {
        if (ch == ' ') return 278;
        if (ch is 'i' or 'l' or 'í' or 'ï' or '!' or '¡' or '|' or ':' or ';' or '.' or ',' or '\'' or '‘' or '’')
            return negrita ? 295 : 260;
        if (ch is 'I' or 'j' or 'f' or 't' or 'r' or '(' or ')' or '[' or ']' or '-' or '–')
            return negrita ? 360 : 320;
        if (ch is 'm' or 'w' or 'M' or 'W' or '@' or '%' or '—')
            return negrita ? 920 : 860;
        if (char.IsUpper(ch) || ch is 'Á' or 'É' or 'Í' or 'Ó' or 'Ú' or 'Ñ' or 'Ü')
            return negrita ? 720 : 680;
        if (char.IsDigit(ch) || ch is '$' or '#' or '+' or '=' or '?')
            return negrita ? 580 : 556;
        return negrita ? 585 : 545;
    }

    public static void RectanguloRelleno(StringBuilder sb, double x, double y, double w, double h, double r, double g, double b)
    {
        sb.AppendFormat(
            CultureInfo.InvariantCulture,
            "q {0:0.###} {1:0.###} {2:0.###} rg {3:0.##} {4:0.##} {5:0.##} {6:0.##} re f Q\n",
            r, g, b, x, y, w, h);
    }

    public static void LineaHorizontal(StringBuilder sb, double x, double y, double w, double grosor, double r, double g, double b)
    {
        sb.AppendFormat(
            CultureInfo.InvariantCulture,
            "q {0:0.###} {1:0.###} {2:0.###} RG {3:0.##} w {4:0.##} {5:0.##} m {6:0.##} {5:0.##} l S Q\n",
            r, g, b, grosor, x, y, x + w);
    }

    public static void EscribirTexto(
        StringBuilder sb,
        bool negrita,
        double fontSize,
        double x,
        double y,
        string texto,
        double r,
        double g,
        double b)
    {
        if (string.IsNullOrEmpty(texto)) return;
        var fontRef = negrita ? "/F2" : "/F1";
        var hex = ConvertirAHexWinAnsi(texto);
        sb.AppendFormat(
            CultureInfo.InvariantCulture,
            "BT {0} {1:0.##} Tf {2:0.###} {3:0.###} {4:0.###} rg 1 0 0 1 {5:0.##} {6:0.##} Tm <{7}> Tj ET\n",
            fontRef, fontSize, r, g, b, x, y, hex);
    }

    public static void EscribirTextoDerecha(
        StringBuilder sb,
        bool negrita,
        double fontSize,
        double xDerecha,
        double y,
        string texto,
        double r,
        double g,
        double b)
    {
        if (string.IsNullOrEmpty(texto)) return;
        var ancho = MedirAnchoTexto(texto, fontSize, negrita);
        EscribirTexto(sb, negrita, fontSize, xDerecha - ancho, y, texto, r, g, b);
    }

    public static string ConvertirAHexWinAnsi(string texto)
    {
        var sb = new StringBuilder(texto.Length * 2);
        foreach (var ch in texto)
        {
            byte b = ch switch
            {
                '\u20AC' => 0x80,
                '\u2026' => 0x85,
                '\u2018' => 0x91,
                '\u2019' => 0x92,
                '\u201C' => 0x93,
                '\u201D' => 0x94,
                '\u2022' => 0x95,
                '\u2013' => 0x96,
                '\u2014' => 0x97,
                <= '\u00FF' => (byte)ch,
                _ => (byte)'?'
            };
            sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    public static byte[] CompilarPdf(
        IReadOnlyList<PdfPageBuilder> paginas,
        IReadOnlyList<(string NombreArchivo, string Descripcion, byte[] Contenido)>? adjuntosBinarios = null)
    {
        using var ms = new MemoryStream();
        void WriteAscii(string s)
        {
            var bytes = Encoding.ASCII.GetBytes(s);
            ms.Write(bytes, 0, bytes.Length);
        }

        var adjuntos = adjuntosBinarios ?? Array.Empty<(string, string, byte[])>();
        var baseAdjuntos = 5 + (paginas.Count * 2);
        var totalObjetos = 4 + (paginas.Count * 2) + (adjuntos.Count * 2);
        var offsets = new long[totalObjetos + 1];

        WriteAscii("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");

        offsets[1] = ms.Position;
        if (adjuntos.Count > 0)
        {
            var namesEntries = new StringBuilder();
            for (var k = 0; k < adjuntos.Count; k++)
            {
                var filespecObjId = baseAdjuntos + (k * 2) + 1;
                var claveUnica = $"{k + 1:00}_{adjuntos[k].NombreArchivo}";
                namesEntries.Append($"<{ConvertirAHexWinAnsi(claveUnica)}> {filespecObjId} 0 R ");
            }
            WriteAscii($"1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles << /Names [ {namesEntries}] >> >> >>\nendobj\n");
        }
        else
        {
            WriteAscii("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        }

        var kidsRefs = string.Join(" ", Enumerable.Range(0, paginas.Count).Select(i => $"{5 + (i * 2)} 0 R"));
        offsets[2] = ms.Position;
        WriteAscii($"2 0 obj\n<< /Type /Pages /Kids [ {kidsRefs} ] /Count {paginas.Count} >>\nendobj\n");

        offsets[3] = ms.Position;
        WriteAscii("3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");

        offsets[4] = ms.Position;
        WriteAscii("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

        for (var i = 0; i < paginas.Count; i++)
        {
            var pageObjId = 5 + (i * 2);
            var contentObjId = 6 + (i * 2);
            var contenidoBytes = Encoding.ASCII.GetBytes(paginas[i].Stream.ToString());

            offsets[pageObjId] = ms.Position;
            WriteAscii(
                $"{pageObjId} 0 obj\n" +
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] " +
                $"/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> " +
                $"/Contents {contentObjId} 0 R >>\n" +
                $"endobj\n");

            offsets[contentObjId] = ms.Position;
            WriteAscii($"{contentObjId} 0 obj\n<< /Length {contenidoBytes.Length} >>\nstream\n");
            ms.Write(contenidoBytes, 0, contenidoBytes.Length);
            WriteAscii("\nendstream\nendobj\n");
        }

        for (var k = 0; k < adjuntos.Count; k++)
        {
            var adj = adjuntos[k];
            var rawBytes = adj.Contenido;
            var efObjId = baseAdjuntos + (k * 2);
            var fsObjId = baseAdjuntos + (k * 2) + 1;

            offsets[efObjId] = ms.Position;
            WriteAscii($"{efObjId} 0 obj\n<< /Type /EmbeddedFile /Params << /Size {rawBytes.Length} >> >>\nstream\n");
            ms.Write(rawBytes, 0, rawBytes.Length);
            WriteAscii("\nendstream\nendobj\n");

            var hexNombre = ConvertirAHexWinAnsi(adj.NombreArchivo);
            var hexDesc = ConvertirAHexWinAnsi(adj.Descripcion);
            offsets[fsObjId] = ms.Position;
            WriteAscii(
                $"{fsObjId} 0 obj\n" +
                $"<< /Type /Filespec /F <{hexNombre}> /UF <{hexNombre}> /EF << /F {efObjId} 0 R >> /Desc <{hexDesc}> >>\n" +
                $"endobj\n");
        }

        var xrefPos = ms.Position;
        WriteAscii($"xref\n0 {totalObjetos + 1}\n");
        WriteAscii("0000000000 65535 f \n");
        for (var id = 1; id <= totalObjetos; id++)
        {
            WriteAscii($"{offsets[id]:0000000000} 00000 n \n");
        }

        WriteAscii(
            $"trailer\n<< /Size {totalObjetos + 1} /Root 1 0 R >>\n" +
            $"startxref\n{xrefPos}\n%%EOF");

        return ms.ToArray();
    }
}

public static class AsociadoPdfGenerator
{
    private sealed class DocumentoAdjuntoPdf
    {
        public string Seccion { get; init; } = string.Empty;
        public string Respaldo { get; init; } = string.Empty;
        public string NombreArchivo { get; init; } = string.Empty;
        public string Tamano { get; init; } = string.Empty;
        public string Detalle { get; init; } = string.Empty;
        public string MimeType { get; init; } = "application/octet-stream";
        public byte[]? ContenidoBytes { get; init; }
        public bool TieneArchivoBinario => ContenidoBytes is { Length: > 0 };
    }

    public static byte[] GenerarFichaPdf(AsociadoModel asociado, DateTime? fechaGeneracion = null)
    {
        var generadoEn = fechaGeneracion ?? DateTime.Now;
        var documentosAdjuntos = RecopilarDocumentosAdjuntos(asociado);

        var nombreTitular = ValorONoRegistrado(asociado.NombreCompleto);
        var tipoDoc = string.IsNullOrWhiteSpace(asociado.TipoDocumentoIdentidad) ? "CC/NIT" : asociado.TipoDocumentoIdentidad.Trim();
        var numDoc = ValorONoRegistrado(asociado.Cedula);

        var estadoMembresiaTexto = asociado.EstadoMembresia switch
        {
            "AlDia" => "Activo · Al día",
            "Pendiente" => "En revisión · Período de gracia",
            _ => string.IsNullOrWhiteSpace(asociado.EstadoMembresia) ? "Activo" : asociado.EstadoMembresia
        };

        var ctx = new PdfInformeEngine.DocumentoInformeContexto
        {
            AreaInstitucional = "SECRETARÍA GENERAL · PADRÓN Y REGISTRO DE PRODUCTORES ASOCIADOS",
            TituloDocumento = "INFORME DE REGISTRO DEL ASOCIADO",
            SubtituloDocumento = $"Titular: {nombreTitular}  |  Identificación ({tipoDoc}): {numDoc}  |  Estado de membresía: {estadoMembresiaTexto}",
            ReferenciaDerecha = $"Registro {tipoDoc}: {numDoc}",
            TituloCortoContinuacion = $"INFORME DE ASOCIADO — {nombreTitular.ToUpperInvariant()} ({numDoc})",
            NotaPieLinea1 = $"Informe formal generado por el sistema ASOAGRO el {generadoEn:dd/MM/yyyy a las HH:mm} · Asociado: {nombreTitular} ({tipoDoc} {numDoc}).",
            NotaPieLinea2 = "Los datos y estados de archivos citados reflejan la información almacenada en el sistema y no constituyen validación legal externa.",
            FechaGeneracion = generadoEn
        };

        ctx.IniciarDocumento();

        // =====================================================================
        // SECCIÓN I: IDENTIFICACIÓN DEL ASOCIADO, VINCULACIÓN Y CONTACTO
        // =====================================================================
        PdfInformeEngine.DibujarTituloSeccion(ctx, "I", "Identificación del Asociado, Vinculación y Contacto");

        var tieneAsociacion = !string.IsNullOrWhiteSpace(asociado.AsociacionPertenece);
        var asocTexto = tieneAsociacion ? asociado.AsociacionPertenece.Trim() : "No registrado";
        var vinculacionTexto = tieneAsociacion ? "Activa y confirmada" : "Pendiente de formalizar";
        var fechaRegTexto = asociado.FechaRegistro.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

        var parrafoIdentificacion = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("El presente informe consolida la información registrada en el sistema ASOAGRO correspondiente al productor asociado "),
            new(nombreTitular, true),
            new($", identificado(a) con {tipoDoc} número "),
            new(numDoc, true),
            new(asociado.EsJuridica ? " (registrado como persona jurídica)" : ""),
            new(", cuya inscripción en la plataforma fue realizada el "),
            new(fechaRegTexto, true),
            new(". ")
        };

        if (tieneAsociacion)
        {
            parrafoIdentificacion.Add(new("El titular pertenece a la organización federada "));
            parrafoIdentificacion.Add(new(asocTexto, true));
            parrafoIdentificacion.Add(new(", manteniendo una vinculación asociativa "));
            parrafoIdentificacion.Add(new(vinculacionTexto.ToLowerInvariant(), true));
            parrafoIdentificacion.Add(new(" y estado de membresía "));
            parrafoIdentificacion.Add(new(estadoMembresiaTexto, true));
            parrafoIdentificacion.Add(new("."));
        }
        else
        {
            parrafoIdentificacion.Add(new("En la actualidad, la asignación a una asociación federada figura como "));
            parrafoIdentificacion.Add(new("No registrado", true));
            parrafoIdentificacion.Add(new($" ({vinculacionTexto.ToLowerInvariant()}), manteniendo estado de membresía "));
            parrafoIdentificacion.Add(new(estadoMembresiaTexto, true));
            parrafoIdentificacion.Add(new("."));
        }

        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoIdentificacion);

        var parrafoContacto = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("Para efectos de comunicación institucional y notificaciones, registra el teléfono o WhatsApp "),
            new(ValorONoRegistrado(asociado.Telefono), true),
            new(" y la dirección de correo electrónico "),
            new(ValorONoRegistrado(asociado.Correo), true),
            new(". ")
        };

        if (!string.IsNullOrWhiteSpace(asociado.CedulaArchivoNombre))
        {
            var tamCedula = string.IsNullOrWhiteSpace(asociado.CedulaArchivoTamano)
                ? "archivo almacenado en el sistema"
                : $"tamaño {asociado.CedulaArchivoTamano}, almacenado en el sistema";
            parrafoContacto.Add(new("Como soporte documental de identificación se encuentra registrado el archivo "));
            parrafoContacto.Add(new(asociado.CedulaArchivoNombre, true));
            parrafoContacto.Add(new($" ({tamCedula})."));
        }
        else
        {
            parrafoContacto.Add(new("Respecto al soporte documental de identificación (fotocopia de cédula o NIT), su estado actual en el expediente es "));
            parrafoContacto.Add(new("No registrado", true));
            parrafoContacto.Add(new("."));
        }

        if (!string.IsNullOrWhiteSpace(asociado.RutArchivoNombre))
        {
            var tamRut = string.IsNullOrWhiteSpace(asociado.RutArchivoTamano) ? "almacenado en el sistema" : asociado.RutArchivoTamano;
            parrafoContacto.Add(new(" Adicionalmente, se registra el archivo de RUT "));
            parrafoContacto.Add(new($"{asociado.RutArchivoNombre} ({tamRut})", true));
            parrafoContacto.Add(new("."));
        }

        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoContacto);

        // =====================================================================
        // SECCIÓN II: INFORMACIÓN Y UBICACIÓN DEL PREDIO RURAL
        // =====================================================================
        PdfInformeEngine.DibujarTituloSeccion(ctx, "II", "Información y Ubicación del Predio Rural");

        var nombrePredio = ValorONoRegistrado(asociado.NombrePredio);
        var veredaSector = ValorONoRegistrado(asociado.VeredaSector);
        var municipioTexto = FormatearMunicipio(asociado.Municipio);
        var tenenciaTexto = ValorONoRegistrado(asociado.TenenciaTerreno);
        var extensionTexto = $"{asociado.HectareasPredio:0.#} hectáreas";

        var parrafoPredio = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("El asociado desarrolla su actividad agropecuaria en el predio rural denominado "),
            new(nombrePredio, true),
            new(", ubicado en la vereda o sector "),
            new(veredaSector, true),
            new(", jurisdicción del municipio de "),
            new(municipioTexto, true),
            new(!string.IsNullOrWhiteSpace(asociado.Departamento) ? $" ({asociado.Departamento})" : ""),
            new(". La tenencia del terreno se reporta bajo la modalidad "),
            new(tenenciaTexto, true),
            new(" y cuenta con una extensión total registrada de "),
            new(extensionTexto, true)
        };

        if (asociado.Latitud.HasValue && asociado.Longitud.HasValue)
        {
            parrafoPredio.Add(new($", con coordenadas geográficas de referencia ({asociado.Latitud.Value:0.#####}, {asociado.Longitud.Value:0.#####})."));
        }
        else
        {
            parrafoPredio.Add(new("."));
        }

        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoPredio);

        // =====================================================================
        // SECCIÓN III: LÍNEA PRODUCTIVA Y BUENAS PRÁCTICAS AGRÍCOLAS (BPA)
        // =====================================================================
        PdfInformeEngine.DibujarTituloSeccion(ctx, "III", "Actividad Productiva y Buenas Prácticas Agrícolas (BPA)");

        var actividadTexto = ValorONoRegistrado(asociado.TipoActividadProductiva);
        var cultivosTexto = AsociadoModel.FormatearNombreCultivo(asociado.CultivoPrincipal);
        if (string.Equals(cultivosTexto, "Sin definir", StringComparison.OrdinalIgnoreCase))
        {
            cultivosTexto = "No registrado";
        }
        var produccionTexto = ValorONoRegistrado(asociado.ProduccionAproximada);
        var haProductivas = asociado.HectareasLineaProductiva > 0 ? asociado.HectareasLineaProductiva : asociado.HectareasPredio;

        var parrafoProductivo = new List<PdfInformeEngine.FragmentoTexto>
        {
            new("La unidad productiva registra como vocación o actividad principal la línea "),
            new(actividadTexto, true),
            new(", con cultivos o renglones productivos establecidos en "),
            new(cultivosTexto, true),
            new(" sobre un área productiva de "),
            new($"{haProductivas:0.#} hectáreas", true),
            new($" (de las {asociado.HectareasPredio:0.#} hectáreas totales del predio), y una producción aproximada estimada en "),
            new(produccionTexto, true),
            new(". ")
        };

        if (asociado.TieneBPA)
        {
            parrafoProductivo.Add(new("En relación con las Buenas Prácticas Agrícolas (BPA), el registro indica que el asociado "));
            parrafoProductivo.Add(new("sí cuenta con certificación BPA", true));
            if (!string.IsNullOrWhiteSpace(asociado.CertificadoBpaNombre))
            {
                var tamBpa = string.IsNullOrWhiteSpace(asociado.CertificadoBpaTamano) ? "almacenado en el sistema" : asociado.CertificadoBpaTamano;
                parrafoProductivo.Add(new(", figurando almacenado en el sistema el archivo de soporte "));
                parrafoProductivo.Add(new($"{asociado.CertificadoBpaNombre} ({tamBpa})", true));
                parrafoProductivo.Add(new("."));
            }
            else
            {
                parrafoProductivo.Add(new(", con archivo soporte en estado "));
                parrafoProductivo.Add(new("No registrado", true));
                parrafoProductivo.Add(new("."));
            }
        }
        else
        {
            parrafoProductivo.Add(new("Respecto a las Buenas Prácticas Agrícolas (BPA), el productor "));
            parrafoProductivo.Add(new("no registra certificación BPA", true));
            parrafoProductivo.Add(new(" en el sistema."));
        }

        PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoProductivo);

        // =====================================================================
        // SECCIÓN IV: COMPONENTE SOCIAL, GRUPOS VULNERABLES Y SOPORTES
        // =====================================================================
        PdfInformeEngine.DibujarTituloSeccion(ctx, "IV", "Componente Social, Grupos Vulnerables y Soportes");

        if (asociado.GruposVulnerables.Count == 0)
        {
            if (string.Equals(asociado.EsVictima, "Si", StringComparison.OrdinalIgnoreCase))
            {
                PdfInformeEngine.DibujarParrafoMixto(ctx, new[]
                {
                    new PdfInformeEngine.FragmentoTexto("En la caracterización social del expediente, el asociado registra condición de vulnerabilidad como "),
                    new PdfInformeEngine.FragmentoTexto("Víctima del conflicto armado", true),
                    new PdfInformeEngine.FragmentoTexto(". No se detallan soportes adicionales por otros grupos poblacionales.")
                });
            }
            else
            {
                PdfInformeEngine.DibujarParrafo(
                    ctx,
                    "En el componente social y poblacional no se registran condiciones de vulnerabilidad ni pertenencia a grupos focalizados para este asociado.");
            }
        }
        else
        {
            PdfInformeEngine.DibujarParrafo(
                ctx,
                "El asociado registra pertenencia a las siguientes condiciones o grupos poblacionales de caracterización social, junto con los archivos de soporte almacenados en el sistema:");

            foreach (var codigoGrupo in asociado.GruposVulnerables)
            {
                var nombreGrupo = AsociadoModel.TraducirGrupoVulnerable(codigoGrupo);
                var detalleSoporte = asociado.SoportesGruposVulnerables.TryGetValue(codigoGrupo, out var archivo) && !string.IsNullOrWhiteSpace(archivo)
                    ? $"Archivo almacenado en el sistema: {archivo}"
                    : "Soporte documental: No registrado";

                PdfInformeEngine.DibujarItemVineta(ctx, nombreGrupo, detalleSoporte);
            }
            ctx.CursorY -= 3.0;
        }

        // =====================================================================
        // SECCIÓN V: CUOTAS SOCIALES, APORTES Y COMPROBANTES
        // =====================================================================
        PdfInformeEngine.DibujarTituloSeccion(ctx, "V", "Cuotas Sociales, Aportes y Comprobantes");

        var estadoGeneralCuotas = ValorONoRegistrado(asociado.EstadoGeneralCuotas);
        PdfInformeEngine.DibujarParrafoMixto(ctx, new[]
        {
            new PdfInformeEngine.FragmentoTexto("En relación con los aportes y compromisos económicos asociativos, el titular presenta un estado general de pago "),
            new PdfInformeEngine.FragmentoTexto(estadoGeneralCuotas, true),
            new PdfInformeEngine.FragmentoTexto(", con un valor total pagado acumulado de "),
            new PdfInformeEngine.FragmentoTexto($"${asociado.ValorTotalPagadoAcumulado:N0} COP", true),
            new PdfInformeEngine.FragmentoTexto(" y un saldo pendiente acumulado de "),
            new PdfInformeEngine.FragmentoTexto($"${asociado.ValorTotalPendienteAcumulado:N0} COP", true),
            new PdfInformeEngine.FragmentoTexto($" ({asociado.TotalCuotasPendientes} cuota(s) pendiente(s)). Se registran en total "),
            new PdfInformeEngine.FragmentoTexto($"{asociado.TotalSoportesCuotas} comprobante(s)", true),
            new PdfInformeEngine.FragmentoTexto(" almacenado(s) como respaldo de dichos aportes.")
        });

        if (asociado.CuotasAportes.Count == 0)
        {
            PdfInformeEngine.DibujarParrafo(
                ctx,
                "No se registran cuotas o aportes individuales detallados para este asociado.");
        }
        else
        {
            var columnasCuotas = new List<PdfInformeEngine.ColumnaTabla>
            {
                new() { Titulo = "N°", Ancho = 24.0, Negrita = true },
                new() { Titulo = "Tipo y Periodicidad", Ancho = 108.0, Negrita = true },
                new() { Titulo = "Valor Cuota", Ancho = 68.0, AlinearDerecha = true },
                new() { Titulo = "Vencimiento", Ancho = 62.0 },
                new() { Titulo = "Pendiente", Ancho = 82.0, AlinearDerecha = true },
                new() { Titulo = "Pagado / Estado", Ancho = 86.0 },
                new() { Titulo = "Comprobantes Almacenados", Ancho = 86.0 }
            };

            var filasCuotas = new List<string[]>();
            for (var idx = 0; idx < asociado.CuotasAportes.Count; idx++)
            {
                var cuota = asociado.CuotasAportes[idx];
                var estadoCuota = cuota.CalcularEstadoAutomatico();
                var vencimientoTexto = cuota.FechaVencimiento?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "No registrado";
                var comprobantesTexto = cuota.Soportes.Count > 0
                    ? string.Join("; ", cuota.Soportes.Select(s =>
                        string.IsNullOrWhiteSpace(s.ArchivoTamano)
                            ? $"{s.ArchivoNombre} (Almacenado)"
                            : $"{s.ArchivoNombre} ({s.ArchivoTamano})"))
                    : "Sin comprobante";

                filasCuotas.Add(new[]
                {
                    (idx + 1).ToString(CultureInfo.InvariantCulture),
                    $"{ValorONoRegistrado(cuota.TipoCuota)} ({ValorONoRegistrado(cuota.Periodicidad)})",
                    $"${cuota.ValorCuota:N0} COP",
                    vencimientoTexto,
                    $"{cuota.CantidadCuotasPendientes} cuota(s) · ${cuota.ValorTotalPendiente:N0}",
                    $"${cuota.ValorTotalPagado:N0} · {estadoCuota}",
                    comprobantesTexto
                });
            }

            PdfInformeEngine.DibujarTablaFormal(ctx, columnasCuotas, filasCuotas);
        }

        // =====================================================================
        // SECCIÓN VI: RELACIÓN DE DOCUMENTOS ADJUNTOS ALMACENADOS
        // =====================================================================
        PdfInformeEngine.DibujarTituloSeccion(ctx, "VI", "Relación de Documentos y Archivos Adjuntos del Registro");

        if (documentosAdjuntos.Count == 0)
        {
            PdfInformeEngine.DibujarParrafo(
                ctx,
                "No se registran archivos digitales adjuntos almacenados en este expediente.");
        }
        else
        {
            var cantIncrustados = documentosAdjuntos.Count(d => d.TieneArchivoBinario);
            var textoIntroAdjuntos =
                "A continuación se relacionan los archivos digitales asociados al registro del productor. " +
                "El estado indicado da cuenta únicamente de su almacenamiento o referencia dentro del sistema ASOAGRO y no implica validación legal sobre el contenido del documento.";

            if (cantIncrustados > 0)
            {
                textoIntroAdjuntos += $" Adicionalmente, {cantIncrustados} archivo(s) original(es) se encuentran incrustados en el contenedor de este PDF.";
            }

            PdfInformeEngine.DibujarParrafo(ctx, textoIntroAdjuntos);

            var columnasAdjuntos = new List<PdfInformeEngine.ColumnaTabla>
            {
                new() { Titulo = "N°", Ancho = 24.0, Negrita = true },
                new() { Titulo = "Sección y Concepto", Ancho = 136.0, Negrita = true },
                new() { Titulo = "Nombre del Archivo", Ancho = 156.0 },
                new() { Titulo = "Tamaño", Ancho = 58.0 },
                new() { Titulo = "Estado en el Sistema", Ancho = 142.0 }
            };

            var filasAdjuntos = new List<string[]>();
            for (var i = 0; i < documentosAdjuntos.Count; i++)
            {
                var doc = documentosAdjuntos[i];
                var estadoSistema = doc.TieneArchivoBinario
                    ? "Almacenado en el sistema (incrustado en PDF)"
                    : "Almacenado en el sistema (referencia registrada)";

                filasAdjuntos.Add(new[]
                {
                    (i + 1).ToString(CultureInfo.InvariantCulture),
                    $"{doc.Seccion} · {doc.Respaldo}",
                    doc.NombreArchivo,
                    doc.Tamano,
                    estadoSistema
                });
            }

            PdfInformeEngine.DibujarTablaFormal(ctx, columnasAdjuntos, filasAdjuntos);
        }

        PdfInformeEngine.DibujarPiePaginas(ctx);

        var adjuntosBinarios = documentosAdjuntos
            .Where(a => a.TieneArchivoBinario)
            .Select(a => (a.NombreArchivo, $"{a.Seccion} - {a.Respaldo}", a.ContenidoBytes!))
            .ToList();

        return PdfInformeEngine.CompilarPdf(ctx.Paginas, adjuntosBinarios);
    }

    private static string ValorONoRegistrado(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? "No registrado" : valor.Trim();

    private static string FormatearMunicipio(string? municipio) => municipio switch
    {
        "Apartado" => "Apartadó",
        "Chigorodo" => "Chigorodó",
        "Mutata" => "Mutatá",
        "Necocli" => "Necoclí",
        "San Pedro de Uraba" => "San Pedro de Urabá",
        _ => string.IsNullOrWhiteSpace(municipio) ? "No registrado" : municipio.Trim()
    };

    private static List<DocumentoAdjuntoPdf> RecopilarDocumentosAdjuntos(AsociadoModel asociado)
    {
        var lista = new List<DocumentoAdjuntoPdf>();

        if (!string.IsNullOrWhiteSpace(asociado.CedulaArchivoNombre))
        {
            var (mime, bytes) = ExtraerBytesDesdeDataUrl(asociado.CedulaArchivoUrl);
            lista.Add(new DocumentoAdjuntoPdf
            {
                Seccion = "Identificación",
                Respaldo = "Fotocopia de Cédula / NIT",
                NombreArchivo = asociado.CedulaArchivoNombre,
                Tamano = string.IsNullOrWhiteSpace(asociado.CedulaArchivoTamano) ? "Registrado" : asociado.CedulaArchivoTamano,
                Detalle = $"Soporte de identificación del titular CC/NIT {asociado.Cedula}",
                MimeType = mime,
                ContenidoBytes = bytes
            });
        }

        if (!string.IsNullOrWhiteSpace(asociado.RutArchivoNombre))
        {
            var (mime, bytes) = ExtraerBytesDesdeDataUrl(asociado.RutArchivoUrl);
            lista.Add(new DocumentoAdjuntoPdf
            {
                Seccion = "Identificación",
                Respaldo = "Registro Único Tributario (RUT)",
                NombreArchivo = asociado.RutArchivoNombre,
                Tamano = string.IsNullOrWhiteSpace(asociado.RutArchivoTamano) ? "Registrado" : asociado.RutArchivoTamano,
                Detalle = $"Soporte tributario del titular CC/NIT {asociado.Cedula}",
                MimeType = mime,
                ContenidoBytes = bytes
            });
        }

        if (asociado.TieneBPA && !string.IsNullOrWhiteSpace(asociado.CertificadoBpaNombre))
        {
            var (mime, bytes) = ExtraerBytesDesdeDataUrl(asociado.CertificadoBpaUrl);
            lista.Add(new DocumentoAdjuntoPdf
            {
                Seccion = "Línea Productiva",
                Respaldo = "Certificación BPA",
                NombreArchivo = asociado.CertificadoBpaNombre,
                Tamano = string.IsNullOrWhiteSpace(asociado.CertificadoBpaTamano) ? "Registrado" : asociado.CertificadoBpaTamano,
                Detalle = $"Soporte de Buenas Prácticas Agrícolas · Predio {asociado.NombrePredio}",
                MimeType = mime,
                ContenidoBytes = bytes
            });
        }

        foreach (var cod in asociado.GruposVulnerables)
        {
            if (asociado.SoportesGruposVulnerables.TryGetValue(cod, out var nombreSop) && !string.IsNullOrWhiteSpace(nombreSop))
            {
                asociado.SoportesGruposUrls.TryGetValue(cod, out var urlSop);
                var (mime, bytes) = ExtraerBytesDesdeDataUrl(urlSop);
                lista.Add(new DocumentoAdjuntoPdf
                {
                    Seccion = "Componente Social",
                    Respaldo = AsociadoModel.TraducirGrupoVulnerable(cod),
                    NombreArchivo = nombreSop,
                    Tamano = bytes is { Length: > 0 } ? $"{Math.Max(1, bytes.Length / 1024)} KB" : "Registrado",
                    Detalle = $"Soporte de condición social: {AsociadoModel.TraducirGrupoVulnerable(cod)}",
                    MimeType = mime,
                    ContenidoBytes = bytes
                });
            }
        }

        for (var i = 0; i < asociado.CuotasAportes.Count; i++)
        {
            var cuota = asociado.CuotasAportes[i];
            foreach (var sop in cuota.Soportes)
            {
                if (!string.IsNullOrWhiteSpace(sop.ArchivoNombre))
                {
                    var (mime, bytes) = ExtraerBytesDesdeDataUrl(sop.ArchivoUrl);
                    lista.Add(new DocumentoAdjuntoPdf
                    {
                        Seccion = $"Cuotas y Aportes #{i + 1}",
                        Respaldo = $"{cuota.TipoCuota} ({cuota.Periodicidad})",
                        NombreArchivo = sop.ArchivoNombre,
                        Tamano = string.IsNullOrWhiteSpace(sop.ArchivoTamano) ? "Registrado" : sop.ArchivoTamano,
                        Detalle = $"Comprobante de cuota de {cuota.TipoCuota} (${cuota.ValorCuota:N0} COP)",
                        MimeType = mime,
                        ContenidoBytes = bytes
                    });
                }
            }
        }

        return lista;
    }

    private static (string MimeType, byte[]? Bytes) ExtraerBytesDesdeDataUrl(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl) || !dataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return ("application/octet-stream", null);
        }

        var comaIndex = dataUrl.IndexOf(',');
        if (comaIndex <= 5 || comaIndex >= dataUrl.Length - 1)
        {
            return ("application/octet-stream", null);
        }

        var cabecera = dataUrl[5..comaIndex];
        var base64 = dataUrl[(comaIndex + 1)..];
        var mime = cabecera.Split(';', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "application/octet-stream";

        try
        {
            return (mime, Convert.FromBase64String(base64));
        }
        catch
        {
            return (mime, null);
        }
    }
}

using System.Globalization;
using System.Text;
using Asoagro.Models;

namespace Asoagro.Services;

/// <summary>
/// Generador formal en C# puro de la Ficha / Resumen del Expediente Institucional de la Asociación (PDF 1.4).
/// Diseñado para archivo e impresión institucional en FAAUA / ASOAGRO.
/// </summary>
public static class ExpedienteAsociacionPdfGenerator
{
    private const double PageWidth = 612.0;  // 8.5 in (Letter)
    private const double PageHeight = 792.0; // 11.0 in (Letter)
    private const double MarginX = 38.0;
    private const double ContentWidth = PageWidth - (MarginX * 2.0);
    private const double BottomLimit = 55.0;

    private sealed class PdfPageBuilder
    {
        public StringBuilder Stream { get; } = new();
    }

    public static byte[] GenerarResumenPdf(
        AsociacionInstitucional asoc, 
        IEnumerable<DocumentoAsociacion> documentos,
        string funcionarioResponsable = "Secretaría FAAUA",
        DateTime? fechaGeneracion = null)
    {
        var items = (documentos ?? Enumerable.Empty<DocumentoAsociacion>()).Select(d => new DocumentoItemInstitucional
        {
            TipoDocumento = d.TipoDocumento,
            Titulo = d.Titulo,
            EstadoRequisito = d.Aplica ? "Aplica" : "No aplica",
            MotivoNoAplica = d.MotivoNoAplica,
            FechaEmision = d.FechaEmision,
            FechaVencimiento = d.FechaVencimiento,
            NumeroReferencia = d.NumeroReferencia,
            EntidadEmisora = d.EntidadEmisora,
            Periodo = d.Periodo,
            Observaciones = d.Observaciones,
            ArchivoNombre = d.ArchivoNombre,
            ArchivoContenido = d.ArchivoContenido,
            ArchivoTipoMime = d.ArchivoTipoMime
        }).ToList();

        return GenerarResumenPdf(asoc, items, funcionarioResponsable, fechaGeneracion);
    }

    public static byte[] GenerarResumenPdf(
        AsociacionInstitucional asoc, 
        IEnumerable<DocumentoItemInstitucional> documentos,
        string funcionarioResponsable = "Secretaría FAAUA",
        DateTime? fechaGeneracion = null)
    {
        var generadoEn = fechaGeneracion ?? DateTime.Now;
        var paginas = new List<PdfPageBuilder>();
        PdfPageBuilder paginaActual = null!;
        double cursorY = 0;

        void NuevaPagina(bool esPrimera)
        {
            paginaActual = new PdfPageBuilder();
            paginas.Add(paginaActual);

            // Borde y fondo formal
            paginaActual.Stream.AppendLine("q");
            paginaActual.Stream.AppendLine("0.96 0.97 0.95 rg"); // Fondo suave
            paginaActual.Stream.AppendLine($"0 0 {PageWidth:F2} {PageHeight:F2} re f");
            paginaActual.Stream.AppendLine("1.0 1.0 1.0 rg"); // Hoja blanca central
            paginaActual.Stream.AppendLine($"15 15 {PageWidth - 30:F2} {PageHeight - 30:F2} re f");
            paginaActual.Stream.AppendLine("0.85 0.88 0.84 RG 1 w");
            paginaActual.Stream.AppendLine($"15 15 {PageWidth - 30:F2} {PageHeight - 30:F2} re S");
            paginaActual.Stream.AppendLine("Q");

            cursorY = PageHeight - 38.0;

            // Encabezado
            paginaActual.Stream.AppendLine("q");
            paginaActual.Stream.AppendLine("0.08 0.33 0.18 rg"); // Verde bosque #14532d
            paginaActual.Stream.AppendLine($"{MarginX:F2} {cursorY - 48:F2} {ContentWidth:F2} 48 re f");
            paginaActual.Stream.AppendLine("0.85 0.47 0.03 rg"); // Franja trigo #d97706
            paginaActual.Stream.AppendLine($"{MarginX:F2} {cursorY - 51:F2} {ContentWidth:F2} 3 re f");
            paginaActual.Stream.AppendLine("Q");

            EscribirTexto(paginaActual.Stream, MarginX + 12, cursorY - 18, "ASOAGRO · FAAUA", 11, true, 1, 1, 1);
            EscribirTexto(paginaActual.Stream, MarginX + 12, cursorY - 32, "FEDERACIÓN DE ASOCIACIONES AGROPECUARIAS DEL URABÁ", 7.5, false, 0.9, 0.95, 0.9);
            EscribirTexto(paginaActual.Stream, MarginX + 12, cursorY - 43, "SISTEMA DE GESTIÓN DOCUMENTAL INSTITUCIONAL · SECRETARÍA GENERAL", 6.8, false, 0.85, 0.9, 0.85);

            var textoFecha = $"Generado: {generadoEn:dd/MM/yyyy HH:mm}";
            EscribirTexto(paginaActual.Stream, PageWidth - MarginX - 12 - (textoFecha.Length * 4.6), cursorY - 24, textoFecha, 7.5, false, 0.9, 0.95, 0.9);

            cursorY -= 62.0;

            // Título del documento
            var nombreTitulo = string.IsNullOrWhiteSpace(asoc.Sigla)
                ? asoc.NombreLegal
                : $"{asoc.Sigla} · {asoc.NombreLegal}";
            
            EscribirTexto(paginaActual.Stream, MarginX, cursorY, "EXPEDIENTE INSTITUCIONAL DE LA ASOCIACIÓN", 13, true, 0.08, 0.33, 0.18);
            cursorY -= 14.0;
            EscribirTexto(paginaActual.Stream, MarginX, cursorY, nombreTitulo.ToUpperInvariant(), 9.5, true, 0.2, 0.25, 0.22);
            cursorY -= 12.0;

            var subtitulo = $"NIT: {(string.IsNullOrWhiteSpace(asoc.Nit) ? "En trámite" : $"{asoc.Nit}-{asoc.DigitoVerificacion}")} · Tipología: {asoc.TipoOrganizacion} · Ámbito: {asoc.Ambito} · Sede: {asoc.Municipio} ({asoc.Departamento})";
            EscribirTexto(paginaActual.Stream, MarginX, cursorY, subtitulo, 7.8, false, 0.35, 0.4, 0.36);
            cursorY -= 18.0;

            // Línea separadora
            paginaActual.Stream.AppendLine("q 0.82 0.85 0.8 RG 0.7 w");
            paginaActual.Stream.AppendLine($"{MarginX:F2} {cursorY:F2} m {PageWidth - MarginX:F2} {cursorY:F2} l S Q");
            cursorY -= 12.0;
        }

        void AsegurarEspacio(double espacioRequerido)
        {
            if (cursorY - espacioRequerido < BottomLimit)
            {
                NuevaPagina(false);
            }
        }

        void DibujarSeccionEncabezado(string titulo, string kicker)
        {
            AsegurarEspacio(32.0);
            paginaActual.Stream.AppendLine("q");
            paginaActual.Stream.AppendLine("0.93 0.96 0.93 rg");
            paginaActual.Stream.AppendLine($"{MarginX:F2} {cursorY - 18:F2} {ContentWidth:F2} 18 re f");
            paginaActual.Stream.AppendLine("0.08 0.33 0.18 RG 1.5 w");
            paginaActual.Stream.AppendLine($"{MarginX:F2} {cursorY - 18:F2} m {MarginX:F2} {cursorY:F2} l S");
            paginaActual.Stream.AppendLine("Q");

            EscribirTexto(paginaActual.Stream, MarginX + 8, cursorY - 8, kicker.ToUpperInvariant(), 6.5, true, 0.7, 0.4, 0.1);
            EscribirTexto(paginaActual.Stream, MarginX + 8, cursorY - 15, titulo, 9, true, 0.08, 0.33, 0.18);
            cursorY -= 24.0;
        }

        void DibujarFilaDato(string etiqueta1, string valor1, string etiqueta2 = "", string valor2 = "")
        {
            AsegurarEspacio(18.0);
            var colWidth = ContentWidth / 2.0;

            // Columna 1
            EscribirTexto(paginaActual.Stream, MarginX + 4, cursorY, etiqueta1.ToUpperInvariant(), 6.5, true, 0.45, 0.5, 0.46);
            EscribirTexto(paginaActual.Stream, MarginX + 4, cursorY - 8.5, Truncar(valor1, 52), 7.8, false, 0.1, 0.15, 0.12);

            // Columna 2
            if (!string.IsNullOrWhiteSpace(etiqueta2))
            {
                EscribirTexto(paginaActual.Stream, MarginX + colWidth + 4, cursorY, etiqueta2.ToUpperInvariant(), 6.5, true, 0.45, 0.5, 0.46);
                EscribirTexto(paginaActual.Stream, MarginX + colWidth + 4, cursorY - 8.5, Truncar(valor2, 52), 7.8, false, 0.1, 0.15, 0.12);
            }

            cursorY -= 17.0;
        }

        // Iniciar página 1
        NuevaPagina(true);

        // ==========================================
        // 1. IDENTIFICACIÓN DE LA ASOCIACIÓN
        // ==========================================
        DibujarSeccionEncabezado("IDENTIFICACIÓN DE LA PERSONA JURÍDICA", "Sección 1");
        DibujarFilaDato("Nombre Legal Completo", asoc.NombreLegal, "Sigla / Acrónimo", string.IsNullOrWhiteSpace(asoc.Sigla) ? "Sin sigla registrada" : asoc.Sigla);
        DibujarFilaDato("NIT Institucional", string.IsNullOrWhiteSpace(asoc.Nit) ? "En trámite ante DIAN" : $"{asoc.Nit}-{asoc.DigitoVerificacion}", "Tipo de Organización", asoc.TipoOrganizacion);
        DibujarFilaDato("Ámbito Territorial", asoc.Ambito, "Grado de Organización", asoc.Ambito == "Nacional" ? "Segundo / Tercer grado" : "Primer grado (Base productora)");
        DibujarFilaDato("Fecha de Constitución", asoc.FechaConstitucion?.ToString("dd/MM/yyyy") ?? "No indicada", "Estado Institucional", asoc.Estado);
        DibujarFilaDato("Entidad de Inscripción", asoc.EntidadInscripcion, "Matrícula / Radicado", string.IsNullOrWhiteSpace(asoc.NumeroRegistro) ? "Pendiente" : asoc.NumeroRegistro);
        DibujarFilaDato("Fecha de Inscripción", asoc.FechaInscripcion?.ToString("dd/MM/yyyy") ?? "No indicada", "Renovación Matrícula", asoc.FechaRenovacionMatricula?.ToString("dd/MM/yyyy") ?? "No indicada");

        if (!string.IsNullOrWhiteSpace(asoc.ExcepcionesIdentificacion))
        {
            DibujarFilaDato("Excepciones Institucionales", asoc.ExcepcionesIdentificacion);
        }

        cursorY -= 4.0;

        // ==========================================
        // 2. DOMICILIO Y CONTACTO INSTITUCIONAL
        // ==========================================
        DibujarSeccionEncabezado("DOMICILIO PRINCIPAL Y CONTACTO FORMAL", "Sección 2");
        DibujarFilaDato("Departamento y Municipio", $"{asoc.Municipio}, {asoc.Departamento} (Urabá Antioqueño)", "Dirección Sede Principal", asoc.DireccionPrincipal);
        DibujarFilaDato("Correo Electrónico Oficial", asoc.CorreoInstitucional, "Teléfono Institucional", asoc.TelefonoContacto);
        DibujarFilaDato("Dirección Notificaciones", asoc.NotificacionesDiferentes && !string.IsNullOrWhiteSpace(asoc.DireccionNotificaciones) ? asoc.DireccionNotificaciones : "Misma dirección de la sede principal", "Sitio Web", string.IsNullOrWhiteSpace(asoc.SitioWeb) ? "No registrado" : asoc.SitioWeb);
        DibujarFilaDato("Persona de Contacto Institucional", string.IsNullOrWhiteSpace(asoc.ContactoNombre) ? "No especificado" : asoc.ContactoNombre, "Cargo de Contacto", string.IsNullOrWhiteSpace(asoc.ContactoCargo) ? "No especificado" : asoc.ContactoCargo);

        cursorY -= 4.0;

        // ==========================================
        // 3. CONSTITUCIÓN Y ESTATUTOS (LEY 2219 DE 2022)
        // ==========================================
        DibujarSeccionEncabezado("RÉGIMEN CONSTITUTIVO Y ESTATUTARIO (LEY 2219 DE 2022)", "Sección 3");
        DibujarFilaDato("Fecha Asamblea Fundacional", asoc.FechaAsambleaConstitucion?.ToString("dd/MM/yyyy") ?? "No indicada", "Aprobación de Estatutos", asoc.AprobacionEstatutos ? "Sí, aprobados en asamblea" : "Pendiente");
        DibujarFilaDato("Cuotas de Sostenimiento Acordadas", asoc.CuotasInicialesAcordadas ? (string.IsNullOrWhiteSpace(asoc.DetalleCuotasIniciales) ? "Sí acordadas" : asoc.DetalleCuotasIniciales) : "No acordadas inicialmente", "Mecanismos de Autogobierno", string.IsNullOrWhiteSpace(asoc.ReglasAutogobierno) ? "Directrices estatutarias de asamblea" : asoc.ReglasAutogobierno);

        cursorY -= 4.0;

        // ==========================================
        // 4. REPRESENTACIÓN LEGAL Y ÓRGANOS DE GOBIERNO
        // ==========================================
        DibujarSeccionEncabezado("REPRESENTACIÓN LEGAL Y ÓRGANOS DE GOBIERNO", "Sección 4");
        DibujarFilaDato("Representante Legal", asoc.RepLegalNombre, "Doc. Contacto", string.IsNullOrWhiteSpace(asoc.RepLegalDocumento) ? "No indicado" : asoc.RepLegalDocumento);
        DibujarFilaDato("Periodo de Nombramiento", $"Inicio: {asoc.RepLegalPeriodoInicio:dd/MM/yyyy} · Vence: {asoc.RepLegalPeriodoFin:dd/MM/yyyy}", "Fecha Inscripción Registro", asoc.RepLegalFechaInscripcion?.ToString("dd/MM/yyyy") ?? "No indicada");
        DibujarFilaDato("Presidente de Junta Directiva", string.IsNullOrWhiteSpace(asoc.JuntaPresidente) ? "No indicado" : asoc.JuntaPresidente, "Periodo Junta Directiva", string.IsNullOrWhiteSpace(asoc.JuntaPeriodo) ? "No especificado" : asoc.JuntaPeriodo);
        DibujarFilaDato("Fiscalización / Control", asoc.FiscalAplica ? $"{asoc.FiscalTipo}: {asoc.FiscalNombre}" : $"No aplica ({asoc.FiscalMotivoNoAplica})", "Periodo Fiscal", string.IsNullOrWhiteSpace(asoc.FiscalPeriodo) ? "-" : asoc.FiscalPeriodo);

        cursorY -= 4.0;

        // ==========================================
        // 5. IDENTIFICACIÓN TRIBUTARIA
        // ==========================================
        DibujarSeccionEncabezado("IDENTIFICACIÓN TRIBUTARIA Y OBLIGACIONES CONFIRMADAS", "Sección 5");
        DibujarFilaDato("Estado en el RUT", asoc.RutEstado, "Fecha Actualización RUT", asoc.RutFechaActualizacion?.ToString("dd/MM/yyyy") ?? "No indicada");
        DibujarFilaDato("Régimen Tributario Declarado", asoc.RegimenTributario, "Trámite RTE DIAN", asoc.RegimenTributario.Contains("Especial") ? $"{asoc.RteEstadoDian} (Vence: {asoc.RteFechaLimiteRenovacion:dd/MM/yyyy})" : "No aplica Régimen Especial");
        DibujarFilaDato("Responsabilidades Tributarias en RUT", asoc.ResponsabilidadesTributarias.Any() ? string.Join(", ", asoc.ResponsabilidadesTributarias) : "Sin responsabilidades adicionales registradas");

        cursorY -= 6.0;

        // ==========================================
        // 6. INVENTARIO DE DOCUMENTOS Y SOPORTES DIGITALIZADOS
        // ==========================================
        DibujarSeccionEncabezado("INVENTARIO DE DOCUMENTOS Y EXPEDIENTE DIGITAL", "Sección 6 y 7");

        AsegurarEspacio(20.0);
        paginaActual.Stream.AppendLine("q 0.9 0.93 0.9 rg");
        paginaActual.Stream.AppendLine($"{MarginX:F2} {cursorY - 14:F2} {ContentWidth:F2} 14 re f Q");
        EscribirTexto(paginaActual.Stream, MarginX + 4, cursorY - 10, "DOCUMENTO INSTITUCIONAL", 6.8, true, 0.1, 0.2, 0.1);
        EscribirTexto(paginaActual.Stream, MarginX + 180, cursorY - 10, "ESTADO / ARCHIVO", 6.8, true, 0.1, 0.2, 0.1);
        EscribirTexto(paginaActual.Stream, MarginX + 330, cursorY - 10, "VENCIMIENTO / VIGENCIA", 6.8, true, 0.1, 0.2, 0.1);
        EscribirTexto(paginaActual.Stream, MarginX + 440, cursorY - 10, "OBSERVACIONES", 6.8, true, 0.1, 0.2, 0.1);
        cursorY -= 17.0;

        var listaDocs = documentos.ToList();
        foreach (var doc in listaDocs)
        {
            AsegurarEspacio(15.0);
            var estadoTexto = !doc.Aplica 
                ? "No aplica" 
                : (!string.IsNullOrWhiteSpace(doc.ArchivoNombre) ? $"Digitalizado ({doc.ArchivoNombre})" : "Pendiente de carga");

            var vigencia = doc.FechaVencimiento.HasValue ? doc.FechaVencimiento.Value.ToString("dd/MM/yyyy") : "Sin vencimiento / Vigente";
            var obs = !doc.Aplica ? doc.MotivoNoAplica : (doc.NumeroReferencia ?? "");

            EscribirTexto(paginaActual.Stream, MarginX + 4, cursorY, Truncar(doc.TipoDocumento, 36), 7.2, true, 0.15, 0.2, 0.15);
            EscribirTexto(paginaActual.Stream, MarginX + 180, cursorY, Truncar(estadoTexto, 32), 6.8, false, doc.Aplica && !string.IsNullOrWhiteSpace(doc.ArchivoNombre) ? 0.08 : 0.5, doc.Aplica && !string.IsNullOrWhiteSpace(doc.ArchivoNombre) ? 0.45 : 0.2, 0.1);
            EscribirTexto(paginaActual.Stream, MarginX + 330, cursorY, vigencia, 6.8, false, 0.2, 0.2, 0.2);
            EscribirTexto(paginaActual.Stream, MarginX + 440, cursorY, Truncar(obs, 20), 6.5, false, 0.4, 0.4, 0.4);

            paginaActual.Stream.AppendLine($"q 0.92 0.92 0.92 RG 0.5 w {MarginX:F2} {cursorY - 3:F2} m {PageWidth - MarginX:F2} {cursorY - 3:F2} l S Q");
            cursorY -= 14.0;
        }

        // ==========================================
        // PIE DE PÁGINA Y DESCARGO FORMAL
        // ==========================================
        var totalPaginas = paginas.Count;
        for (int i = 0; i < totalPaginas; i++)
        {
            var p = paginas[i];
            var numeroPagina = i + 1;

            p.Stream.AppendLine("q");
            p.Stream.AppendLine("0.8 0.83 0.78 RG 0.7 w");
            p.Stream.AppendLine($"{MarginX:F2} 42 m {PageWidth - MarginX:F2} 42 l S");
            p.Stream.AppendLine("Q");

            var descargo = "Reporte formal generado por el Sistema ASOAGRO (FAAUA). No reemplaza certificados oficiales expedidos por entidades del Estado ni constituye asesoría legal.";
            EscribirTexto(p.Stream, MarginX, 32, descargo, 5.8, false, 0.45, 0.48, 0.45);

            var auditoria = $"Responsable: {funcionarioResponsable} · Archivo institucional Urabá Antioqueño · Página {numeroPagina} de {totalPaginas}";
            EscribirTexto(p.Stream, MarginX, 23, auditoria, 6.2, true, 0.2, 0.3, 0.22);
        }

        return EnsamblarDocumentoPdf(paginas);
    }

    private static void EscribirTexto(
        StringBuilder sb,
        double x,
        double y,
        string texto,
        double tamano,
        bool negrita = false,
        double r = 0,
        double g = 0,
        double b = 0)
    {
        if (string.IsNullOrEmpty(texto)) return;

        var fuente = negrita ? "/F2" : "/F1";
        var seguro = EscaparTextoPdf(texto);

        sb.AppendLine("BT");
        sb.AppendLine(CultureInfo.InvariantCulture, $"{r:F2} {g:F2} {b:F2} rg");
        sb.AppendLine($"{fuente} {tamano:F1} Tf");
        sb.AppendLine(CultureInfo.InvariantCulture, $"{x:F2} {y:F2} Td");
        sb.AppendLine($"({seguro}) Tj");
        sb.AppendLine("ET");
    }

    private static string EscaparTextoPdf(string texto)
    {
        var normalizado = texto
            .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
            .Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U")
            .Replace("ñ", "n").Replace("Ñ", "N")
            .Replace("ü", "u").Replace("Ü", "U")
            .Replace("·", "-");

        var sb = new StringBuilder(normalizado.Length + 8);
        foreach (var ch in normalizado)
        {
            if (ch is '(' or ')' or '\\')
            {
                sb.Append('\\').Append(ch);
            }
            else if (ch >= 32 && ch <= 126)
            {
                sb.Append(ch);
            }
            else
            {
                sb.Append(' ');
            }
        }
        return sb.ToString();
    }

    private static string Truncar(string texto, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(texto)) return "-";
        texto = texto.Trim();
        return texto.Length <= maxLen ? texto : string.Concat(texto.AsSpan(0, maxLen - 2), "..");
    }

    private static byte[] EnsamblarDocumentoPdf(IReadOnlyList<PdfPageBuilder> paginas)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, new UTF8Encoding(false));

        writer.NewLine = "\n";
        writer.WriteLine("%PDF-1.4");
        writer.Flush();

        var offsets = new List<long> { 0 };

        void RegistrarObjeto()
        {
            writer.Flush();
            offsets.Add(ms.Position);
        }

        // 1: Catalog
        RegistrarObjeto();
        writer.WriteLine("1 0 obj");
        writer.WriteLine("<< /Type /Catalog /Pages 2 0 R >>");
        writer.WriteLine("endobj");

        // 2: Pages
        RegistrarObjeto();
        writer.WriteLine("2 0 obj");
        writer.Write("<< /Type /Pages /Count " + paginas.Count + " /Kids [");
        for (int i = 0; i < paginas.Count; i++)
        {
            var pageObjNum = 5 + (i * 2);
            writer.Write($" {pageObjNum} 0 R");
        }
        writer.WriteLine(" ] >>");
        writer.WriteLine("endobj");

        // 3: Font Regular (Helvetica)
        RegistrarObjeto();
        writer.WriteLine("3 0 obj");
        writer.WriteLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        writer.WriteLine("endobj");

        // 4: Font Bold (Helvetica-Bold)
        RegistrarObjeto();
        writer.WriteLine("4 0 obj");
        writer.WriteLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
        writer.WriteLine("endobj");

        // Páginas y sus Streams
        for (int i = 0; i < paginas.Count; i++)
        {
            var pageObjNum = 5 + (i * 2);
            var contentObjNum = pageObjNum + 1;
            var streamBytes = Encoding.ASCII.GetBytes(paginas[i].Stream.ToString());

            // Objeto Page
            RegistrarObjeto();
            writer.WriteLine($"{pageObjNum} 0 obj");
            writer.WriteLine("<< /Type /Page /Parent 2 0 R");
            writer.WriteLine(FormattableString.Invariant($"   /MediaBox [0 0 {PageWidth:F2} {PageHeight:F2}]"));
            writer.WriteLine("   /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >>");
            writer.WriteLine($"   /Contents {contentObjNum} 0 R");
            writer.WriteLine(">>");
            writer.WriteLine("endobj");

            // Objeto Content Stream
            RegistrarObjeto();
            writer.WriteLine($"{contentObjNum} 0 obj");
            writer.WriteLine($"<< /Length {streamBytes.Length} >>");
            writer.WriteLine("stream");
            writer.Flush();

            ms.Write(streamBytes, 0, streamBytes.Length);

            writer.WriteLine();
            writer.WriteLine("endstream");
            writer.WriteLine("endobj");
        }

        // Xref Table
        writer.Flush();
        var startXref = ms.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {offsets.Count}");
        writer.WriteLine("0000000000 65535 f ");
        for (int i = 1; i < offsets.Count; i++)
        {
            writer.WriteLine($"{offsets[i]:D10} 00000 n ");
        }

        // Trailer
        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {offsets.Count} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(startXref);
        writer.WriteLine("%%EOF");
        writer.Flush();

        return ms.ToArray();
    }
}


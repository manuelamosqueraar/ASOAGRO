using System.Globalization;
using System.Text;
using Asoagro.Models;

namespace Asoagro.Services;

public static class AsociadoPdfGenerator
{
    private const double PageWidth = 612.0;  // 8.5 in (Letter)
    private const double PageHeight = 792.0; // 11.0 in (Letter)
    private const double MarginX = 40.0;
    private const double ContentWidth = PageWidth - (MarginX * 2.0);
    private const double BottomLimit = 64.0;

    private sealed class PdfPageBuilder
    {
        public StringBuilder Stream { get; } = new();
    }

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
        var paginas = new List<PdfPageBuilder>();
        var documentosAdjuntos = RecopilarDocumentosAdjuntos(asociado);
        PdfPageBuilder paginaActual = null!;
        double cursorY = 0;

        void NuevaPagina(bool esPrimera)
        {
            paginaActual = new PdfPageBuilder();
            paginas.Add(paginaActual);

            if (esPrimera)
            {
                DibujarEncabezadoPrincipal(paginaActual.Stream, asociado, generadoEn);
                cursorY = PageHeight - 122.0;
            }
            else
            {
                DibujarEncabezadoContinuacion(paginaActual.Stream, asociado, generadoEn);
                cursorY = PageHeight - 76.0;
            }
        }

        void AsegurarEspacio(double altoRequerido)
        {
            if (cursorY - altoRequerido < BottomLimit)
            {
                NuevaPagina(esPrimera: false);
            }
        }

        void DibujarTituloSeccion(string numero, string titulo)
        {
            AsegurarEspacio(42.0);
            cursorY -= 8.0;

            var yCaja = cursorY - 22.0;
            RectanguloRelleno(paginaActual.Stream, MarginX, yCaja, ContentWidth, 22.0, 0.93, 0.96, 0.93);
            RectanguloRelleno(paginaActual.Stream, MarginX, yCaja, 4.5, 22.0, 0.09, 0.22, 0.16);

            EscribirTexto(paginaActual.Stream, Bold: true, 10.0, MarginX + 12.0, yCaja + 6.5, $"{numero}. {titulo.ToUpperInvariant()}", 0.09, 0.22, 0.16);
            cursorY = yCaja - 8.0;
        }

        void DibujarFilaParDatos(string etiqueta1, string valor1, string etiqueta2, string valor2)
        {
            var colWidth = (ContentWidth - 10.0) / 2.0;
            var lineas1 = EnvolverTexto(string.IsNullOrWhiteSpace(valor1) ? "No registrado" : valor1, 44);
            var lineas2 = EnvolverTexto(string.IsNullOrWhiteSpace(valor2) ? "No registrado" : valor2, 44);
            var maxLineas = Math.Max(lineas1.Count, lineas2.Count);
            var altoCaja = 20.0 + (maxLineas * 11.5);

            AsegurarEspacio(altoCaja + 5.0);
            var yCaja = cursorY - altoCaja;

            RectanguloConBorde(paginaActual.Stream, MarginX, yCaja, colWidth, altoCaja, 0.99, 0.99, 0.98, 0.85, 0.87, 0.84);
            EscribirTexto(paginaActual.Stream, Bold: true, 7.5, MarginX + 8.0, cursorY - 11.0, etiqueta1.ToUpperInvariant(), 0.36, 0.43, 0.39);
            for (var i = 0; i < lineas1.Count; i++)
            {
                EscribirTexto(paginaActual.Stream, Bold: false, 9.2, MarginX + 8.0, cursorY - 23.0 - (i * 11.5), lineas1[i], 0.09, 0.16, 0.12);
            }

            var xDer = MarginX + colWidth + 10.0;
            RectanguloConBorde(paginaActual.Stream, xDer, yCaja, colWidth, altoCaja, 0.99, 0.99, 0.98, 0.85, 0.87, 0.84);
            EscribirTexto(paginaActual.Stream, Bold: true, 7.5, xDer + 8.0, cursorY - 11.0, etiqueta2.ToUpperInvariant(), 0.36, 0.43, 0.39);
            for (var i = 0; i < lineas2.Count; i++)
            {
                EscribirTexto(paginaActual.Stream, Bold: false, 9.2, xDer + 8.0, cursorY - 23.0 - (i * 11.5), lineas2[i], 0.09, 0.16, 0.12);
            }

            cursorY = yCaja - 5.0;
        }

        void DibujarFilaAnchoCompleto(string etiqueta, string valor, bool resaltar = false)
        {
            var lineas = EnvolverTexto(string.IsNullOrWhiteSpace(valor) ? "No registrado" : valor, 94);
            var altoCaja = 20.0 + (lineas.Count * 11.5);

            AsegurarEspacio(altoCaja + 5.0);
            var yCaja = cursorY - altoCaja;

            if (resaltar)
            {
                RectanguloConBorde(paginaActual.Stream, MarginX, yCaja, ContentWidth, altoCaja, 0.96, 0.98, 0.93, 0.73, 0.85, 0.35);
            }
            else
            {
                RectanguloConBorde(paginaActual.Stream, MarginX, yCaja, ContentWidth, altoCaja, 0.99, 0.99, 0.98, 0.85, 0.87, 0.84);
            }

            EscribirTexto(paginaActual.Stream, Bold: true, 7.5, MarginX + 8.0, cursorY - 11.0, etiqueta.ToUpperInvariant(), 0.36, 0.43, 0.39);
            for (var i = 0; i < lineas.Count; i++)
            {
                EscribirTexto(paginaActual.Stream, Bold: resaltar, 9.2, MarginX + 8.0, cursorY - 23.0 - (i * 11.5), lineas[i], 0.09, 0.16, 0.12);
            }

            cursorY = yCaja - 5.0;
        }

        NuevaPagina(esPrimera: true);

        // =====================================================================
        // SECCIÓN I: IDENTIFICACIÓN Y CONTACTO
        // =====================================================================
        DibujarTituloSeccion("I", "Identificación del Asociado y Contacto");

        var estadoMembresiaTexto = asociado.EstadoMembresia switch
        {
            "AlDia" => "Activo · Al día",
            "Pendiente" => "En revisión · Período de gracia",
            _ => string.IsNullOrWhiteSpace(asociado.EstadoMembresia) ? "Activo" : asociado.EstadoMembresia
        };

        var docIdentificacionTexto = !string.IsNullOrWhiteSpace(asociado.CedulaArchivoNombre)
            ? $"{asociado.CedulaArchivoNombre} · {(string.IsNullOrWhiteSpace(asociado.CedulaArchivoTamano) ? "Archivo registrado en el sistema" : asociado.CedulaArchivoTamano)}"
            : "Sin documento adjunto";

        DibujarFilaParDatos(
            "Nombre completo / Titular", asociado.NombreCompleto,
            "Documento de identificación (Cédula / NIT)", asociado.Cedula);

        DibujarFilaParDatos(
            "Teléfono / WhatsApp", asociado.Telefono,
            "Correo electrónico", string.IsNullOrWhiteSpace(asociado.Correo) ? "No registrado" : asociado.Correo);

        DibujarFilaParDatos(
            "Estado de membresía", estadoMembresiaTexto,
            "Fecha de registro en el sistema", asociado.FechaRegistro.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture));

        DibujarFilaAnchoCompleto(
            "Fotocopia de la cédula o documento NIT (Soporte de identificación)",
            docIdentificacionTexto,
            resaltar: !string.IsNullOrWhiteSpace(asociado.CedulaArchivoNombre));

        // =====================================================================
        // SECCIÓN II: PREDIO RURAL
        // =====================================================================
        DibujarTituloSeccion("II", "Información del Predio Rural");

        DibujarFilaParDatos(
            "Nombre del predio o finca", asociado.NombrePredio,
            "Vereda o sector", asociado.VeredaSector);

        DibujarFilaParDatos(
            "Municipio", FormatearMunicipio(asociado.Municipio),
            "Tenencia del terreno", string.IsNullOrWhiteSpace(asociado.TenenciaTerreno) ? "No especificada" : asociado.TenenciaTerreno);

        DibujarFilaAnchoCompleto(
            "Extensión total del predio",
            $"{asociado.HectareasPredio:0.#} hectáreas totales");

        // =====================================================================
        // SECCIÓN III: LÍNEA PRODUCTIVA Y CERTIFICACIÓN BPA
        // =====================================================================
        DibujarTituloSeccion("III", "Línea Productiva y Buenas Prácticas Agrícolas (BPA)");

        var cultivosTexto = AsociadoModel.FormatearNombreCultivo(asociado.CultivoPrincipal);
        var bpaEstadoTexto = asociado.TieneBPA
            ? "Sí cuenta con certificación BPA vigente"
            : "No registra certificación BPA";
        var bpaArchivoTexto = asociado.TieneBPA
            ? (!string.IsNullOrWhiteSpace(asociado.CertificadoBpaNombre)
                ? $"{asociado.CertificadoBpaNombre} ({(string.IsNullOrWhiteSpace(asociado.CertificadoBpaTamano) ? "Adjunto en expediente" : asociado.CertificadoBpaTamano)})"
                : "Certificado registrado sin nombre de archivo")
            : "No aplica";

        DibujarFilaParDatos(
            "Actividad productiva", string.IsNullOrWhiteSpace(asociado.TipoActividadProductiva) ? "No especificada" : asociado.TipoActividadProductiva,
            "Producción aproximada", string.IsNullOrWhiteSpace(asociado.ProduccionAproximada) ? "No especificada" : asociado.ProduccionAproximada);

        DibujarFilaParDatos(
            "Cultivos establecidos", cultivosTexto,
            "Área de la línea productiva", $"{(asociado.HectareasLineaProductiva > 0 ? asociado.HectareasLineaProductiva : asociado.HectareasPredio):0.#} ha productivas (de {asociado.HectareasPredio:0.#} ha totales)");

        DibujarFilaParDatos(
            "Buenas Prácticas Agrícolas (BPA)", bpaEstadoTexto,
            "Certificado BPA adjunto", bpaArchivoTexto);

        // =====================================================================
        // SECCIÓN IV: COMPONENTE SOCIAL Y JURÍDICO
        // =====================================================================
        DibujarTituloSeccion("IV", "Componente Social, Grupos Vulnerables y Soportes");

        if (asociado.GruposVulnerables.Count == 0)
        {
            var textoCondicion = string.Equals(asociado.EsVictima, "Si", StringComparison.OrdinalIgnoreCase)
                ? "Registra condición de vulnerabilidad (Víctima del conflicto armado)"
                : "Sin condiciones de vulnerabilidad reportadas en el registro.";
            DibujarFilaAnchoCompleto("Condiciones sociales registradas", textoCondicion);
        }
        else
        {
            foreach (var codigoGrupo in asociado.GruposVulnerables)
            {
                var nombreGrupo = AsociadoModel.TraducirGrupoVulnerable(codigoGrupo);
                var archivoSoporte = asociado.SoportesGruposVulnerables.TryGetValue(codigoGrupo, out var archivo) && !string.IsNullOrWhiteSpace(archivo)
                    ? $"Archivo soporte: {archivo}"
                    : "Soporte registrado en el expediente";

                DibujarFilaParDatos(
                    "Condición / Grupo vulnerable", nombreGrupo,
                    "Documento soporte vinculado", archivoSoporte);
            }
        }

        // =====================================================================
        // SECCIÓN V: CUOTAS SOCIALES Y APORTES
        // =====================================================================
        DibujarTituloSeccion("V", "Cuotas Sociales, Aportes y Comprobantes");

        DibujarFilaParDatos(
            "Valor total pagado acumulado", $"${asociado.ValorTotalPagadoAcumulado:N0} COP",
            "Valor total pendiente acumulado", $"${asociado.ValorTotalPendienteAcumulado:N0} COP ({asociado.TotalCuotasPendientes} cuota(s) pendiente(s))");

        DibujarFilaParDatos(
            "Estado general de pago", asociado.EstadoGeneralCuotas,
            "Total de comprobantes asociados", $"{asociado.TotalSoportesCuotas} comprobante(s) registrado(s)");

        if (asociado.CuotasAportes.Count == 0)
        {
            DibujarFilaAnchoCompleto("Detalle de cuotas y aportes", "No se registran cuotas o aportes individuales para este asociado.");
        }
        else
        {
            for (var idx = 0; idx < asociado.CuotasAportes.Count; idx++)
            {
                var cuota = asociado.CuotasAportes[idx];
                var estadoCuota = cuota.CalcularEstadoAutomatico();
                var vencimientoTexto = cuota.FechaVencimiento?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "No definida";
                var comprobantesTexto = cuota.Soportes.Count > 0
                    ? string.Join(" | ", cuota.Soportes.Select(s => $"{s.ArchivoNombre} ({(string.IsNullOrWhiteSpace(s.ArchivoTamano) ? "Soporte adjunto" : s.ArchivoTamano)})"))
                    : "Sin comprobante adjunto";

                AsegurarEspacio(78.0);
                var ySub = cursorY - 18.0;
                RectanguloRelleno(paginaActual.Stream, MarginX, ySub, ContentWidth, 18.0, 0.89, 0.94, 0.95);
                EscribirTexto(
                    paginaActual.Stream,
                    Bold: true,
                    8.5,
                    MarginX + 8.0,
                    ySub + 5.0,
                    $"REGISTRO #{idx + 1} · CUOTA O APORTE DE {cuota.TipoCuota.ToUpperInvariant()} ({cuota.Periodicidad.ToUpperInvariant()}) — ESTADO: {estadoCuota.ToUpperInvariant()}",
                    0.09, 0.22, 0.16);
                cursorY = ySub - 4.0;

                DibujarFilaParDatos(
                    "Tipo de cuota y periodicidad", $"{cuota.TipoCuota} · Periodicidad: {cuota.Periodicidad}",
                    "Valor de la cuota y fecha de vencimiento", $"Valor: ${cuota.ValorCuota:N0} COP · Vencimiento / Próx. pago: {vencimientoTexto}");

                DibujarFilaParDatos(
                    "Cuotas pendientes y valor total pendiente", $"{cuota.CantidadCuotasPendientes} cuota(s) · Total pendiente: ${cuota.ValorTotalPendiente:N0} COP",
                    "Valor total pagado y estado de pago", $"Total pagado: ${cuota.ValorTotalPagado:N0} COP · Estado: {estadoCuota}");

                DibujarFilaAnchoCompleto(
                    $"Documento(s) / comprobante(s) asociado(s) a {cuota.TipoCuota} ({cuota.Periodicidad})",
                    comprobantesTexto,
                    resaltar: cuota.Soportes.Count > 0);
            }
        }

        // =====================================================================
        // SECCIÓN VI: RELACIÓN CONSOLIDADA DE DOCUMENTOS ADJUNTOS
        // =====================================================================
        DibujarTituloSeccion("VI", "Relación de Documentos y Archivos Adjuntos del Registro");

        if (documentosAdjuntos.Count == 0)
        {
            DibujarFilaAnchoCompleto("Archivos adjuntos", "No hay archivos adjuntos registrados en este expediente.");
        }
        else
        {
            var cantIncrustados = documentosAdjuntos.Count(d => d.TieneArchivoBinario);
            if (cantIncrustados > 0)
            {
                DibujarFilaAnchoCompleto(
                    "Archivos digitales incrustados en este PDF",
                    $"Este PDF contiene {cantIncrustados} archivo(s) original(es) incrustado(s) directamente en el contenedor PDF (accesibles desde el panel de archivos adjuntos del visor PDF), además de las referencias detalladas a continuación.",
                    resaltar: true);
            }

            for (var i = 0; i < documentosAdjuntos.Count; i++)
            {
                var doc = documentosAdjuntos[i];
                var disponibilidad = doc.TieneArchivoBinario
                    ? $"Archivo adjunto incrustado en este PDF y referenciado ({doc.Tamano})"
                    : $"Referencia registrada en el expediente ({doc.Tamano})";

                DibujarFilaParDatos(
                    $"Adjunto #{i + 1} · {doc.Seccion} ({doc.Respaldo})",
                    $"Archivo: {doc.NombreArchivo}",
                    "Disponibilidad y respaldo",
                    $"{disponibilidad} · {doc.Detalle}");
            }
        }

        var totalPaginas = paginas.Count;
        for (var i = 0; i < totalPaginas; i++)
        {
            DibujarPiePagina(paginas[i].Stream, asociado, generadoEn, i + 1, totalPaginas);
        }

        return CompilarDocumentoPdf(paginas, documentosAdjuntos);
    }

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
                Detalle = $"Documento obligatorio de identificación del titular CC/NIT {asociado.Cedula}",
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
                Detalle = $"Certificado de Buenas Prácticas Agrícolas · Predio {asociado.NombrePredio}",
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
                        Detalle = $"Respalda cuota de {cuota.TipoCuota} (${cuota.ValorCuota:N0} COP) · Estado: {cuota.CalcularEstadoAutomatico()}",
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

    private static void DibujarEncabezadoPrincipal(StringBuilder sb, AsociadoModel asociado, DateTime generadoEn)
    {
        RectanguloRelleno(sb, MarginX, PageHeight - 98.0, ContentWidth, 62.0, 0.09, 0.22, 0.16);
        RectanguloRelleno(sb, MarginX, PageHeight - 102.0, ContentWidth, 4.0, 0.78, 0.91, 0.42);

        EscribirTexto(sb, Bold: true, 8.0, MarginX + 14.0, PageHeight - 52.0, "ASOAGRO · SISTEMA DE GESTIÓN DE ASOCIADOS · URABÁ", 0.78, 0.91, 0.42);
        EscribirTexto(sb, Bold: true, 15.0, MarginX + 14.0, PageHeight - 70.0, "REPORTE DE REGISTRO DEL ASOCIADO", 1.0, 1.0, 1.0);
        EscribirTexto(sb, Bold: false, 8.8, MarginX + 14.0, PageHeight - 85.0, $"Asociado: {asociado.NombreCompleto} · CC/NIT: {asociado.Cedula}", 0.90, 0.95, 0.91);

        var fechaTexto = $"Generado: {generadoEn:dd/MM/yyyy HH:mm}";
        var folioTexto = $"Registro CC/NIT: {asociado.Cedula}";
        EscribirTexto(sb, Bold: true, 8.5, PageWidth - MarginX - 155.0, PageHeight - 56.0, folioTexto, 1.0, 1.0, 1.0);
        EscribirTexto(sb, Bold: false, 8.0, PageWidth - MarginX - 155.0, PageHeight - 70.0, fechaTexto, 0.88, 0.93, 0.89);
        EscribirTexto(sb, Bold: false, 7.8, PageWidth - MarginX - 155.0, PageHeight - 84.0, "Reporte generado por el sistema", 0.78, 0.91, 0.42);
    }

    private static void DibujarEncabezadoContinuacion(StringBuilder sb, AsociadoModel asociado, DateTime generadoEn)
    {
        RectanguloRelleno(sb, MarginX, PageHeight - 62.0, ContentWidth, 26.0, 0.09, 0.22, 0.16);
        EscribirTexto(sb, Bold: true, 8.5, MarginX + 10.0, PageHeight - 52.0, $"ASOAGRO · REPORTE DE ASOCIADO: {asociado.NombreCompleto.ToUpperInvariant()} (CC/NIT {asociado.Cedula})", 1.0, 1.0, 1.0);
        EscribirTexto(sb, Bold: false, 8.0, PageWidth - MarginX - 135.0, PageHeight - 52.0, $"Generado: {generadoEn:dd/MM/yyyy HH:mm}", 0.78, 0.91, 0.42);
    }

    private static void DibujarPiePagina(StringBuilder sb, AsociadoModel asociado, DateTime generadoEn, int paginaActual, int totalPaginas)
    {
        RectanguloRelleno(sb, MarginX, 46.0, ContentWidth, 0.8, 0.78, 0.82, 0.79);

        EscribirTexto(
            sb,
            Bold: false,
            7.4,
            MarginX,
            34.0,
            $"Reporte formal generado por el sistema ASOAGRO el {generadoEn:dd/MM/yyyy a las HH:mm} · Asociado: {asociado.NombreCompleto} (CC/NIT {asociado.Cedula}).",
            0.38, 0.45, 0.41);

        EscribirTexto(
            sb,
            Bold: false,
            7.0,
            MarginX,
            23.0,
            "La información y referencias documentales contenidas en este reporte corresponden a los datos registrados en el sistema.",
            0.46, 0.52, 0.48);

        EscribirTexto(
            sb,
            Bold: true,
            8.2,
            PageWidth - MarginX - 72.0,
            30.0,
            $"Página {paginaActual} de {totalPaginas}",
            0.09, 0.22, 0.16);
    }

    private static string FormatearMunicipio(string? municipio) => municipio switch
    {
        "Apartado" => "Apartadó",
        "Chigorodo" => "Chigorodó",
        "Mutata" => "Mutatá",
        "Necocli" => "Necoclí",
        "San Pedro de Uraba" => "San Pedro de Urabá",
        _ => string.IsNullOrWhiteSpace(municipio) ? "No especificado" : municipio
    };

    private static List<string> EnvolverTexto(string texto, int maxCaracteresPorLinea)
    {
        var resultado = new List<string>();
        if (string.IsNullOrWhiteSpace(texto))
        {
            resultado.Add("No registrado");
            return resultado;
        }

        var limpio = texto.Replace("\r", " ").Replace("\n", " ").Trim();
        var palabras = limpio.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var actual = new StringBuilder();

        foreach (var palabra in palabras)
        {
            if (actual.Length == 0)
            {
                actual.Append(palabra);
            }
            else if (actual.Length + 1 + palabra.Length <= maxCaracteresPorLinea)
            {
                actual.Append(' ').Append(palabra);
            }
            else
            {
                resultado.Add(actual.ToString());
                actual.Clear();
                actual.Append(palabra);
            }
        }

        if (actual.Length > 0)
        {
            resultado.Add(actual.ToString());
        }

        return resultado;
    }

    private static void RectanguloRelleno(StringBuilder sb, double x, double y, double w, double h, double r, double g, double b)
    {
        sb.AppendFormat(
            CultureInfo.InvariantCulture,
            "q {0:0.###} {1:0.###} {2:0.###} rg {3:0.##} {4:0.##} {5:0.##} {6:0.##} re f Q\n",
            r, g, b, x, y, w, h);
    }

    private static void RectanguloConBorde(
        StringBuilder sb,
        double x,
        double y,
        double w,
        double h,
        double fillR,
        double fillG,
        double fillB,
        double strokeR,
        double strokeG,
        double strokeB)
    {
        sb.AppendFormat(
            CultureInfo.InvariantCulture,
            "q {0:0.###} {1:0.###} {2:0.###} rg {3:0.###} {4:0.###} {5:0.###} RG 0.75 w {6:0.##} {7:0.##} {8:0.##} {9:0.##} re B Q\n",
            fillR, fillG, fillB, strokeR, strokeG, strokeB, x, y, w, h);
    }

    private static void EscribirTexto(
        StringBuilder sb,
        bool Bold,
        double fontSize,
        double x,
        double y,
        string texto,
        double r,
        double g,
        double b)
    {
        var fontRef = Bold ? "/F2" : "/F1";
        var hex = ConvertirAHexWinAnsi(texto);
        sb.AppendFormat(
            CultureInfo.InvariantCulture,
            "BT {0} {1:0.##} Tf {2:0.###} {3:0.###} {4:0.###} rg 1 0 0 1 {5:0.##} {6:0.##} Tm <{7}> Tj ET\n",
            fontRef, fontSize, r, g, b, x, y, hex);
    }

    private static string ConvertirAHexWinAnsi(string texto)
    {
        var sb = new StringBuilder(texto.Length * 2);
        foreach (var ch in texto)
        {
            byte b = ch switch
            {
                '\u2022' => 0x95,
                '\u2013' => 0x96,
                '\u2014' => 0x97,
                '\u2018' or '\u2019' => 0x27,
                '\u201C' or '\u201D' => 0x22,
                <= '\u00FF' => (byte)ch,
                _ => (byte)'?'
            };
            sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    private static byte[] CompilarDocumentoPdf(List<PdfPageBuilder> paginas, List<DocumentoAdjuntoPdf> adjuntos)
    {
        using var ms = new MemoryStream();
        void WriteAscii(string s)
        {
            var bytes = Encoding.ASCII.GetBytes(s);
            ms.Write(bytes, 0, bytes.Length);
        }

        var adjuntosBinarios = adjuntos.Where(a => a.TieneArchivoBinario).ToList();

        // Objetos base:
        // 1: Catalog
        // 2: Pages
        // 3: Font F1 (Helvetica)
        // 4: Font F2 (Helvetica-Bold)
        // Para cada página i (0..N-1):
        //   5 + (i * 2): Page object
        //   6 + (i * 2): Content stream object
        // Para cada adjunto binario k (0..M-1):
        //   baseAdjuntos + (k * 2): EmbeddedFile stream object
        //   baseAdjuntos + (k * 2) + 1: Filespec object
        var baseAdjuntos = 5 + (paginas.Count * 2);
        var totalObjetos = 4 + (paginas.Count * 2) + (adjuntosBinarios.Count * 2);
        var offsets = new long[totalObjetos + 1];

        WriteAscii("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");

        // 1: Catalog (con /Names /EmbeddedFiles cuando existen adjuntos binarios en memoria)
        offsets[1] = ms.Position;
        if (adjuntosBinarios.Count > 0)
        {
            var namesEntries = new StringBuilder();
            for (var k = 0; k < adjuntosBinarios.Count; k++)
            {
                var filespecObjId = baseAdjuntos + (k * 2) + 1;
                var claveUnica = $"{k + 1:00}_{adjuntosBinarios[k].NombreArchivo}";
                namesEntries.Append($"<{ConvertirAHexWinAnsi(claveUnica)}> {filespecObjId} 0 R ");
            }

            WriteAscii($"1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles << /Names [ {namesEntries}] >> >> >>\nendobj\n");
        }
        else
        {
            WriteAscii("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        }

        // 2: Pages
        var kidsRefs = string.Join(" ", Enumerable.Range(0, paginas.Count).Select(i => $"{5 + (i * 2)} 0 R"));
        offsets[2] = ms.Position;
        WriteAscii($"2 0 obj\n<< /Type /Pages /Kids [ {kidsRefs} ] /Count {paginas.Count} >>\nendobj\n");

        // 3: Font Helvetica
        offsets[3] = ms.Position;
        WriteAscii("3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");

        // 4: Font Helvetica-Bold
        offsets[4] = ms.Position;
        WriteAscii("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

        // Páginas
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

        // Archivos adjuntos incrustados (/EmbeddedFile + /Filespec)
        for (var k = 0; k < adjuntosBinarios.Count; k++)
        {
            var adj = adjuntosBinarios[k];
            var rawBytes = adj.ContenidoBytes!;
            var efObjId = baseAdjuntos + (k * 2);
            var fsObjId = baseAdjuntos + (k * 2) + 1;

            offsets[efObjId] = ms.Position;
            WriteAscii($"{efObjId} 0 obj\n<< /Type /EmbeddedFile /Params << /Size {rawBytes.Length} >> >>\nstream\n");
            ms.Write(rawBytes, 0, rawBytes.Length);
            WriteAscii("\nendstream\nendobj\n");

            var hexNombre = ConvertirAHexWinAnsi(adj.NombreArchivo);
            var hexDesc = ConvertirAHexWinAnsi($"{adj.Seccion} - {adj.Respaldo}");
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

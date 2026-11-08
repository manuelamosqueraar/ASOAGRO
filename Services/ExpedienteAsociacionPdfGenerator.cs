using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Asoagro.Models;

namespace Asoagro.Services;

/// <summary>
/// Generador formal del Informe del Expediente Institucional de la Asociación (PDF 1.4).
/// Presenta la información en formato de informe institucional con redacción coherente por secciones y tabla de soportes.
/// </summary>
public static class ExpedienteAsociacionPdfGenerator
{
    public static byte[] GenerarResumenPdf(
        AsociacionInstitucional asoc,
        IEnumerable<DocumentoAsociacion> documentos,
        string funcionarioResponsable = "Secretaría FAAUA",
        DateTime? fechaGeneracion = null)
    {
        var items = (documentos ?? Enumerable.Empty<DocumentoAsociacion>()).Select(d => new DocumentoItemInstitucional
        {
            CategoriaSeccion = d.Categoria,
            TipoDocumento = d.TipoDocumento,
            Titulo = d.Titulo,
            Aplica = d.Aplica,
            EstadoRequisito = d.Aplica ? "Aplica" : "No aplica",
            MotivoNoAplica = d.MotivoNoAplica,
            FechaEmision = d.FechaEmision,
            TieneVencimiento = d.TieneVencimiento,
            FechaVencimiento = d.FechaVencimiento,
            NumeroReferencia = d.NumeroReferencia,
            EntidadEmisora = d.EntidadEmisora,
            Periodo = d.Periodo,
            Observaciones = d.Observaciones,
            ArchivoNombre = d.ArchivoNombre,
            ArchivoContenido = d.ArchivoContenido,
            ArchivoTipoMime = d.ArchivoTipoMime,
            TamanoBytes = d.TamanoBytes,
            EstadoRevision = d.EstadoRevision
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
        var listaDocs = (documentos ?? Enumerable.Empty<DocumentoItemInstitucional>()).ToList();

        var nombreLegal = ValorONoRegistrado(asoc.NombreLegal);
        var nitTexto = FormatearNit(asoc.Nit, asoc.DigitoVerificacion);
        var estadoAsoc = ValorONoRegistrado(asoc.Estado);
        var tituloEntidad = !string.IsNullOrWhiteSpace(asoc.Sigla)
            ? $"{asoc.Sigla.Trim()} — {nombreLegal}"
            : nombreLegal;

        var ctx = new PdfInformeEngine.DocumentoInformeContexto
        {
            AreaInstitucional = "SECRETARÍA GENERAL · GESTIÓN DOCUMENTAL E INSTITUCIONAL",
            TituloDocumento = "INFORME DEL EXPEDIENTE INSTITUCIONAL DE LA ASOCIACIÓN",
            SubtituloDocumento = $"Asociación: {tituloEntidad}  |  NIT: {nitTexto}  |  Estado: {estadoAsoc}",
            ReferenciaDerecha = $"NIT: {nitTexto}",
            TituloCortoContinuacion = $"EXPEDIENTE INSTITUCIONAL — {tituloEntidad.ToUpperInvariant()}",
            NotaPieLinea1 = $"Informe generado por el sistema ASOAGRO el {generadoEn:dd/MM/yyyy a las HH:mm} · Responsable: {ValorONoRegistrado(funcionarioResponsable)}.",
            NotaPieLinea2 = "Este informe consolida los registros almacenados en el sistema y no reemplaza certificados oficiales ni implica validación legal externa.",
            FechaGeneracion = generadoEn
        };

        ctx.IniciarDocumento();

        // Determinar si el objeto de asociación trae ficha institucional extendida o únicamente datos básicos del expediente
        var tieneFichaExtendida =
            !string.IsNullOrWhiteSpace(asoc.DireccionPrincipal) ||
            !string.IsNullOrWhiteSpace(asoc.RepLegalNombre) ||
            !string.IsNullOrWhiteSpace(asoc.CorreoInstitucional) ||
            !string.IsNullOrWhiteSpace(asoc.EntidadInscripcion) ||
            !string.IsNullOrWhiteSpace(asoc.NumeroRegistro) ||
            asoc.FechaConstitucion.HasValue;

        // =====================================================================
        // SECCIÓN I: IDENTIFICACIÓN DE LA ASOCIACIÓN
        // =====================================================================
        PdfInformeEngine.DibujarTituloSeccion(ctx, "I", "Identificación Institucional de la Asociación");

        if (tieneFichaExtendida)
        {
            var siglaTexto = string.IsNullOrWhiteSpace(asoc.Sigla) ? "No registrado" : asoc.Sigla.Trim();
            var parrafoId1 = new List<PdfInformeEngine.FragmentoTexto>
            {
                new("El presente informe describe la información institucional y documental registrada en el sistema ASOAGRO para la entidad "),
                new(nombreLegal, true),
                new(", con sigla institucional "),
                new(siglaTexto, true),
                new(" y NIT "),
                new(nitTexto, true),
                new(". La organización se clasifica como "),
                new(ValorONoRegistrado(asoc.TipoOrganizacion), true),
                new(" dedicada a la actividad productiva "),
                new(ValorONoRegistrado(asoc.ActividadProductiva), true),
                new(", con ámbito territorial "),
                new(ValorONoRegistrado(asoc.Ambito), true),
                new(", nivel de organización "),
                new(ValorONoRegistrado(asoc.GradoOrganizacion), true),
                new(" y estado institucional "),
                new(estadoAsoc, true),
                new(".")
            };
            PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoId1);

            var parrafoId2 = new List<PdfInformeEngine.FragmentoTexto>
            {
                new("Respecto a su constitución y registro legal, figura con fecha de constitución "),
                new(FormatearFecha(asoc.FechaConstitucion), true),
                new(", inscrita ante "),
                new(ValorONoRegistrado(asoc.EntidadInscripcion), true),
                new(" bajo el número de matrícula o radicado "),
                new(ValorONoRegistrado(asoc.NumeroRegistro), true),
                new(", con fecha de inscripción "),
                new(FormatearFecha(asoc.FechaInscripcion), true),
                new(" y fecha de renovación de matrícula "),
                new(FormatearFecha(asoc.FechaRenovacionMatricula), true),
                new(".")
            };

            if (!string.IsNullOrWhiteSpace(asoc.ExcepcionesIdentificacion))
            {
                parrafoId2.Add(new(" Observaciones o excepciones registradas en identificación: "));
                parrafoId2.Add(new(asoc.ExcepcionesIdentificacion.Trim(), true));
                parrafoId2.Add(new("."));
            }

            PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoId2);

            // =====================================================================
            // SECCIÓN II: DOMICILIO Y CONTACTO INSTITUCIONAL
            // =====================================================================
            PdfInformeEngine.DibujarTituloSeccion(ctx, "II", "Domicilio Principal y Contacto Institucional");

            var dirNotificaciones = asoc.NotificacionesDiferentes && !string.IsNullOrWhiteSpace(asoc.DireccionNotificaciones)
                ? asoc.DireccionNotificaciones.Trim()
                : (string.IsNullOrWhiteSpace(asoc.DireccionPrincipal) ? "No registrado" : "Misma dirección de la sede principal");

            var parrafoDomicilio = new List<PdfInformeEngine.FragmentoTexto>
            {
                new("La asociación registra su domicilio principal en el municipio de "),
                new(ValorONoRegistrado(asoc.Municipio), true),
                new(!string.IsNullOrWhiteSpace(asoc.Departamento) ? $" ({asoc.Departamento.Trim()})" : ""),
                new(", en la dirección "),
                new(ValorONoRegistrado(asoc.DireccionPrincipal), true),
                new(", y dirección para notificaciones en "),
                new(dirNotificaciones, true),
                new(". Como canales de comunicación oficial registra el correo electrónico "),
                new(ValorONoRegistrado(asoc.CorreoInstitucional), true),
                new(", el teléfono de contacto "),
                new(ValorONoRegistrado(asoc.TelefonoContacto), true),
                new(" y sitio web "),
                new(ValorONoRegistrado(asoc.SitioWeb), true),
                new(". La persona de enlace o contacto institucional registrada es "),
                new(ValorONoRegistrado(asoc.ContactoNombre), true),
                new(", desempeñando el cargo de "),
                new(ValorONoRegistrado(asoc.ContactoCargo), true),
                new(".")
            };
            PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoDomicilio);

            // =====================================================================
            // SECCIÓN III: RÉGIMEN CONSTITUTIVO, REPRESENTACIÓN Y GOBIERNO
            // =====================================================================
            PdfInformeEngine.DibujarTituloSeccion(ctx, "III", "Régimen Estatutario, Representación Legal y Órganos de Gobierno");

            var cuotasAcordadasTexto = asoc.CuotasInicialesAcordadas
                ? (string.IsNullOrWhiteSpace(asoc.DetalleCuotasIniciales) ? "Acordadas en asamblea" : asoc.DetalleCuotasIniciales.Trim())
                : "No acordadas inicialmente";
            var reglasAutogobiernoTexto = string.IsNullOrWhiteSpace(asoc.ReglasAutogobierno)
                ? "Directrices estatutarias de asamblea"
                : asoc.ReglasAutogobierno.Trim();

            var parrafoEstatutos = new List<PdfInformeEngine.FragmentoTexto>
            {
                new("En el marco constitutivo y estatutario (Ley 2219 de 2022), la fecha de asamblea fundacional registrada es "),
                new(FormatearFecha(asoc.FechaAsambleaConstitucion), true),
                new(", con estado de aprobación de estatutos "),
                new(asoc.AprobacionEstatutos ? "Aprobados en asamblea" : "Pendiente", true),
                new(", cuotas de sostenimiento "),
                new(cuotasAcordadasTexto, true),
                new(" y mecanismos de autogobierno definidos bajo "),
                new(reglasAutogobiernoTexto, true),
                new(".")
            };
            PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoEstatutos);

            var periodoRepTexto = (asoc.RepLegalPeriodoInicio.HasValue || asoc.RepLegalPeriodoFin.HasValue)
                ? $"del {FormatearFecha(asoc.RepLegalPeriodoInicio)} al {FormatearFecha(asoc.RepLegalPeriodoFin)}"
                : "No registrado";

            var fiscalDetalle = asoc.FiscalAplica
                ? $"{ValorONoRegistrado(asoc.FiscalTipo)}: {ValorONoRegistrado(asoc.FiscalNombre)} (Período: {ValorONoRegistrado(asoc.FiscalPeriodo)})"
                : $"No aplica ({ValorONoRegistrado(asoc.FiscalMotivoNoAplica)})";

            var parrafoGobierno = new List<PdfInformeEngine.FragmentoTexto>
            {
                new("La representación legal de la asociación se encuentra a cargo de "),
                new(ValorONoRegistrado(asoc.RepLegalNombre), true),
                new(", con documento de identificación "),
                new(ValorONoRegistrado(asoc.RepLegalDocumento), true),
                new(", período de nombramiento "),
                new(periodoRepTexto, true),
                new(" y fecha de inscripción en registro "),
                new(FormatearFecha(asoc.RepLegalFechaInscripcion), true),
                new(". En la Junta Directiva figura como presidente "),
                new(ValorONoRegistrado(asoc.JuntaPresidente), true),
                new(" para el período "),
                new(ValorONoRegistrado(asoc.JuntaPeriodo), true),
                new(". Respecto al órgano de fiscalización y control: "),
                new(fiscalDetalle, true),
                new(".")
            };
            PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoGobierno);

            // =====================================================================
            // SECCIÓN IV: INFORMACIÓN TRIBUTARIA
            // =====================================================================
            PdfInformeEngine.DibujarTituloSeccion(ctx, "IV", "Identificación y Situación Tributaria Registrada");

            var responsabilidadesTexto = asoc.ResponsabilidadesTributarias is { Count: > 0 }
                ? string.Join(", ", asoc.ResponsabilidadesTributarias)
                : "No registrado";

            var parrafoTributario = new List<PdfInformeEngine.FragmentoTexto>
            {
                new("En relación con las obligaciones tributarias registradas, el estado en el Registro Único Tributario (RUT) es "),
                new(ValorONoRegistrado(asoc.RutEstado), true),
                new(", con fecha de actualización "),
                new(FormatearFecha(asoc.RutFechaActualizacion), true),
                new(", bajo el régimen tributario declarado "),
                new(ValorONoRegistrado(asoc.RegimenTributario), true),
                new(". ")
            };

            if (!string.IsNullOrWhiteSpace(asoc.RegimenTributario) &&
                asoc.RegimenTributario.Contains("Especial", StringComparison.OrdinalIgnoreCase))
            {
                parrafoTributario.Add(new("Para el trámite del Régimen Tributario Especial (RTE) ante la DIAN, registra estado "));
                parrafoTributario.Add(new(ValorONoRegistrado(asoc.RteEstadoDian), true));
                parrafoTributario.Add(new(" con fecha límite de renovación "));
                parrafoTributario.Add(new(FormatearFecha(asoc.RteFechaLimiteRenovacion), true));
                parrafoTributario.Add(new(". "));
            }

            parrafoTributario.Add(new("Las responsabilidades tributarias asociadas en el RUT son: "));
            parrafoTributario.Add(new(responsabilidadesTexto, true));
            parrafoTributario.Add(new("."));

            PdfInformeEngine.DibujarParrafoMixto(ctx, parrafoTributario);
        }
        else
        {
            // Cuando el informe se genera únicamente con los datos básicos del expediente documental
            PdfInformeEngine.DibujarParrafoMixto(ctx, new[]
            {
                new PdfInformeEngine.FragmentoTexto("El presente informe consolida el estado de los documentos y soportes registrados en el sistema ASOAGRO para la asociación "),
                new PdfInformeEngine.FragmentoTexto(tituloEntidad, true),
                new PdfInformeEngine.FragmentoTexto(", identificada con NIT "),
                new PdfInformeEngine.FragmentoTexto(nitTexto, true),
                new PdfInformeEngine.FragmentoTexto(", cuyo estado actual en el expediente es "),
                new PdfInformeEngine.FragmentoTexto(estadoAsoc, true),
                new PdfInformeEngine.FragmentoTexto(".")
            });
        }

        // =====================================================================
        // SECCIÓN DOCUMENTAL: INVENTARIO DE DOCUMENTOS Y SOPORTES EN EL SISTEMA
        // =====================================================================
        var numSeccionDocs = tieneFichaExtendida ? "V" : "II";
        PdfInformeEngine.DibujarTituloSeccion(ctx, numSeccionDocs, "Relación de Documentos y Soportes del Expediente Digital");

        if (listaDocs.Count == 0)
        {
            PdfInformeEngine.DibujarParrafo(
                ctx,
                "No se registran documentos ni soportes cargados en el expediente de esta asociación a la fecha de generación del informe.");
        }
        else
        {
            var totalDocs = listaDocs.Count;
            var conArchivo = listaDocs.Count(d => d.Aplica && !string.IsNullOrWhiteSpace(d.ArchivoNombre));
            var pendientesCarga = listaDocs.Count(d => d.Aplica && string.IsNullOrWhiteSpace(d.ArchivoNombre));
            var noAplican = listaDocs.Count(d => !d.Aplica);

            PdfInformeEngine.DibujarParrafoMixto(ctx, new[]
            {
                new PdfInformeEngine.FragmentoTexto("El expediente documental registra un total de "),
                new PdfInformeEngine.FragmentoTexto($"{totalDocs} documento(s)", true),
                new PdfInformeEngine.FragmentoTexto(", de los cuales "),
                new PdfInformeEngine.FragmentoTexto($"{conArchivo}", true),
                new PdfInformeEngine.FragmentoTexto(" cuentan con archivo digital almacenado en el sistema, "),
                new PdfInformeEngine.FragmentoTexto($"{pendientesCarga}", true),
                new PdfInformeEngine.FragmentoTexto(" se encuentran pendientes de carga y "),
                new PdfInformeEngine.FragmentoTexto($"{noAplican}", true),
                new PdfInformeEngine.FragmentoTexto(" figuran como no aplicables. El estado indicado para cada archivo refleja su almacenamiento en el sistema y las fechas reportadas en su registro, sin constituir validación legal externa.")
            });

            var columnasDocs = new List<PdfInformeEngine.ColumnaTabla>
            {
                new() { Titulo = "N°", Ancho = 22.0, Negrita = true },
                new() { Titulo = "Documento Institucional", Ancho = 152.0, Negrita = true },
                new() { Titulo = "Archivo y Estado en Sistema", Ancho = 142.0 },
                new() { Titulo = "Emisión y Vencimiento", Ancho = 92.0 },
                new() { Titulo = "Referencia / Observaciones", Ancho = 108.0 }
            };

            var filasDocs = new List<string[]>();
            for (var i = 0; i < listaDocs.Count; i++)
            {
                var doc = listaDocs[i];
                var nombreDoc = !string.IsNullOrWhiteSpace(doc.TipoDocumento)
                    ? doc.TipoDocumento.Trim()
                    : ValorONoRegistrado(doc.Titulo);

                string estadoArchivo;
                if (!doc.Aplica)
                {
                    estadoArchivo = "No aplica";
                }
                else if (!string.IsNullOrWhiteSpace(doc.ArchivoNombre))
                {
                    estadoArchivo = $"Almacenado en el sistema: {doc.ArchivoNombre.Trim()}";
                }
                else
                {
                    estadoArchivo = "Pendiente de carga (Sin archivo)";
                }

                var emisionTexto = doc.FechaEmision.HasValue
                    ? $"Emisión: {doc.FechaEmision.Value:dd/MM/yyyy}"
                    : "Emisión: No registrado";

                string vencimientoTexto;
                if (!doc.Aplica)
                {
                    vencimientoTexto = "Venc.: No aplica";
                }
                else if (doc.FechaVencimiento.HasValue)
                {
                    vencimientoTexto = $"Venc.: {doc.FechaVencimiento.Value:dd/MM/yyyy}";
                }
                else
                {
                    vencimientoTexto = "Sin vencimiento registrado";
                }

                var detallesObs = new List<string>();
                if (!doc.Aplica && !string.IsNullOrWhiteSpace(doc.MotivoNoAplica))
                {
                    detallesObs.Add($"Motivo: {doc.MotivoNoAplica.Trim()}");
                }
                if (!string.IsNullOrWhiteSpace(doc.NumeroReferencia))
                {
                    detallesObs.Add($"Ref.: {doc.NumeroReferencia.Trim()}");
                }
                if (!string.IsNullOrWhiteSpace(doc.Observaciones))
                {
                    detallesObs.Add(doc.Observaciones.Trim());
                }

                var obsTexto = detallesObs.Count > 0 ? string.Join(" · ", detallesObs) : "No registrado";

                filasDocs.Add(new[]
                {
                    (i + 1).ToString(CultureInfo.InvariantCulture),
                    nombreDoc,
                    estadoArchivo,
                    $"{emisionTexto} | {vencimientoTexto}",
                    obsTexto
                });
            }

            PdfInformeEngine.DibujarTablaFormal(ctx, columnasDocs, filasDocs);
        }

        PdfInformeEngine.DibujarPiePaginas(ctx);
        return PdfInformeEngine.CompilarPdf(ctx.Paginas);
    }

    private static string ValorONoRegistrado(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? "No registrado" : valor.Trim();

    private static string FormatearFecha(DateTime? fecha) =>
        fecha.HasValue && fecha.Value.Year > 1900
            ? fecha.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            : "No registrado";

    private static string FormatearNit(string? nit, string? dv)
    {
        if (string.IsNullOrWhiteSpace(nit)) return "No registrado";
        var nitLimpio = nit.Trim();
        if (string.IsNullOrWhiteSpace(dv) || nitLimpio.Contains('-')) return nitLimpio;
        return $"{nitLimpio}-{dv.Trim()}";
    }
}

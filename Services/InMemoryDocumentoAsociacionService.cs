using System.Collections.Concurrent;
using Asoagro.Models;

namespace Asoagro.Services;

public class InMemoryDocumentoAsociacionService : IDocumentoAsociacionService
{
    private static readonly ConcurrentDictionary<Guid, DocumentoAsociacion> Documentos = new();
    private static readonly ConcurrentDictionary<string, AsociacionInstitucional> Asociaciones = new(StringComparer.OrdinalIgnoreCase);

    private static readonly List<string> CatalogoAsociacionesBase = new()
    {
        "ASOAGRO - Asociación Agropecuaria de Urabá",
        "ASOPLÁTANO - Asociación de Productores de Plátano de Urabá",
        "ASOCACAO - Asociación de Cacaoteros de Urabá",
        "ASOPROMAGRO - Asociación de Productores Agropecuarios de Apartadó",
        "COAGROURABÁ - Cooperativa Agropecuaria de Urabá",
        "ASOGANADEROS - Asociación de Ganaderos y Productores de Urabá"
    };

    private static readonly List<string> CatalogoTiposDocumentosBase = new()
    {
        "Registro Único Tributario (RUT)",
        "Certificado de Existencia y Representación Legal (Cámara de Comercio)",
        "Acta de Constitución y Aprobación de Estatutos",
        "Estatutos Vigentes de la Asociación",
        "Acta de Asamblea General y Elección de Junta Directiva",
        "Constancia de Aceptación de Cargos Directivos",
        "Declaración de Renta e Ingresos Institucionales",
        "Estados Financieros Certificados con Dictamen",
        "Soportes de Actualización Anual Régimen Tributario Especial (RTE - DIAN)",
        "Certificado Fitosanitario / Registro ICA",
        "Certificación de Buenas Prácticas Agrícolas (BPA)",
        "Aval de Asistencia Técnica Agropecuaria",
        "Otro Documento Legal o Institucional"
    };

    static InMemoryDocumentoAsociacionService()
    {
        // Registrar ASOAGRO institucionalmente como entidad base
        var asoagro = new AsociacionInstitucional
        {
            NombreLegal = "Asociación Agropecuaria de Urabá",
            Sigla = "ASOAGRO",
            Nit = "900543210",
            DigitoVerificacion = "8",
            TipoOrganizacion = "Asociación agropecuaria",
            Ambito = "Regional",
            FechaConstitucion = new DateTime(2015, 6, 12),
            Estado = "Activa",
            NumeroRegistro = "00028471-03",
            EntidadInscripcion = "Cámara de Comercio de Urabá",
            FechaInscripcion = new DateTime(2015, 7, 1),
            FechaRenovacionMatricula = new DateTime(2026, 3, 31),
            Departamento = "Antioquia",
            Municipio = "Apartadó",
            DireccionPrincipal = "Carrera 100 # 98-45, Barrio Ortíz",
            CorreoInstitucional = "secretaria@asoagro-uraba.org",
            TelefonoContacto = "3104509876",
            SitioWeb = "https://asoagro-uraba.org",
            ContactoNombre = "Karen Argumedo",
            ContactoCargo = "Secretaria General",
            RepLegalNombre = "Manuela Andrea Mosquera",
            RepLegalDocumento = "1045123890",
            RepLegalPeriodoInicio = new DateTime(2024, 1, 1),
            RepLegalPeriodoFin = new DateTime(2027, 12, 31),
            JuntaPresidente = "Carlos Julio Ramírez",
            JuntaVicepresidente = "María Elena Mendoza",
            JuntaSecretario = "Karen Argumedo",
            JuntaTesorero = "Gabriel Restrepo",
            JuntaPeriodo = "2024 - 2027",
            FiscalAplica = true,
            FiscalNombre = "Claudia Patricia Henao",
            FiscalTipo = "Revisor fiscal externo",
            FiscalPeriodo = "2024 - 2026",
            RutEstado = "Inscrito y activo",
            RutFechaActualizacion = new DateTime(2024, 2, 15),
            ResponsabilidadesTributarias = new() { "Renta Régimen Ordinario", "Retención en la fuente", "Facturación Electrónica" },
            RegimenTributario = "Régimen Ordinario",
            FechaCreacion = DateTime.Now.AddMonths(-6),
            FechaActualizacion = DateTime.Now,
            CreadoPor = "Secretaría FAAUA",
            ActualizadoPor = "Secretaría FAAUA"
        };
        Asociaciones[asoagro.NombreLegal] = asoagro;
        Asociaciones[asoagro.Sigla] = asoagro;
        Asociaciones["ASOAGRO - Asociación Agropecuaria de Urabá"] = asoagro;
    }

    public IReadOnlyList<DocumentoAsociacion> ObtenerTodos()
    {
        return Documentos.Values
            .OrderByDescending(d => d.FechaRegistro)
            .ToList();
    }

    public IReadOnlyList<DocumentoAsociacion> ObtenerPorAsociacion(string nombreAsociacion)
    {
        if (string.IsNullOrWhiteSpace(nombreAsociacion))
            return ObtenerTodos();

        var nombreNormalizado = nombreAsociacion.Trim();

        return Documentos.Values
            .Where(d => string.Equals(d.NombreAsociacion.Trim(), nombreNormalizado, StringComparison.OrdinalIgnoreCase)
                     || d.NombreAsociacion.Contains(nombreNormalizado, StringComparison.OrdinalIgnoreCase)
                     || nombreNormalizado.Contains(d.NombreAsociacion, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(d => d.FechaRegistro)
            .ToList();
    }

    public DocumentoAsociacion? ObtenerPorId(Guid id)
    {
        Documentos.TryGetValue(id, out var doc);
        return doc;
    }

    public void Registrar(DocumentoAsociacion doc)
    {
        if (doc.Id == Guid.Empty)
        {
            doc.Id = Guid.NewGuid();
        }
        doc.FechaRegistro = DateTime.Now;
        Documentos[doc.Id] = doc;
    }

    public bool ActualizarVencimientoYArchivo(
        Guid id,
        DateTime? nuevaFechaVencimiento,
        string? nuevoArchivoNombre,
        byte[]? nuevoArchivoContenido,
        string? nuevasObservaciones,
        string usuario)
    {
        if (!Documentos.TryGetValue(id, out var doc))
            return false;

        doc.FechaVencimiento = nuevaFechaVencimiento;
        if (!string.IsNullOrWhiteSpace(nuevoArchivoNombre))
        {
            doc.ArchivoNombre = nuevoArchivoNombre;
            doc.ArchivoContenido = nuevoArchivoContenido;
        }

        if (!string.IsNullOrWhiteSpace(nuevasObservaciones))
        {
            doc.Observaciones = $"{doc.Observaciones} (Actualizado por {usuario} el {DateTime.Now:dd/MM/yyyy}: {nuevasObservaciones})".Trim();
        }

        return true;
    }

    public bool Eliminar(Guid id)
    {
        return Documentos.TryRemove(id, out _);
    }

    public IReadOnlyList<AlertaDocumentoAsociacion> ObtenerAlertas(DateTime fechaReferencia)
    {
        var fechaBase = fechaReferencia.Date;
        var alertas = new List<AlertaDocumentoAsociacion>();

        foreach (var doc in Documentos.Values)
        {
            if (!doc.Aplica) continue;
            if (!doc.FechaVencimiento.HasValue) continue;

            var vencimiento = doc.FechaVencimiento.Value.Date;
            var diasRestantes = (vencimiento - fechaBase).Days;

            // Se generan alertas para documentos vencidos o que venzan en los próximos 30 días
            if (diasRestantes <= 30)
            {
                string nivel;
                if (diasRestantes < 0)
                {
                    nivel = "Vencido";
                }
                else if (diasRestantes <= 7)
                {
                    nivel = "Crítica: vence en menos de 7 días";
                }
                else if (diasRestantes <= 15)
                {
                    nivel = "Advertencia: vence en menos de 15 días";
                }
                else
                {
                    nivel = "Aviso preventivo: vence en menos de 30 días";
                }

                alertas.Add(new AlertaDocumentoAsociacion
                {
                    DocumentoId = doc.Id,
                    NombreAsociacion = doc.NombreAsociacion,
                    NitAsociacion = doc.NitAsociacion,
                    TipoDocumento = doc.TipoDocumento,
                    Titulo = string.IsNullOrWhiteSpace(doc.Titulo) ? doc.TipoDocumento : doc.Titulo,
                    FechaVencimiento = vencimiento,
                    DiasRestantes = diasRestantes,
                    NivelAlerta = nivel,
                    ArchivoNombre = doc.ArchivoNombre,
                    Observaciones = doc.Observaciones
                });
            }
        }

        return alertas.OrderBy(a => a.DiasRestantes).ToList();
    }

    public ResumenExpedienteAsociacion ObtenerExpediente(string nombreAsociacion)
    {
        var docs = ObtenerPorAsociacion(nombreAsociacion);
        var nit = docs.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.NitAsociacion))?.NitAsociacion;

        if (string.IsNullOrWhiteSpace(nit))
        {
            var asoc = ObtenerAsociacionInstitucional(nombreAsociacion);
            if (asoc != null) nit = asoc.Nit;
        }

        var registrados = docs.Count;
        var vigentes = docs.Count(d => d.Aplica && d.EstadoCalculado == "Vigente");
        var porVencer = docs.Count(d => d.Aplica && d.EstadoCalculado == "Por vencer");
        var vencidos = docs.Count(d => d.Aplica && d.EstadoCalculado == "Vencido");
        var noAplica = docs.Count(d => !d.Aplica);

        // Documentos institucionales indispensables
        var indispensables = new List<string>
        {
            "Registro Único Tributario (RUT)",
            "Certificado de Existencia y Representación Legal (Cámara de Comercio)",
            "Acta de Constitución y Aprobación de Estatutos",
            "Estatutos Vigentes de la Asociación",
            "Acta de Asamblea General y Elección de Junta Directiva",
            "Declaración de Renta e Ingresos Institucionales"
        };

        var tiposRegistrados = docs
            .Select(d => d.TipoDocumento)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var faltantes = indispensables
            .Where(t => !tiposRegistrados.Contains(t))
            .ToList();

        return new ResumenExpedienteAsociacion
        {
            NombreAsociacion = nombreAsociacion,
            NitAsociacion = nit ?? "",
            TotalRegistrados = registrados,
            Vigentes = vigentes,
            PorVencer = porVencer,
            Vencidos = vencidos,
            NoAplicables = noAplica,
            DocumentosFaltantes = faltantes,
            Documentos = docs.ToList()
        };
    }

    public IReadOnlyList<string> ObtenerAsociaciones()
    {
        var asociadasConDocs = Documentos.Values
            .Select(d => d.NombreAsociacion)
            .Where(s => !string.IsNullOrWhiteSpace(s));

        var asociadasInst = Asociaciones.Values
            .Select(a => string.IsNullOrWhiteSpace(a.Sigla) ? a.NombreLegal : $"{a.Sigla} - {a.NombreLegal}");

        return CatalogoAsociacionesBase
            .Union(asociadasConDocs, StringComparer.OrdinalIgnoreCase)
            .Union(asociadasInst, StringComparer.OrdinalIgnoreCase)
            .OrderBy(a => a)
            .ToList();
    }

    public IReadOnlyList<string> ObtenerCatalogoTiposDocumentos()
    {
        return CatalogoTiposDocumentosBase;
    }

    public IReadOnlyList<AsociacionInstitucional> ObtenerAsociacionesInstitucionales()
    {
        return Asociaciones.Values
            .OrderBy(a => a.NombreLegal)
            .ToList();
    }

    public AsociacionInstitucional? ObtenerAsociacionInstitucional(string nombreAsociacion)
    {
        if (string.IsNullOrWhiteSpace(nombreAsociacion)) return null;

        var buscado = nombreAsociacion.Trim();
        if (Asociaciones.TryGetValue(buscado, out var asoc)) return asoc;

        return Asociaciones.Values.FirstOrDefault(a =>
            string.Equals(a.NombreLegal, buscado, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a.Sigla, buscado, StringComparison.OrdinalIgnoreCase) ||
            buscado.Contains(a.NombreLegal, StringComparison.OrdinalIgnoreCase) ||
            buscado.Contains(a.Sigla, StringComparison.OrdinalIgnoreCase));
    }

    public void GuardarAsociacionInstitucional(AsociacionInstitucional asociacion, IEnumerable<DocumentoItemInstitucional>? documentos = null)
    {
        var clave = string.IsNullOrWhiteSpace(asociacion.Sigla)
            ? asociacion.NombreLegal.Trim()
            : $"{asociacion.Sigla.Trim()} - {asociacion.NombreLegal.Trim()}";

        asociacion.FechaActualizacion = DateTime.Now;
        Asociaciones[clave] = asociacion;
        Asociaciones[asociacion.NombreLegal.Trim()] = asociacion;
        if (!string.IsNullOrWhiteSpace(asociacion.Sigla))
        {
            Asociaciones[asociacion.Sigla.Trim()] = asociacion;
        }

        // Si se proveen documentos institucionales, sincronizarlos con el repositorio general de documentos
        if (documentos != null)
        {
            var nombreAsoc = clave;
            var nitCompleto = string.IsNullOrWhiteSpace(asociacion.DigitoVerificacion)
                ? asociacion.Nit
                : $"{asociacion.Nit}-{asociacion.DigitoVerificacion}";

            foreach (var docItem in documentos)
            {
                // Buscar si ya existe este tipo de documento para la asociación
                var docExistente = Documentos.Values.FirstOrDefault(d =>
                    string.Equals(d.NombreAsociacion.Trim(), nombreAsoc.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(d.TipoDocumento.Trim(), docItem.TipoDocumento.Trim(), StringComparison.OrdinalIgnoreCase));

                if (docExistente != null)
                {
                    docExistente.NitAsociacion = nitCompleto;
                    docExistente.Titulo = string.IsNullOrWhiteSpace(docItem.Titulo) ? docItem.TipoDocumento : docItem.Titulo;
                    docExistente.Aplica = docItem.Aplica;
                    docExistente.MotivoNoAplica = docItem.MotivoNoAplica;
                    docExistente.FechaEmision = docItem.FechaEmision ?? DateTime.Today;
                    docExistente.FechaVencimiento = docItem.Aplica ? docItem.FechaVencimiento : null;
                    docExistente.NumeroReferencia = docItem.NumeroReferencia;
                    docExistente.EntidadEmisora = docItem.EntidadEmisora;
                    docExistente.Periodo = docItem.Periodo;
                    docExistente.Observaciones = docItem.Observaciones ?? string.Empty;
                    docExistente.RegistradoPor = asociacion.ActualizadoPor;

                    if (!string.IsNullOrWhiteSpace(docItem.ArchivoNombre) && docItem.ArchivoContenido != null)
                    {
                        docExistente.ArchivoNombre = docItem.ArchivoNombre;
                        docExistente.ArchivoContenido = docItem.ArchivoContenido;
                        docExistente.ArchivoTipoMime = docItem.ArchivoTipoMime ?? "application/pdf";
                    }
                }
                else
                {
                    // Crear nuevo documento radicado
                    var nuevoDoc = new DocumentoAsociacion
                    {
                        NombreAsociacion = nombreAsoc,
                        NitAsociacion = nitCompleto,
                        TipoDocumento = docItem.TipoDocumento,
                        Titulo = string.IsNullOrWhiteSpace(docItem.Titulo) ? docItem.TipoDocumento : docItem.Titulo,
                        Aplica = docItem.Aplica,
                        MotivoNoAplica = docItem.MotivoNoAplica,
                        FechaEmision = docItem.FechaEmision ?? DateTime.Today,
                        FechaVencimiento = docItem.Aplica ? docItem.FechaVencimiento : null,
                        NumeroReferencia = docItem.NumeroReferencia,
                        EntidadEmisora = docItem.EntidadEmisora,
                        Periodo = docItem.Periodo,
                        Observaciones = docItem.Observaciones ?? string.Empty,
                        ArchivoNombre = docItem.ArchivoNombre ?? string.Empty,
                        ArchivoContenido = docItem.ArchivoContenido,
                        ArchivoTipoMime = docItem.ArchivoTipoMime ?? "application/pdf",
                        RegistradoPor = asociacion.ActualizadoPor,
                        FechaRegistro = DateTime.Now
                    };
                    Registrar(nuevoDoc);
                }
            }
        }
    }
}

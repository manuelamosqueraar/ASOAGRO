using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Asoagro.Models;

namespace Asoagro.Services;

public class InMemoryDocumentoAsociacionService : IDocumentoAsociacionService
{
    private static readonly ConcurrentDictionary<Guid, DocumentoAsociacion> Documentos = new();
    private static readonly ConcurrentDictionary<Guid, AsociacionInstitucional> AsociacionesPorId = new();

    public static readonly List<string> CategoriasDocumentalesBase = new()
    {
        "Constitución y estatutos",
        "Inscripción y existencia legal",
        "Representación y gobierno",
        "Información tributaria",
        "Actas y decisiones",
        "Informes, declaraciones y otros soportes institucionales"
    };

    public static readonly List<string> CatalogoTiposDocumentosBase = new()
    {
        "Registro Único Tributario (RUT)",
        "Certificado de Existencia y Representación Legal (Cámara de Comercio)",
        "Acta de Constitución y Aprobación de Estatutos",
        "Estatutos Vigentes de la Asociación",
        "Reforma de Estatutos Aprobada",
        "Acta de Asamblea General y Elección de Junta Directiva",
        "Constancia de Aceptación de Cargos Directivos",
        "Certificación de Nombramiento de Representante Legal",
        "Declaración de Renta e Ingresos Institucionales",
        "Estados Financieros Certificados con Dictamen",
        "Soportes de Actualización Anual Régimen Tributario Especial (RTE - DIAN)",
        "Certificado Fitosanitario / Registro ICA",
        "Certificación de Buenas Prácticas Agrícolas (BPA)",
        "Aval de Asistencia Técnica Agropecuaria",
        "Otro Documento Legal o Institucional"
    };

    public static string InferirCategoriaPorTipo(string tipoDocumento)
    {
        var t = tipoDocumento?.Trim() ?? string.Empty;
        if (t.Contains("Constitución", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Estatuto", StringComparison.OrdinalIgnoreCase))
            return "Constitución y estatutos";

        if (t.Contains("Existencia", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Cámara de Comercio", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("RUES", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Personería", StringComparison.OrdinalIgnoreCase))
            return "Inscripción y existencia legal";

        if (t.Contains("Junta Directiva", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Representación", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Nombramiento", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Aceptación de Cargos", StringComparison.OrdinalIgnoreCase))
            return "Representación y gobierno";

        if (t.Contains("RUT", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Tributario", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Renta", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("RTE", StringComparison.OrdinalIgnoreCase))
            return "Información tributaria";

        if (t.Contains("Acta", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Asamblea", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Resolución", StringComparison.OrdinalIgnoreCase))
            return "Actas y decisiones";

        return "Informes, declaraciones y otros soportes institucionales";
    }

    public static bool DocumentoTieneVencimientoPorDefecto(string tipoDocumento)
    {
        var t = tipoDocumento?.Trim() ?? string.Empty;
        if (t.Contains("RUT", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Estatutos", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Constitución", StringComparison.OrdinalIgnoreCase))
            return false;

        if (t.Contains("Cámara de Comercio", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Existencia y Representación", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("RTE", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Fitosanitario", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("BPA", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    static InMemoryDocumentoAsociacionService()
    {
        // 1. ASOAGRO - Entidad Principal
        var asoagro = new AsociacionInstitucional
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            NombreLegal = "Asociación Agropecuaria de Urabá",
            Sigla = "ASOAGRO",
            Nit = "900543210",
            DigitoVerificacion = "8",
            TipoOrganizacion = "Asociación agropecuaria",
            ActividadProductiva = "Mixto",
            Ambito = "Regional",
            GradoOrganizacion = "Segundo grado (Federación / Regional)",
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
            RepLegalCargo = "Representante Legal",
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
            RutEstado = "Inscrita y activa",
            RutFechaActualizacion = new DateTime(2024, 2, 15),
            ResponsabilidadesTributarias = new() { "Renta Régimen Ordinario", "Retención en la fuente", "Facturación Electrónica" },
            RegimenTributario = "Régimen Ordinario",
            FechaCreacion = DateTime.Now.AddMonths(-12),
            FechaActualizacion = DateTime.Now,
            CreadoPor = "Secretaría FAAUA",
            ActualizadoPor = "Secretaría FAAUA"
        };
        AsociacionesPorId[asoagro.Id] = asoagro;

        // 2. ASOPLÁTANO
        var asoplatano = new AsociacionInstitucional
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            NombreLegal = "Asociación de Productores de Plátano de Urabá",
            Sigla = "ASOPLÁTANO",
            Nit = "901234567",
            DigitoVerificacion = "1",
            TipoOrganizacion = "Asociación campesina",
            ActividadProductiva = "Agrícola",
            Ambito = "Municipal",
            GradoOrganizacion = "Primer grado (Asociación de base)",
            FechaConstitucion = new DateTime(2018, 3, 22),
            Estado = "Activa",
            NumeroRegistro = "00035124-05",
            EntidadInscripcion = "Cámara de Comercio de Urabá",
            FechaInscripcion = new DateTime(2018, 4, 10),
            Departamento = "Antioquia",
            Municipio = "Carepa",
            DireccionPrincipal = "Vereda La Balsa, Sector El Mango",
            CorreoInstitucional = "contacto@asoplatanouraba.org",
            TelefonoContacto = "3129876543",
            SitioWeb = "https://asoplatanouraba.org",
            ContactoNombre = "Jorge Iván Arboleda",
            ContactoCargo = "Presidente de Asociación",
            RepLegalNombre = "Jorge Iván Arboleda",
            RepLegalCargo = "Representante Legal",
            RepLegalDocumento = "1045123987",
            RepLegalPeriodoInicio = new DateTime(2023, 1, 1),
            RepLegalPeriodoFin = new DateTime(2026, 12, 31),
            JuntaPresidente = "Jorge Iván Arboleda",
            JuntaVicepresidente = "Hernán Darío Giraldo",
            JuntaSecretario = "Marta Sofía Palacios",
            JuntaTesorero = "Elkin Darío Moreno",
            JuntaPeriodo = "2023 - 2026",
            FiscalAplica = false,
            FiscalMotivoNoAplica = "Por estatutos de asociación de base y Ley 2219/2022 no se exige revisor fiscal permanente",
            RutEstado = "Inscrita y activa",
            RutFechaActualizacion = new DateTime(2023, 6, 20),
            ResponsabilidadesTributarias = new() { "Renta Régimen Ordinario" },
            RegimenTributario = "Régimen Ordinario",
            FechaCreacion = DateTime.Now.AddMonths(-10),
            FechaActualizacion = DateTime.Now,
            CreadoPor = "Secretaría FAAUA",
            ActualizadoPor = "Secretaría FAAUA"
        };
        AsociacionesPorId[asoplatano.Id] = asoplatano;

        // 3. ASOCACAO
        var asocacao = new AsociacionInstitucional
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            NombreLegal = "Asociación de Cacaoteros de Urabá",
            Sigla = "ASOCACAO",
            Nit = "900876543",
            DigitoVerificacion = "4",
            TipoOrganizacion = "Asociación campesina",
            ActividadProductiva = "Agrícola",
            Ambito = "Municipal",
            GradoOrganizacion = "Primer grado (Asociación de base)",
            FechaConstitucion = new DateTime(2017, 8, 14),
            Estado = "Activa",
            NumeroRegistro = "00031890-04",
            EntidadInscripcion = "Cámara de Comercio de Urabá",
            FechaInscripcion = new DateTime(2017, 9, 5),
            Departamento = "Antioquia",
            Municipio = "Turbo",
            DireccionPrincipal = "Vereda San José, Finca El Porvenir",
            CorreoInstitucional = "info@asocacao-uraba.org",
            TelefonoContacto = "3147890123",
            SitioWeb = "",
            ContactoNombre = "María Elena Mendoza",
            ContactoCargo = "Representante",
            RepLegalNombre = "María Elena Mendoza",
            RepLegalCargo = "Representante Legal",
            RepLegalDocumento = "78901234",
            RepLegalPeriodoInicio = new DateTime(2023, 6, 1),
            RepLegalPeriodoFin = new DateTime(2026, 5, 31),
            JuntaPresidente = "María Elena Mendoza",
            JuntaVicepresidente = "Lucía Morales",
            JuntaSecretario = "Roberto Carlos Serna",
            JuntaTesorero = "Eusebio Martínez",
            JuntaPeriodo = "2023 - 2026",
            FiscalAplica = true,
            FiscalNombre = "Fiscal de Vigilancia Interno",
            FiscalTipo = "Fiscal principal asambleario",
            FiscalPeriodo = "2023 - 2026",
            RutEstado = "Inscrita y activa",
            RutFechaActualizacion = new DateTime(2023, 11, 10),
            ResponsabilidadesTributarias = new() { "Renta Régimen Ordinario" },
            RegimenTributario = "Régimen Ordinario",
            FechaCreacion = DateTime.Now.AddMonths(-8),
            FechaActualizacion = DateTime.Now,
            CreadoPor = "Secretaría FAAUA",
            ActualizadoPor = "Secretaría FAAUA"
        };
        AsociacionesPorId[asocacao.Id] = asocacao;

        // 4. ASOPROMAGRO
        var asopromagro = new AsociacionInstitucional
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            NombreLegal = "Asociación de Productores Agropecuarios de Apartadó",
            Sigla = "ASOPROMAGRO",
            Nit = "901456789",
            DigitoVerificacion = "2",
            TipoOrganizacion = "Asociación agropecuaria",
            ActividadProductiva = "Agrícola",
            Ambito = "Municipal",
            GradoOrganizacion = "Primer grado (Asociación de base)",
            FechaConstitucion = new DateTime(2020, 2, 18),
            Estado = "Activa",
            NumeroRegistro = "00041235-08",
            EntidadInscripcion = "Cámara de Comercio de Urabá",
            FechaInscripcion = new DateTime(2020, 3, 2),
            Departamento = "Antioquia",
            Municipio = "Apartadó",
            DireccionPrincipal = "Vereda Salsipuedes, Kilómetro 4",
            CorreoInstitucional = "asopromagro@gmail.com",
            TelefonoContacto = "3118901234",
            SitioWeb = "",
            ContactoNombre = "Pedro Nel Zapata",
            ContactoCargo = "Representante Legal",
            RepLegalNombre = "Pedro Nel Zapata",
            RepLegalCargo = "Representante Legal",
            RepLegalDocumento = "71234567",
            RepLegalPeriodoInicio = new DateTime(2024, 2, 1),
            RepLegalPeriodoFin = new DateTime(2027, 1, 31),
            JuntaPresidente = "Pedro Nel Zapata",
            JuntaVicepresidente = "Gloria Inés Cárdenas",
            JuntaSecretario = "Fernando Chaverra",
            JuntaTesorero = "Matilde Ramos",
            JuntaPeriodo = "2024 - 2027",
            FiscalAplica = false,
            FiscalMotivoNoAplica = "Asociación en régimen simplificado de base sin junta de vigilancia externa",
            RutEstado = "Inscrita y activa",
            RutFechaActualizacion = new DateTime(2024, 4, 12),
            ResponsabilidadesTributarias = new() { "Renta Régimen Ordinario" },
            RegimenTributario = "Régimen Ordinario",
            FechaCreacion = DateTime.Now.AddMonths(-5),
            FechaActualizacion = DateTime.Now,
            CreadoPor = "Secretaría FAAUA",
            ActualizadoPor = "Secretaría FAAUA"
        };
        AsociacionesPorId[asopromagro.Id] = asopromagro;

        // 5. COAGROURABÁ
        var coagrouraba = new AsociacionInstitucional
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            NombreLegal = "Cooperativa Agropecuaria de Urabá",
            Sigla = "COAGROURABÁ",
            Nit = "890123456",
            DigitoVerificacion = "9",
            TipoOrganizacion = "Cooperativa agropecuaria",
            ActividadProductiva = "Mixto",
            Ambito = "Regional",
            GradoOrganizacion = "Segundo grado (Federación / Regional)",
            FechaConstitucion = new DateTime(2012, 11, 25),
            Estado = "Activa",
            NumeroRegistro = "00019234-01",
            EntidadInscripcion = "Cámara de Comercio de Urabá",
            FechaInscripcion = new DateTime(2012, 12, 10),
            Departamento = "Antioquia",
            Municipio = "Chigorodó",
            DireccionPrincipal = "Calle 102 # 95-18, Centro",
            CorreoInstitucional = "gerencia@coagrouraba.coop",
            TelefonoContacto = "3156789012",
            SitioWeb = "https://coagrouraba.coop",
            ContactoNombre = "Ana Lucía Benítez",
            ContactoCargo = "Gerente General",
            RepLegalNombre = "Ana Lucía Benítez",
            RepLegalCargo = "Gerente y Representante Legal",
            RepLegalDocumento = "43567890",
            RepLegalPeriodoInicio = new DateTime(2023, 1, 1),
            RepLegalPeriodoFin = new DateTime(2026, 12, 31),
            JuntaPresidente = "Bernardo Quintero",
            JuntaVicepresidente = "Clara Luz Ospina",
            JuntaSecretario = "Esteban Hoyos",
            JuntaTesorero = "Alfonso Cuadrado",
            JuntaPeriodo = "2023 - 2026",
            FiscalAplica = true,
            FiscalNombre = "Auditoría Contable Urabá SAS",
            FiscalTipo = "Revisión fiscal estatutaria",
            FiscalPeriodo = "2023 - 2025",
            RutEstado = "Inscrita y activa",
            RutFechaActualizacion = new DateTime(2024, 1, 18),
            ResponsabilidadesTributarias = new() { "Renta Régimen Especial Cooperativo", "Retención en la fuente", "Facturación Electrónica" },
            RegimenTributario = "Régimen Especial",
            FechaCreacion = DateTime.Now.AddMonths(-14),
            FechaActualizacion = DateTime.Now,
            CreadoPor = "Secretaría FAAUA",
            ActualizadoPor = "Secretaría FAAUA"
        };
        AsociacionesPorId[coagrouraba.Id] = coagrouraba;

        // 6. ASOGANADEROS
        var asoganaderos = new AsociacionInstitucional
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            NombreLegal = "Asociación de Ganaderos y Productores de Urabá",
            Sigla = "ASOGANADEROS",
            Nit = "900345678",
            DigitoVerificacion = "5",
            TipoOrganizacion = "Asociación agropecuaria",
            ActividadProductiva = "Pecuario o ganadero",
            Ambito = "Municipal",
            GradoOrganizacion = "Primer grado (Asociación de base)",
            FechaConstitucion = new DateTime(2016, 5, 30),
            Estado = "Activa",
            NumeroRegistro = "00029512-02",
            EntidadInscripcion = "Cámara de Comercio de Urabá",
            FechaInscripcion = new DateTime(2016, 6, 15),
            Departamento = "Antioquia",
            Municipio = "Necoclí",
            DireccionPrincipal = "Carrera 45 # 50-12, Sector Malecón",
            CorreoInstitucional = "asoganaderos.necocli@gmail.com",
            TelefonoContacto = "3134567891",
            SitioWeb = "",
            ContactoNombre = "Héctor Fabio Morales",
            ContactoCargo = "Director Ejecutivo",
            RepLegalNombre = "Héctor Fabio Morales",
            RepLegalCargo = "Representante Legal",
            RepLegalDocumento = "98765432",
            RepLegalPeriodoInicio = new DateTime(2023, 4, 1),
            RepLegalPeriodoFin = new DateTime(2026, 3, 31),
            JuntaPresidente = "Héctor Fabio Morales",
            JuntaVicepresidente = "Camilo Torres",
            JuntaSecretario = "Liliana Cano",
            JuntaTesorero = "Samuel Piedrahita",
            JuntaPeriodo = "2023 - 2026",
            FiscalAplica = true,
            FiscalNombre = "Veeduría Interna de Control",
            FiscalTipo = "Comité de control social",
            FiscalPeriodo = "2023 - 2026",
            RutEstado = "Inscrita y activa",
            RutFechaActualizacion = new DateTime(2023, 9, 5),
            ResponsabilidadesTributarias = new() { "Renta Régimen Ordinario" },
            RegimenTributario = "Régimen Ordinario",
            FechaCreacion = DateTime.Now.AddMonths(-9),
            FechaActualizacion = DateTime.Now,
            CreadoPor = "Secretaría FAAUA",
            ActualizadoPor = "Secretaría FAAUA"
        };
        AsociacionesPorId[asoganaderos.Id] = asoganaderos;

        // Documentos de prueba reales para ASOAGRO
        var hoy = DateTime.Today;
        var doc1 = new DocumentoAsociacion
        {
            Id = Guid.NewGuid(),
            NombreAsociacion = "ASOAGRO - Asociación Agropecuaria de Urabá",
            NitAsociacion = "900543210-8",
            TipoDocumento = "Certificado de Existencia y Representación Legal (Cámara de Comercio)",
            Titulo = "Cámara de Comercio de Urabá",
            FechaEmision = hoy.AddMonths(-11),
            TieneVencimiento = true,
            FechaVencimiento = hoy.AddDays(5),
            Aplica = true,
            RegistradoPor = "Secretaría FAAUA",
            FechaRegistro = hoy.AddMonths(-11)
        };
        var doc2 = new DocumentoAsociacion
        {
            Id = Guid.NewGuid(),
            NombreAsociacion = "ASOAGRO - Asociación Agropecuaria de Urabá",
            NitAsociacion = "900543210-8",
            TipoDocumento = "Registro Único Tributario (RUT)",
            Titulo = "RUT Actualizado DIAN",
            FechaEmision = hoy.AddMonths(-6),
            TieneVencimiento = false,
            FechaVencimiento = null, // El RUT tiene vigencia indefinida según DIAN
            Aplica = true,
            RegistradoPor = "Secretaría FAAUA",
            FechaRegistro = hoy.AddMonths(-6)
        };
        var doc3 = new DocumentoAsociacion
        {
            Id = Guid.NewGuid(),
            NombreAsociacion = "ASOAGRO - Asociación Agropecuaria de Urabá",
            NitAsociacion = "900543210-8",
            TipoDocumento = "Declaración de Renta e Ingresos Institucionales",
            Titulo = "Declaración Anual de Ingresos",
            FechaEmision = hoy.AddMonths(-3),
            TieneVencimiento = true,
            FechaVencimiento = hoy.AddDays(25),
            Aplica = true,
            RegistradoPor = "Secretaría FAAUA",
            FechaRegistro = hoy.AddMonths(-3)
        };
        var doc4 = new DocumentoAsociacion
        {
            Id = Guid.NewGuid(),
            NombreAsociacion = "ASOAGRO - Asociación Agropecuaria de Urabá",
            NitAsociacion = "900543210-8",
            TipoDocumento = "Póliza de Responsabilidad y Cumplimiento",
            Titulo = "Póliza Seguros del Estado",
            FechaEmision = hoy.AddMonths(-5),
            TieneVencimiento = true,
            FechaVencimiento = hoy.AddDays(12),
            Aplica = true,
            RegistradoPor = "Secretaría FAAUA",
            FechaRegistro = hoy.AddMonths(-5)
        };
        Documentos[doc1.Id] = doc1;
        Documentos[doc2.Id] = doc2;
        Documentos[doc3.Id] = doc3;
        Documentos[doc4.Id] = doc4;
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
            // Solo generar alerta si el documento tiene vencimiento aplicable y fecha registrada
            if (!doc.TieneVencimiento || !doc.FechaVencimiento.HasValue) continue;

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
        return AsociacionesPorId.Values
            .Select(a => string.IsNullOrWhiteSpace(a.Sigla) ? a.NombreLegal.Trim() : $"{a.Sigla.Trim()} - {a.NombreLegal.Trim()}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(a => a)
            .ToList();
    }

    public IReadOnlyList<string> ObtenerCatalogoTiposDocumentos()
    {
        return CatalogoTiposDocumentosBase;
    }

    public IReadOnlyList<AsociacionInstitucional> ObtenerAsociacionesInstitucionales()
    {
        return AsociacionesPorId.Values
            .OrderBy(a => a.NombreLegal)
            .ToList();
    }

    public AsociacionInstitucional? ObtenerAsociacionPorId(Guid id)
    {
        AsociacionesPorId.TryGetValue(id, out var asoc);
        return asoc;
    }

    public AsociacionInstitucional? ObtenerAsociacionInstitucional(string nombreAsociacion)
    {
        if (string.IsNullOrWhiteSpace(nombreAsociacion)) return null;

        var buscado = nombreAsociacion.Trim();

        return AsociacionesPorId.Values.FirstOrDefault(a =>
            a.Id.ToString().Equals(buscado, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a.NombreLegal, buscado, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a.Sigla, buscado, StringComparison.OrdinalIgnoreCase) ||
            string.Equals($"{a.Sigla} - {a.NombreLegal}", buscado, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(a.Nit) && a.Nit.Equals(buscado, StringComparison.OrdinalIgnoreCase)) ||
            buscado.Contains(a.NombreLegal, StringComparison.OrdinalIgnoreCase) ||
            buscado.Contains(a.Sigla, StringComparison.OrdinalIgnoreCase));
    }

    public bool ExisteNit(string nit, Guid? ignorarId = null)
    {
        if (string.IsNullOrWhiteSpace(nit)) return false;
        var nitLimpio = Regex.Replace(nit, "[^0-9]", "");
        if (string.IsNullOrWhiteSpace(nitLimpio)) return false;

        return AsociacionesPorId.Values.Any(a =>
            a.Id != ignorarId &&
            !string.IsNullOrWhiteSpace(a.Nit) &&
            Regex.Replace(a.Nit, "[^0-9]", "") == nitLimpio);
    }

    public bool CambiarEstadoAsociacion(Guid id, string nuevoEstado, string motivo, string usuario)
    {
        if (AsociacionesPorId.TryGetValue(id, out var asoc))
        {
            asoc.Estado = nuevoEstado;
            asoc.MotivoCambioEstado = motivo;
            asoc.FechaCambioEstado = DateTime.Now;
            asoc.FechaActualizacion = DateTime.Now;
            asoc.ActualizadoPor = usuario;
            return true;
        }
        return false;
    }

    public bool GuardarAsociacionInstitucional(AsociacionInstitucional asociacion, IEnumerable<DocumentoItemInstitucional>? documentos = null)
    {
        if (asociacion.Id == Guid.Empty)
        {
            asociacion.Id = Guid.NewGuid();
            asociacion.FechaCreacion = DateTime.Now;
        }

        // Validación de no duplicar NIT si ya existe en otra entidad distinta
        if (!string.IsNullOrWhiteSpace(asociacion.Nit) && ExisteNit(asociacion.Nit, asociacion.Id))
        {
            return false;
        }

        asociacion.FechaActualizacion = DateTime.Now;
        AsociacionesPorId[asociacion.Id] = asociacion;

        var clave = string.IsNullOrWhiteSpace(asociacion.Sigla)
            ? asociacion.NombreLegal.Trim()
            : $"{asociacion.Sigla.Trim()} - {asociacion.NombreLegal.Trim()}";

        // Si se proveen documentos institucionales, sincronizarlos con el expediente
        if (documentos != null)
        {
            var nombreAsoc = clave;
            var nitCompleto = string.IsNullOrWhiteSpace(asociacion.DigitoVerificacion)
                ? asociacion.Nit
                : $"{asociacion.Nit}-{asociacion.DigitoVerificacion}";

            foreach (var docItem in documentos)
            {
                var docExistente = Documentos.Values.FirstOrDefault(d =>
                    string.Equals(d.NombreAsociacion.Trim(), nombreAsoc.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(d.TipoDocumento.Trim(), docItem.TipoDocumento.Trim(), StringComparison.OrdinalIgnoreCase));

                var cat = !string.IsNullOrWhiteSpace(docItem.CategoriaSeccion)
                    ? docItem.CategoriaSeccion
                    : InferirCategoriaPorTipo(docItem.TipoDocumento);

                if (docExistente != null)
                {
                    docExistente.AsociacionId = asociacion.Id;
                    docExistente.NitAsociacion = nitCompleto;
                    docExistente.Categoria = cat;
                    docExistente.Titulo = string.IsNullOrWhiteSpace(docItem.Titulo) ? docItem.TipoDocumento : docItem.Titulo;
                    docExistente.Aplica = docItem.Aplica;
                    docExistente.MotivoNoAplica = docItem.MotivoNoAplica;
                    docExistente.FechaEmision = docItem.FechaEmision ?? DateTime.Today;
                    docExistente.TieneVencimiento = docItem.TieneVencimiento;
                    docExistente.FechaVencimiento = (docItem.Aplica && docItem.TieneVencimiento) ? docItem.FechaVencimiento : null;
                    docExistente.NumeroReferencia = docItem.NumeroReferencia;
                    docExistente.EntidadEmisora = docItem.EntidadEmisora;
                    docExistente.Periodo = docItem.Periodo;
                    docExistente.Observaciones = docItem.Observaciones ?? string.Empty;
                    docExistente.RegistradoPor = asociacion.ActualizadoPor;
                    docExistente.NivelRequerimiento = docItem.NivelRequerimiento;

                    if (!string.IsNullOrWhiteSpace(docItem.ArchivoNombre) && docItem.ArchivoContenido != null)
                    {
                        docExistente.ArchivoNombre = docItem.ArchivoNombre;
                        docExistente.ArchivoContenido = docItem.ArchivoContenido;
                        docExistente.ArchivoTipoMime = docItem.ArchivoTipoMime ?? "application/pdf";
                        docExistente.TamanoBytes = docItem.TamanoBytes > 0 ? docItem.TamanoBytes : docItem.ArchivoContenido.Length;
                        docExistente.EstadoRevision = "Aprobado";
                    }
                }
                else
                {
                    var nuevoDoc = new DocumentoAsociacion
                    {
                        AsociacionId = asociacion.Id,
                        NombreAsociacion = nombreAsoc,
                        NitAsociacion = nitCompleto,
                        Categoria = cat,
                        TipoDocumento = docItem.TipoDocumento,
                        Titulo = string.IsNullOrWhiteSpace(docItem.Titulo) ? docItem.TipoDocumento : docItem.Titulo,
                        Aplica = docItem.Aplica,
                        MotivoNoAplica = docItem.MotivoNoAplica,
                        FechaEmision = docItem.FechaEmision ?? DateTime.Today,
                        TieneVencimiento = docItem.TieneVencimiento,
                        FechaVencimiento = (docItem.Aplica && docItem.TieneVencimiento) ? docItem.FechaVencimiento : null,
                        NumeroReferencia = docItem.NumeroReferencia,
                        EntidadEmisora = docItem.EntidadEmisora,
                        Periodo = docItem.Periodo,
                        Observaciones = docItem.Observaciones ?? string.Empty,
                        ArchivoNombre = docItem.ArchivoNombre ?? string.Empty,
                        ArchivoContenido = docItem.ArchivoContenido,
                        ArchivoTipoMime = docItem.ArchivoTipoMime ?? "application/pdf",
                        TamanoBytes = docItem.TamanoBytes > 0 ? docItem.TamanoBytes : (docItem.ArchivoContenido?.Length ?? 0),
                        RegistradoPor = asociacion.ActualizadoPor,
                        FechaRegistro = DateTime.Now,
                        NivelRequerimiento = docItem.NivelRequerimiento,
                        EstadoRevision = string.IsNullOrWhiteSpace(docItem.ArchivoNombre) ? "Pendiente de revisión" : "Aprobado"
                    };
                    Registrar(nuevoDoc);
                }
            }
        }

        return true;
    }

    public bool ExisteNombre(string nombre, Guid? ignorarId = null)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return false;
        var limpio = nombre.Trim();
        return AsociacionesPorId.Values.Any(a =>
            a.Id != ignorarId &&
            string.Equals(a.NombreLegal.Trim(), limpio, StringComparison.OrdinalIgnoreCase));
    }

    public bool AgregarNuevaVersion(
        Guid documentoId,
        string archivoNombre,
        byte[] archivoContenido,
        string archivoTipoMime,
        long tamanoBytes,
        string observaciones,
        string usuario,
        DateTime? fechaEmision = null,
        DateTime? fechaVencimiento = null)
    {
        if (!Documentos.TryGetValue(documentoId, out var doc))
            return false;

        // Si ya tenía archivo previo, archivar la versión anterior en el historial
        if (!string.IsNullOrWhiteSpace(doc.ArchivoNombre) && doc.ArchivoContenido != null)
        {
            doc.HistorialVersiones.Add(new DocumentoVersionHistorial
            {
                Id = Guid.NewGuid(),
                NumeroVersion = doc.VersionActual,
                ArchivoNombre = doc.ArchivoNombre,
                ArchivoContenido = doc.ArchivoContenido,
                ArchivoTipoMime = doc.ArchivoTipoMime,
                TamanoBytes = doc.TamanoBytes,
                FechaCarga = doc.FechaRegistro,
                SubidoPor = doc.RegistradoPor,
                Observaciones = doc.Observaciones,
                EstadoVersion = "Reemplazada",
                FechaEmision = doc.FechaEmision,
                FechaVencimiento = doc.FechaVencimiento
            });
            doc.VersionActual++;
        }

        doc.ArchivoNombre = archivoNombre;
        doc.ArchivoContenido = archivoContenido;
        doc.ArchivoTipoMime = archivoTipoMime;
        doc.TamanoBytes = tamanoBytes;
        doc.FechaRegistro = DateTime.Now;
        doc.RegistradoPor = usuario;
        doc.EstadoRevision = "Aprobado";

        if (!string.IsNullOrWhiteSpace(observaciones))
        {
            doc.Observaciones = observaciones;
        }

        if (fechaEmision.HasValue)
        {
            doc.FechaEmision = fechaEmision.Value;
        }

        if (doc.TieneVencimiento && fechaVencimiento.HasValue)
        {
            doc.FechaVencimiento = fechaVencimiento.Value;
        }

        return true;
    }

    public bool ActualizarRevisionDocumento(Guid documentoId, string nuevoEstadoRevision, string observaciones, string usuario)
    {
        if (!Documentos.TryGetValue(documentoId, out var doc))
            return false;

        doc.EstadoRevision = nuevoEstadoRevision;
        if (!string.IsNullOrWhiteSpace(observaciones))
        {
            doc.Observaciones = $"{doc.Observaciones} (Revisión por {usuario} [{nuevoEstadoRevision}] el {DateTime.Now:dd/MM/yyyy}: {observaciones})".Trim();
        }
        return true;
    }

    public IReadOnlyList<DocumentoAsociacion> ObtenerDocumentosPorCategoria(string nombreAsociacion, string categoria)
    {
        var docs = ObtenerPorAsociacion(nombreAsociacion);
        if (string.IsNullOrWhiteSpace(categoria) || categoria.Equals("Todas", StringComparison.OrdinalIgnoreCase))
            return docs;

        return docs.Where(d => string.Equals(d.Categoria, categoria, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public IReadOnlyList<string> ObtenerCategoriasDocumentales()
    {
        return CategoriasDocumentalesBase;
    }

    public bool RegistrarDocumentoEnExpediente(DocumentoAsociacion documento, string usuario)
    {
        if (documento.Id == Guid.Empty)
        {
            documento.Id = Guid.NewGuid();
        }
        documento.RegistradoPor = usuario;
        documento.FechaRegistro = DateTime.Now;
        if (string.IsNullOrWhiteSpace(documento.Categoria))
        {
            documento.Categoria = InferirCategoriaPorTipo(documento.TipoDocumento);
        }
        Documentos[documento.Id] = documento;
        return true;
    }
}

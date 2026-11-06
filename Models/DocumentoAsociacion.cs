using System;
using System.Collections.Generic;

namespace Asoagro.Models;

public class DocumentoAsociacion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string NombreAsociacion { get; set; } = string.Empty;

    public string NitAsociacion { get; set; } = string.Empty;

    public string TipoDocumento { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public DateTime FechaEmision { get; set; } = DateTime.Today;

    public DateTime? FechaVencimiento { get; set; }

    public string Observaciones { get; set; } = string.Empty;

    public string ArchivoNombre { get; set; } = string.Empty;

    public byte[]? ArchivoContenido { get; set; }

    public string ArchivoTipoMime { get; set; } = "application/pdf";

    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public string RegistradoPor { get; set; } = "Secretaría";

    public bool Aplica { get; set; } = true;

    public string MotivoNoAplica { get; set; } = string.Empty;

    public string? NumeroReferencia { get; set; }

    public string? EntidadEmisora { get; set; }

    public string? Periodo { get; set; }

    public int? DiasRestantes => FechaVencimiento.HasValue
        ? (FechaVencimiento.Value.Date - DateTime.Today).Days
        : null;

    public string EstadoCalculado
    {
        get
        {
            if (!Aplica) return "No aplica";
            if (!FechaVencimiento.HasValue) return "Vigente";
            var dias = DiasRestantes!.Value;
            if (dias < 0) return "Vencido";
            if (dias <= 30) return "Por vencer";
            return "Vigente";
        }
    }
}

public class DocumentoItemInstitucional
{
    public string Clave { get; set; } = string.Empty;
    public string CategoriaSeccion { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    
    // Estado del requisito documental: "Obligatorio", "Aplica", "Pendiente", "No aplica"
    public string EstadoRequisito { get; set; } = "Obligatorio"; 

    public bool Aplica => EstadoRequisito != "No aplica";
    public string MotivoNoAplica { get; set; } = string.Empty;
    public DateTime? FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? NumeroReferencia { get; set; }
    public string? EntidadEmisora { get; set; }
    public string? Periodo { get; set; }
    public string? Observaciones { get; set; }
    public string? ArchivoNombre { get; set; }
    public byte[]? ArchivoContenido { get; set; }
    public string? ArchivoTipoMime { get; set; } = "application/pdf";
    public long TamanoBytes { get; set; }
    public string NivelRequerimiento { get; set; } = "Obligatorio";
}

public class AsociacionInstitucional
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Paso 1: Identificación
    public string NombreLegal { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public string TipoOrganizacion { get; set; } = "Asociación campesina";
    public string Ambito { get; set; } = "Municipal";
    public string GradoOrganizacion { get; set; } = "Primer grado (Asociación de base)";
    public string Nit { get; set; } = string.Empty;
    public string DigitoVerificacion { get; set; } = string.Empty;
    public DateTime? FechaConstitucion { get; set; }
    public string Estado { get; set; } = "Activa";
    public string ExcepcionesIdentificacion { get; set; } = string.Empty;

    // Paso 2: Domicilio y Contacto institucional
    public string Departamento { get; set; } = "Antioquia";
    public string Municipio { get; set; } = "Apartadó";
    public string DireccionPrincipal { get; set; } = string.Empty;
    public string CorreoInstitucional { get; set; } = string.Empty;
    public string TelefonoContacto { get; set; } = string.Empty;
    public bool DireccionNotificacionesEsMisma { get; set; } = true;
    public bool NotificacionesDiferentes => !DireccionNotificacionesEsMisma;
    public string DireccionNotificaciones { get; set; } = string.Empty;
    public string SitioWeb { get; set; } = string.Empty;
    public string ContactoNombre { get; set; } = string.Empty;
    public string ContactoCargo { get; set; } = string.Empty;

    // Paso 3: Registro Legal y Constitución (Ley 2219 de 2022)
    public string EntidadInscripcion { get; set; } = string.Empty;
    public string NumeroRegistro { get; set; } = string.Empty;
    public DateTime? FechaInscripcion { get; set; }
    public DateTime? FechaRenovacionMatricula { get; set; }
    public string CamaraComercio { get; set; } = string.Empty;
    public string NumeroRues { get; set; } = string.Empty;
    public string AutoridadVigilancia { get; set; } = string.Empty;

    public DateTime? FechaAsambleaConstitucion { get; set; }
    public bool AprobacionEstatutos { get; set; } = true;
    public bool CuotasInicialesAcordadas { get; set; } = true;
    public string DetalleCuotasIniciales { get; set; } = string.Empty;
    public string ReglasAutogobierno { get; set; } = string.Empty;

    // Paso 4: Representación y Gobierno institucional
    public string RepLegalNombre { get; set; } = string.Empty;
    public string RepLegalCargo { get; set; } = "Representante Legal";
    public string RepLegalDocumento { get; set; } = string.Empty;
    public DateTime? RepLegalFechaNombramiento { get; set; }
    public DateTime? RepLegalPeriodoInicio { get; set; }
    public DateTime? RepLegalPeriodoFin { get; set; }
    public DateTime? RepLegalFechaInscripcion { get; set; }

    public string JuntaPresidente { get; set; } = string.Empty;
    public string JuntaVicepresidente { get; set; } = string.Empty;
    public string JuntaSecretario { get; set; } = string.Empty;
    public string JuntaTesorero { get; set; } = string.Empty;
    public string JuntaPeriodo { get; set; } = string.Empty;

    public bool FiscalAplica { get; set; } = true;
    public string FiscalMotivoNoAplica { get; set; } = string.Empty;
    public string FiscalNombre { get; set; } = string.Empty;
    public string FiscalTipo { get; set; } = "Fiscal principal";
    public string FiscalPeriodo { get; set; } = string.Empty;

    // Paso 5: Identificación tributaria
    public string RutEstado { get; set; } = "Inscrita y activa"; // "Inscrita y activa", "Pendiente / En trámite", "No aplica"
    public string RutEstadoMotivo { get; set; } = string.Empty;
    public DateTime? RutFechaActualizacion { get; set; }
    public List<string> ResponsabilidadesTributarias { get; set; } = new();
    public string RegimenTributario { get; set; } = "Régimen Ordinario";
    public string RegimenTributarioOtro { get; set; } = string.Empty;
    public bool RteConfirmado { get; set; } = false;
    public string RteEstadoDian { get; set; } = "Permanencia anual confirmada";
    public DateTime? RteUltimaActualizacion { get; set; }
    public DateTime? RteFechaLimiteRenovacion { get; set; }

    // Auditoría
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    public string CreadoPor { get; set; } = "Secretaría FAAUA";
    public string ActualizadoPor { get; set; } = "Secretaría FAAUA";
}

public class AlertaDocumentoAsociacion
{
    public Guid DocumentoId { get; set; }
    public string NombreAsociacion { get; set; } = string.Empty;
    public string NitAsociacion { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public int DiasRestantes { get; set; }
    public string NivelAlerta { get; set; } = string.Empty;
    public string ArchivoNombre { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
}

public class ResumenExpedienteAsociacion
{
    public string NombreAsociacion { get; set; } = string.Empty;
    public string NitAsociacion { get; set; } = string.Empty;
    public int TotalRegistrados { get; set; }
    public int Vigentes { get; set; }
    public int PorVencer { get; set; }
    public int Vencidos { get; set; }
    public int NoAplicables { get; set; }
    public List<string> DocumentosFaltantes { get; set; } = new();
    public List<DocumentoAsociacion> Documentos { get; set; } = new();
}

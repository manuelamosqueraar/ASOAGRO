using System.Security.Cryptography;

namespace Asoagro.Models;

// Estados posibles de un documento institucional
public static class EstadosDocumento
{
    public const string Vigente = "Vigente";
    public const string EnRevision = "En revisión";
    public const string Derogado = "Derogado";
    public const string Anulado = "Anulado";
}

// Acta, estatuto, reglamento o resolución de la asociación
public class DocumentoInstitucional
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Titulo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = EstadosDocumento.Vigente;
    public DateTime Fecha { get; set; } = DateTime.Today;
    public DateTime? FechaVencimiento { get; set; }
    public string NumeroReferencia { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string CedulaAsociado { get; set; } = string.Empty;
    public string NombreAsociado { get; set; } = string.Empty;

    // Archivo digital. Null = registro sin archivo adjunto
    public ArchivoAdjunto? Archivo { get; set; }

    // Versión del archivo. 0 = sin archivo; cada reemplazo suma 1
    public int Version { get; set; }

    public DateTime FechaCreacion { get; init; } = DateTime.Now;
    public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    public string CreadoPor { get; init; } = string.Empty;
    public string ActualizadoPor { get; set; } = string.Empty;

    // Código corto para identificar el registro. Ejemplo: ACT-3F2A9C1B
    public string Codigo => "ACT-" + Id.ToString("N")[..8].ToUpperInvariant();

    // Compromisos derivados del acta o acuerdo institucional
    public List<CompromisoActa> Compromisos { get; set; } = new();
}

public class CompromisoActa
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Descripcion { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public DateTime FechaLimite { get; set; } = DateTime.Today.AddDays(15);
    public string Estado { get; set; } = "Pendiente"; // "Pendiente", "En curso", "Cumplido", "Atrasado"
    public int AvancePorcentaje { get; set; } = 0;
    public string? SoporteNombre { get; set; }
    public DateTime? FechaCumplimiento { get; set; }
    public string? AsociacionNombre { get; set; } = "ASOAGRO - Asociación Agropecuaria de Urabá";
    public string? Observaciones { get; set; }
}

// Archivo con su huella SHA-256, que permite comprobar que no fue alterado
public record ArchivoAdjunto(string Nombre, string TipoMime, byte[] Contenido, string Sha256)
{
    public long Tamano => Contenido.LongLength;

    public bool Verificar() => string.Equals(
        Convert.ToHexString(SHA256.HashData(Contenido)),
        Sha256,
        StringComparison.OrdinalIgnoreCase);

    public static ArchivoAdjunto Crear(string nombre, string tipoMime, byte[] contenido) =>
        new(nombre, tipoMime, contenido, Convert.ToHexString(SHA256.HashData(contenido)));
}

// Datos que llegan desde el formulario
public record DatosDocumento(
    string Titulo,
    string Tipo,
    string Estado,
    DateTime Fecha,
    string NumeroReferencia,
    string Descripcion,
    string CedulaAsociado,
    string NombreAsociado,
    DateTime? FechaVencimiento = null);

// Resultado de una edición: si se guardó y qué cambió
public record ResultadoActualizacion(bool Exitoso, string Mensaje, IReadOnlyList<string> Cambios);

// Una línea del historial de cambios
public record EntradaAuditoria(DateTime Fecha, string Usuario, string Accion, string Detalle);

// Elemento consolidado para la vista de historial de actividad
public record ItemHistorialActividad(
    Guid DocumentoId,
    string CodigoDocumento,
    string TituloDocumento,
    string TipoDocumento,
    DateTime Fecha,
    string Usuario,
    string Accion,
    string Detalle);

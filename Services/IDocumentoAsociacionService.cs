using Asoagro.Models;

namespace Asoagro.Services;

public interface IDocumentoAsociacionService
{
    IReadOnlyList<DocumentoAsociacion> ObtenerTodos();
    IReadOnlyList<DocumentoAsociacion> ObtenerPorAsociacion(string nombreAsociacion);
    DocumentoAsociacion? ObtenerPorId(Guid id);
    void Registrar(DocumentoAsociacion doc);
    bool ActualizarVencimientoYArchivo(Guid id, DateTime? nuevaFechaVencimiento, string? nuevoArchivoNombre, byte[]? nuevoArchivoContenido, string? nuevasObservaciones, string usuario);
    bool Eliminar(Guid id);

    IReadOnlyList<AlertaDocumentoAsociacion> ObtenerAlertas(DateTime fechaReferencia);
    ResumenExpedienteAsociacion ObtenerExpediente(string nombreAsociacion);
    IReadOnlyList<string> ObtenerAsociaciones();
    IReadOnlyList<string> ObtenerCatalogoTiposDocumentos();

    // Gestión institucional completa de la asociación como entidad
    IReadOnlyList<AsociacionInstitucional> ObtenerAsociacionesInstitucionales();
    AsociacionInstitucional? ObtenerAsociacionInstitucional(string nombreAsociacion);
    AsociacionInstitucional? ObtenerAsociacionPorId(Guid id);
    bool GuardarAsociacionInstitucional(AsociacionInstitucional asociacion, IEnumerable<DocumentoItemInstitucional>? documentos = null);
    bool ExisteNit(string nit, Guid? ignorarId = null);
    bool ExisteNombre(string nombre, Guid? ignorarId = null);
    bool CambiarEstadoAsociacion(Guid id, string nuevoEstado, string motivo, string usuario);

    // Gestión avanzada del expediente documental y versiones
    bool AgregarNuevaVersion(Guid documentoId, string archivoNombre, byte[] archivoContenido, string archivoTipoMime, long tamanoBytes, string observaciones, string usuario, DateTime? fechaEmision = null, DateTime? fechaVencimiento = null);
    bool ActualizarRevisionDocumento(Guid documentoId, string nuevoEstadoRevision, string observaciones, string usuario);
    IReadOnlyList<DocumentoAsociacion> ObtenerDocumentosPorCategoria(string nombreAsociacion, string categoria);
    IReadOnlyList<string> ObtenerCategoriasDocumentales();
    bool RegistrarDocumentoEnExpediente(DocumentoAsociacion documento, string usuario);
}

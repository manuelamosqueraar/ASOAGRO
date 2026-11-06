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
    void GuardarAsociacionInstitucional(AsociacionInstitucional asociacion, IEnumerable<DocumentoItemInstitucional>? documentos = null);
}

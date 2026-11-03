using Asoagro.Models;

namespace Asoagro.Services;

public interface IDocumentoInstitucionalService
{
    // Todos los documentos, del más reciente al más antiguo
    IReadOnlyList<DocumentoInstitucional> ObtenerTodos();

    DocumentoInstitucional? ObtenerPorId(Guid id);

    // Historial del documento, del más reciente al más antiguo
    IReadOnlyList<EntradaAuditoria> ObtenerHistorial(Guid id);

    DocumentoInstitucional Crear(DatosDocumento datos, ArchivoAdjunto? archivo, string usuario);

    // Cambia los datos y, si se envía, reemplaza el archivo. Un registro anulado no se puede editar.
    ResultadoActualizacion Actualizar(Guid id, DatosDocumento datos, ArchivoAdjunto? nuevoArchivo, string usuario);

    // Anula el registro sin borrarlo. Devuelve false si no existe o ya estaba anulado.
    bool Anular(Guid id, string motivo, string usuario);

    // Devuelve el registro a Vigente. Devuelve false si no estaba anulado.
    bool Restaurar(Guid id, string usuario);

    // Deja constancia en el historial de que se descargó el archivo
    void RegistrarDescarga(Guid id, string usuario);
}

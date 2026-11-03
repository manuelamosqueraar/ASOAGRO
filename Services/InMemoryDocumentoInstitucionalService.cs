using Asoagro.Models;

namespace Asoagro.Services;

// Guarda los documentos en memoria mientras la app está abierta.
// Cuando se conecte la base de datos, se creará PostgresDocumentoInstitucionalService con la misma interfaz.
public class InMemoryDocumentoInstitucionalService : IDocumentoInstitucionalService
{
    private readonly object _sync = new();
    private readonly List<DocumentoInstitucional> _documentos = new();
    private readonly Dictionary<Guid, List<EntradaAuditoria>> _historial = new();

    public InMemoryDocumentoInstitucionalService()
    {
        SembrarEjemplos();
    }

    public IReadOnlyList<DocumentoInstitucional> ObtenerTodos()
    {
        lock (_sync)
        {
            return _documentos
                .OrderByDescending(d => d.Fecha)
                .ThenByDescending(d => d.FechaCreacion)
                .ToList();
        }
    }

    public DocumentoInstitucional? ObtenerPorId(Guid id)
    {
        lock (_sync)
        {
            return _documentos.FirstOrDefault(d => d.Id == id);
        }
    }

    public IReadOnlyList<EntradaAuditoria> ObtenerHistorial(Guid id)
    {
        lock (_sync)
        {
            if (!_historial.TryGetValue(id, out var entradas))
            {
                return new List<EntradaAuditoria>();
            }

            // Las más recientes primero
            return entradas.AsEnumerable().Reverse().ToList();
        }
    }

    public DocumentoInstitucional Crear(DatosDocumento datos, ArchivoAdjunto? archivo, string usuario)
    {
        lock (_sync)
        {
            var documento = new DocumentoInstitucional
            {
                Titulo = datos.Titulo.Trim(),
                Tipo = datos.Tipo,
                Estado = datos.Estado,
                Fecha = datos.Fecha.Date,
                FechaVencimiento = datos.FechaVencimiento?.Date,
                NumeroReferencia = datos.NumeroReferencia.Trim(),
                Descripcion = datos.Descripcion.Trim(),
                CedulaAsociado = datos.CedulaAsociado.Trim(),
                NombreAsociado = datos.NombreAsociado.Trim(),
                Archivo = archivo,
                Version = archivo is null ? 0 : 1,
                CreadoPor = usuario,
                ActualizadoPor = usuario
            };

            _documentos.Add(documento);
            Registrar(documento.Id, usuario, "Registro creado",
                archivo is null ? "Sin archivo adjunto" : $"Archivo adjunto: {archivo.Nombre} (versión 1)");

            return documento;
        }
    }

    public ResultadoActualizacion Actualizar(Guid id, DatosDocumento datos, ArchivoAdjunto? nuevoArchivo, string usuario)
    {
        lock (_sync)
        {
            var documento = _documentos.FirstOrDefault(d => d.Id == id);
            if (documento is null)
            {
                return new ResultadoActualizacion(false, "El documento ya no existe.", Array.Empty<string>());
            }

            if (documento.Estado == EstadosDocumento.Anulado)
            {
                return new ResultadoActualizacion(false,
                    "Un registro anulado no se puede editar. Restáuralo primero.", Array.Empty<string>());
            }

            var titulo = datos.Titulo.Trim();
            var referencia = datos.NumeroReferencia.Trim();
            var descripcion = datos.Descripcion.Trim();
            var fecha = datos.Fecha.Date;
            var fechaVencimiento = datos.FechaVencimiento?.Date;
            var cambios = new List<string>();

            if (documento.Titulo != titulo)
                cambios.Add($"Título: «{documento.Titulo}» → «{titulo}»");
            if (documento.Tipo != datos.Tipo)
                cambios.Add($"Tipo: {documento.Tipo} → {datos.Tipo}");
            if (documento.Estado != datos.Estado)
                cambios.Add($"Estado: {documento.Estado} → {datos.Estado}");
            if (documento.Fecha != fecha)
                cambios.Add($"Fecha: {documento.Fecha:dd/MM/yyyy} → {fecha:dd/MM/yyyy}");
            if (documento.FechaVencimiento != fechaVencimiento)
                cambios.Add($"Fecha de vencimiento: {FormatoFecha(documento.FechaVencimiento)} → {FormatoFecha(fechaVencimiento)}");
            if (documento.NumeroReferencia != referencia)
                cambios.Add($"Referencia: {Valor(documento.NumeroReferencia)} → {Valor(referencia)}");
            if (documento.Descripcion != descripcion)
                cambios.Add("Descripción actualizada");
            if (documento.CedulaAsociado != datos.CedulaAsociado.Trim()
                || documento.NombreAsociado != datos.NombreAsociado.Trim())
                cambios.Add($"Asociado relacionado: {Valor(documento.NombreAsociado)} → {Valor(datos.NombreAsociado.Trim())}");
            if (nuevoArchivo is not null)
                cambios.Add(documento.Archivo is null
                    ? $"Archivo adjunto: {nuevoArchivo.Nombre}"
                    : $"Archivo reemplazado: {documento.Archivo.Nombre} → {nuevoArchivo.Nombre}");

            if (cambios.Count == 0)
            {
                return new ResultadoActualizacion(true, "No hay cambios que guardar.", cambios);
            }

            documento.Titulo = titulo;
            documento.Tipo = datos.Tipo;
            documento.Estado = datos.Estado;
            documento.Fecha = fecha;
            documento.FechaVencimiento = fechaVencimiento;
            documento.NumeroReferencia = referencia;
            documento.Descripcion = descripcion;
            documento.CedulaAsociado = datos.CedulaAsociado.Trim();
            documento.NombreAsociado = datos.NombreAsociado.Trim();

            if (nuevoArchivo is not null)
            {
                documento.Archivo = nuevoArchivo;
                documento.Version++;
            }

            documento.FechaActualizacion = DateTime.Now;
            documento.ActualizadoPor = usuario;

            Registrar(id, usuario, "Cambios guardados", string.Join(" · ", cambios));
            return new ResultadoActualizacion(true, "Cambios guardados.", cambios);
        }
    }

    public bool Anular(Guid id, string motivo, string usuario)
    {
        lock (_sync)
        {
            var documento = _documentos.FirstOrDefault(d => d.Id == id);
            if (documento is null || documento.Estado == EstadosDocumento.Anulado)
            {
                return false;
            }

            var estadoAnterior = documento.Estado;
            documento.Estado = EstadosDocumento.Anulado;
            documento.FechaActualizacion = DateTime.Now;
            documento.ActualizadoPor = usuario;

            Registrar(id, usuario, "Registro anulado",
                $"Estado: {estadoAnterior} → Anulado. Motivo: {motivo.Trim()}");
            return true;
        }
    }

    public bool Restaurar(Guid id, string usuario)
    {
        lock (_sync)
        {
            var documento = _documentos.FirstOrDefault(d => d.Id == id);
            if (documento is null || documento.Estado != EstadosDocumento.Anulado)
            {
                return false;
            }

            documento.Estado = EstadosDocumento.Vigente;
            documento.FechaActualizacion = DateTime.Now;
            documento.ActualizadoPor = usuario;

            Registrar(id, usuario, "Registro restaurado", "Estado: Anulado → Vigente");
            return true;
        }
    }

    public void RegistrarDescarga(Guid id, string usuario)
    {
        lock (_sync)
        {
            var documento = _documentos.FirstOrDefault(d => d.Id == id);
            var archivo = documento?.Archivo;
            if (documento is null || archivo is null)
            {
                return;
            }

            Registrar(id, usuario, "Archivo descargado", $"{archivo.Nombre} · versión {documento.Version}");
        }
    }

    private void Registrar(Guid id, string usuario, string accion, string detalle)
    {
        if (!_historial.TryGetValue(id, out var entradas))
        {
            entradas = new List<EntradaAuditoria>();
            _historial[id] = entradas;
        }

        entradas.Add(new EntradaAuditoria(DateTime.Now, usuario, accion, detalle));
    }

    private static string Valor(string texto) =>
        string.IsNullOrWhiteSpace(texto) ? "(vacío)" : texto;

    private static string FormatoFecha(DateTime? fecha) =>
        fecha?.ToString("dd/MM/yyyy") ?? "Sin vencimiento";

    // Datos de ejemplo tomados del histórico original. No tienen archivo adjunto.
    private void SembrarEjemplos()
    {
        AgregarEjemplo("Acta N° 014 - Asamblea Ordinaria Anual 2024", "Acta de Asamblea",
            new DateTime(2024, 2, 10), "014", "Aprobación de estados financieros y plan de inversión.");

        AgregarEjemplo("Acta N° 013 - Nombramiento Revisor Fiscal", "Acta de Asamblea",
            new DateTime(2023, 11, 5), "013", "Elección del nuevo revisor fiscal para el periodo 2024.");

        AgregarEjemplo("Estatutos Generales Asoagro 2023", "Estatuto Actualizado",
            new DateTime(2023, 3, 15), string.Empty, "Reforma estatutaria aprobada en Asamblea Extraordinaria.");

        AgregarEjemplo("Reglamento Interno de Uso de Maquinaria", "Reglamento Interno",
            new DateTime(2022, 8, 20), string.Empty, "Normas y tarifas para el préstamo de herramientas.");
    }

    private void AgregarEjemplo(string titulo, string tipo, DateTime fecha, string referencia, string descripcion)
    {
        var documento = new DocumentoInstitucional
        {
            Titulo = titulo,
            Tipo = tipo,
            Estado = EstadosDocumento.Vigente,
            Fecha = fecha,
            NumeroReferencia = referencia,
            Descripcion = descripcion,
            CreadoPor = "Sistema",
            ActualizadoPor = "Sistema"
        };

        _documentos.Add(documento);
        Registrar(documento.Id, "Sistema", "Registro creado", "Dato de ejemplo, sin archivo adjunto");
    }
}

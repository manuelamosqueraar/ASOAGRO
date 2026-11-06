using Asoagro.Models;

namespace Asoagro.Services;

// Guarda los documentos en memoria mientras la app está abierta.
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

            return entradas.AsEnumerable().Reverse().ToList();
        }
    }

    public IReadOnlyList<ItemHistorialActividad> ObtenerHistorialGlobal()
    {
        lock (_sync)
        {
            var resultado = new List<ItemHistorialActividad>();
            var dictDocs = _documentos.ToDictionary(d => d.Id);

            foreach (var kvp in _historial)
            {
                if (dictDocs.TryGetValue(kvp.Key, out var doc))
                {
                    foreach (var entrada in kvp.Value)
                    {
                        resultado.Add(new ItemHistorialActividad(
                            doc.Id,
                            doc.Codigo,
                            doc.Titulo,
                            doc.Tipo,
                            entrada.Fecha,
                            entrada.Usuario,
                            entrada.Accion,
                            entrada.Detalle));
                    }
                }
            }

            return resultado.OrderByDescending(r => r.Fecha).ToList();
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

    private void SembrarEjemplos()
    {
        var dummyPdf = System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 Documento Oficial de Asoagro - Federación FAAUA");

        var doc1 = AgregarEjemplo("Acta N° 014 - Asamblea Ordinaria Anual 2024", "Acta de Asamblea",
            new DateTime(2024, 2, 10), "014", "Aprobación de estados financieros, fijación de cuotas de sostenimiento y plan operativo anual.",
            ArchivoAdjunto.Crear("Acta_014_Asamblea_Ordinaria_2024.pdf", "application/pdf", dummyPdf));
        doc1.Compromisos.Add(new CompromisoActa
        {
            Descripcion = "Entrega de informe financiero consolidado y balance de tesorería ante la Revisoría Fiscal",
            Responsable = "Tesorería General (Manuela Mosquera)",
            FechaLimite = DateTime.Today.AddDays(10),
            Estado = "En curso",
            Observaciones = "Avance del 85% con conciliación bancaria y libros de recaudo"
        });
        doc1.Compromisos.Add(new CompromisoActa
        {
            Descripcion = "Actualización de expedientes y membresías de 12 productores federados de plátano",
            Responsable = "Secretaría General (Karen Argumedo)",
            FechaLimite = DateTime.Today.AddDays(-4),
            Estado = "Atrasado",
            Observaciones = "Pendiente soporte catastral de 2 predios de base"
        });

        var doc2 = AgregarEjemplo("Acta N° 013 - Nombramiento Revisor Fiscal", "Acta de Asamblea",
            new DateTime(2023, 11, 5), "013", "Elección del nuevo revisor fiscal para el periodo 2024 y ratificación de dignatarios.",
            ArchivoAdjunto.Crear("Acta_013_Nombramiento_Revisor_Fiscal.pdf", "application/pdf", dummyPdf));
        doc2.Compromisos.Add(new CompromisoActa
        {
            Descripcion = "Radicación formal del nombramiento ante la Cámara de Comercio de Urabá",
            Responsable = "Presidencia (Juan Padilla)",
            FechaLimite = DateTime.Today.AddDays(-60),
            Estado = "Cumplido",
            Observaciones = "Inscripción en registro mercantil completada con certificado vigente"
        });

        AgregarEjemplo("Estatutos Generales Asoagro 2023", "Estatuto Actualizado",
            new DateTime(2023, 3, 15), "EST-2023", "Reforma estatutaria aprobada en Asamblea Extraordinaria ante Cámara de Comercio de Urabá.",
            ArchivoAdjunto.Crear("Estatutos_Generales_Asoagro_2023.pdf", "application/pdf", dummyPdf));

        AgregarEjemplo("Reglamento Interno de Uso de Maquinaria y Crédito", "Reglamento Interno",
            new DateTime(2022, 8, 20), "REG-01", "Normas, tarifas de uso y procedimiento de solicitud de maquinaria comunitaria para asociados.",
            ArchivoAdjunto.Crear("Reglamento_Maquinaria_Asoagro.pdf", "application/pdf", dummyPdf));
    }

    private DocumentoInstitucional AgregarEjemplo(string titulo, string tipo, DateTime fecha, string referencia, string descripcion, ArchivoAdjunto? archivo = null)
    {
        var documento = new DocumentoInstitucional
        {
            Titulo = titulo,
            Tipo = tipo,
            Estado = EstadosDocumento.Vigente,
            Fecha = fecha,
            NumeroReferencia = referencia,
            Descripcion = descripcion,
            Archivo = archivo,
            Version = archivo is null ? 0 : 1,
            CreadoPor = "Presidencia",
            ActualizadoPor = "Presidencia"
        };

        _documentos.Add(documento);
        Registrar(documento.Id, "Presidencia", "Registro creado", archivo is null ? "Sin archivo adjunto" : $"Archivo adjunto: {archivo.Nombre}");
        return documento;
    }
}

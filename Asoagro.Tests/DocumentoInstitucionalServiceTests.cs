using Asoagro.Models;
using Asoagro.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Asoagro.Tests;

public class DocumentoInstitucionalServiceTests
{
    [Fact]
    public void Actualizar_DocumentoAsociado_PersisteLaRelacionYLaRegistraEnHistorial()
    {
        var servicio = new InMemoryDocumentoInstitucionalService();
        var datosIniciales = CrearDatos("CC-123", "María Productora");
        var documento = servicio.Crear(datosIniciales, null, "presidencia");

        var resultado = servicio.Actualizar(
            documento.Id,
            CrearDatos("CC-456", "Juan Asociado"),
            null,
            "presidencia");

        var actualizado = servicio.ObtenerPorId(documento.Id);

        Assert.True(resultado.Exitoso);
        Assert.NotNull(actualizado);
        Assert.Equal("CC-456", actualizado.CedulaAsociado);
        Assert.Equal("Juan Asociado", actualizado.NombreAsociado);
        Assert.Contains(resultado.Cambios, cambio => cambio.Contains("Asociado relacionado", StringComparison.Ordinal));
        Assert.Contains(servicio.ObtenerHistorial(documento.Id),
            entrada => entrada.Detalle.Contains("Asociado relacionado", StringComparison.Ordinal));
    }

    [Fact]
    public void Crear_Y_Actualizar_FechaVencimientoOpcional_ConservaYCreaTrazabilidad()
    {
        var servicio = new InMemoryDocumentoInstitucionalService();
        var fechaVencimiento = new DateTime(2027, 5, 15);
        var documento = servicio.Crear(CrearDatos(fechaVencimiento), null, "presidencia");

        Assert.Equal(fechaVencimiento, documento.FechaVencimiento);

        var resultado = servicio.Actualizar(
            documento.Id,
            CrearDatos("CC-123", "María Productora"),
            null,
            "presidencia");

        var actualizado = servicio.ObtenerPorId(documento.Id);

        Assert.True(resultado.Exitoso);
        Assert.NotNull(actualizado);
        Assert.Null(actualizado.FechaVencimiento);
        Assert.Contains(resultado.Cambios,
            cambio => cambio.Contains("Fecha de vencimiento", StringComparison.Ordinal));
        Assert.Contains(servicio.ObtenerHistorial(documento.Id),
            entrada => entrada.Detalle.Contains("Fecha de vencimiento", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Worker_PublicaAlertaParaDocumentoInstitucionalConVencimiento()
    {
        var documentos = new InMemoryDocumentoInstitucionalService();
        var documento = documentos.Crear(
            CrearDatos("CC-123", "María Productora") with { FechaVencimiento = DateTime.Today.AddDays(5) },
            null,
            "presidencia");
        var notificaciones = new NotificacionService();
        var alertaPublicada = new TaskCompletionSource<Notificacion>(TaskCreationOptions.RunContinuationsAsynchronously);
        notificaciones.NotificacionPublicada += notificacion =>
        {
            if (notificacion.ClaveUnica?.StartsWith($"acta|{documento.Id:N}|", StringComparison.Ordinal) == true)
            {
                alertaPublicada.TrySetResult(notificacion);
            }
        };
        var worker = new AlertasVencimientoWorker(
            new SinAlertasDocumentoService(),
            documentos,
            notificaciones,
            NullLogger<AlertasVencimientoWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            var alerta = await alertaPublicada.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal("/actas-estatutos/documentos", alerta.Enlace);
            Assert.Contains("Presidente", alerta.RolesDestino);
            Assert.Contains("Presidente", alerta.RolesEnlace);
            Assert.Contains(documento.Titulo, alerta.Titulo, StringComparison.Ordinal);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    private static DatosDocumento CrearDatos(string cedula, string nombre) => new(
        "Acta de prueba",
        "Acta de Asamblea",
        EstadosDocumento.Vigente,
        new DateTime(2026, 11, 1),
        "ACT-001",
        "Registro de prueba",
        cedula,
        nombre);

    private static DatosDocumento CrearDatos(DateTime fechaVencimiento) =>
        CrearDatos("CC-123", "María Productora") with { FechaVencimiento = fechaVencimiento };

    private sealed class SinAlertasDocumentoService : IAlertaDocumentoService
    {
        public IReadOnlyCollection<AlertaDocumento> ObtenerAlertasPendientes(DateTime fechaReferencia) =>
            Array.Empty<AlertaDocumento>();
    }
}

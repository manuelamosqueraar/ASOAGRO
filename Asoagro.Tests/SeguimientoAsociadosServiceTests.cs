using Asoagro.Configuration;
using Asoagro.Models;
using Asoagro.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Asoagro.Tests;

public class SeguimientoAsociadosServiceTests
{
    [Fact]
    public void Registrar_GuardaLaGestionVinculadaAlAsociado()
    {
        var asociados = new InMemoryAsociadoService(Options.Create(new AsoagroOptions()));
        var service = new InMemorySeguimientoAsociadoService(asociados);
        var asociado = asociados.ObtenerTodos().First();

        var resultado = service.Registrar(new SeguimientoAsociado
        {
            AsociadoId = asociado.Id,
            TipoGestion = "Llamada",
            Descripcion = "Se confirmó la visita al predio.",
            ProximaGestion = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            GestionadoPor = "Directora"
        });

        var seguimiento = Assert.Single(service.ObtenerTodos());
        Assert.True(resultado.Exitoso);
        Assert.Equal(asociado.Id, seguimiento.AsociadoId);
        Assert.Equal(asociado.Cedula, seguimiento.Asociado?.Cedula);
        Assert.Equal("Llamada", seguimiento.TipoGestion);
        Assert.Equal("Directora", seguimiento.GestionadoPor);
    }

    [Fact]
    public void Registrar_RechazaUnAsociadoQueNoExiste()
    {
        var asociados = new InMemoryAsociadoService(Options.Create(new AsoagroOptions()));
        var service = new InMemorySeguimientoAsociadoService(asociados);

        var resultado = service.Registrar(new SeguimientoAsociado
        {
            AsociadoId = int.MaxValue,
            TipoGestion = "Visita",
            Descripcion = "Se revisó el estado del cultivo.",
            GestionadoPor = "Directora"
        });

        Assert.False(resultado.Exitoso);
        Assert.Empty(service.ObtenerTodos());
    }

    [Fact]
    public void ActualizarAsociado_ConservaElIdYReflejaLosCambiosEnElPadron()
    {
        var service = new InMemoryAsociadoService(Options.Create(new AsoagroOptions()));
        var original = service.ObtenerTodos().First();
        var actualizado = AsociadoMapper.ToModel(original);
        actualizado.NombrePredio = "Finca Actualizada";

        var resultado = service.Actualizar(AsociadoMapper.ToEntity(actualizado));

        var registro = service.ObtenerTodos().Single(asociado => asociado.Id == original.Id);
        Assert.True(resultado.Exitoso);
        Assert.Equal("Finca Actualizada", registro.NombrePredio);
        Assert.Equal(original.Cedula, registro.Cedula);
    }

    [Fact]
    public async Task Worker_PublicaAlertaParaContactoProximoUnaSolaVez()
    {
        var asociados = new InMemoryAsociadoService(Options.Create(new AsoagroOptions()));
        var seguimientos = new InMemorySeguimientoAsociadoService(asociados);
        var asociado = asociados.ObtenerTodos().First();
        seguimientos.Registrar(new SeguimientoAsociado
        {
            AsociadoId = asociado.Id,
            TipoGestion = "Llamada",
            Descripcion = "Se acordó volver a conversar.",
            ProximaGestion = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            GestionadoPor = "Directora"
        });

        var notificaciones = new NotificacionService();
        var alertaPublicada = new TaskCompletionSource<Notificacion>(TaskCreationOptions.RunContinuationsAsynchronously);
        notificaciones.NotificacionPublicada += notificacion =>
        {
            if (notificacion.Categoria == CategoriaNotificacion.Seguimiento)
            {
                alertaPublicada.TrySetResult(notificacion);
            }
        };
        var worker = new AlertasSeguimientoWorker(
            seguimientos,
            notificaciones,
            NullLogger<AlertasSeguimientoWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            var alerta = await alertaPublicada.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal("/seguimiento-asociados", alerta.Enlace);
            Assert.Equal(NivelNotificacion.Advertencia, alerta.Nivel);
            Assert.Contains("Director", alerta.RolesDestino);
            Assert.Contains("3 días", alerta.Titulo, StringComparison.Ordinal);
            Assert.StartsWith($"seguimiento|{asociado.Id}|", alerta.ClaveUnica, StringComparison.Ordinal);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }
}

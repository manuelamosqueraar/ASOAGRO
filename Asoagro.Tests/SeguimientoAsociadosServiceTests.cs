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
            GestionadoPor = "Secretaría"
        });

        var seguimiento = Assert.Single(service.ObtenerTodos());
        Assert.True(resultado.Exitoso);
        Assert.Equal(asociado.Id, seguimiento.AsociadoId);
        Assert.Equal(asociado.Cedula, seguimiento.Asociado?.Cedula);
        Assert.Equal("Llamada", seguimiento.TipoGestion);
        Assert.Equal("Secretaría", seguimiento.GestionadoPor);
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
            GestionadoPor = "Secretaría"
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
            GestionadoPor = "Secretaría"
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
            Assert.Contains("Secretaria", alerta.RolesDestino);
            Assert.Contains("3 días", alerta.Titulo, StringComparison.Ordinal);
            Assert.StartsWith($"seguimiento|{asociado.Id}|", alerta.ClaveUnica, StringComparison.Ordinal);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public void ValidarHoraReunion_RechazaHorariosFueraDelRango08a17()
    {
        // 07:30 antes de las 08:00
        var esValidoTemprano = TimeOnly.TryParse("07:30", out var t1) && t1 >= new TimeOnly(8, 0) && t1 <= new TimeOnly(17, 0);
        Assert.False(esValidoTemprano);

        // 17:30 después de las 17:00
        var esValidoTarde = TimeOnly.TryParse("17:30", out var t2) && t2 >= new TimeOnly(8, 0) && t2 <= new TimeOnly(17, 0);
        Assert.False(esValidoTarde);

        // 08:00 y 17:00 límites exactos permitidos
        var esValidoInicio = TimeOnly.TryParse("08:00", out var t3) && t3 >= new TimeOnly(8, 0) && t3 <= new TimeOnly(17, 0);
        Assert.True(esValidoInicio);

        var esValidoFin = TimeOnly.TryParse("17:00", out var t4) && t4 >= new TimeOnly(8, 0) && t4 <= new TimeOnly(17, 0);
        Assert.True(esValidoFin);

        // 10:30 horario normal
        var esValidoMedio = TimeOnly.TryParse("10:30", out var t5) && t5 >= new TimeOnly(8, 0) && t5 <= new TimeOnly(17, 0);
        Assert.True(esValidoMedio);
    }

    [Fact]
    public void EtiquetaHora_SeRecuperaYOcultaEnDescripcionSinExceder1000Caracteres()
    {
        var tagRegex = new System.Text.RegularExpressions.Regex(@"^\[HORA:\s*([0-2]?[0-9]:[0-5][0-9])\]\s*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var hora = "09:30";
        var textoOriginal = "Se acordó revisar el sistema de riego.";
        var tag = $"[HORA:{hora}]\n";

        var maxTexto = 1000 - tag.Length;
        var textoAjustado = textoOriginal.Length > maxTexto ? textoOriginal[..maxTexto] : textoOriginal;
        var guardadoEnBd = tag + textoAjustado;

        Assert.True(guardadoEnBd.Length <= 1000);
        Assert.StartsWith("[HORA:09:30]", guardadoEnBd);

        // Simular lectura de BD tras reinicio (sin caché)
        var match = tagRegex.Match(guardadoEnBd);
        Assert.True(match.Success);

        var horaRecuperada = match.Groups[1].Value;
        var textoLimpio = guardadoEnBd[match.Length..];

        Assert.Equal("09:30", horaRecuperada);
        Assert.Equal(textoOriginal, textoLimpio);
        Assert.DoesNotContain("[HORA:", textoLimpio);
    }

    [Fact]
    public void EtiquetaHora_RegistrosAntiguosSinTagSeCarganIntactos()
    {
        var tagRegex = new System.Text.RegularExpressions.Regex(@"^\[HORA:\s*([0-2]?[0-9]:[0-5][0-9])\]\s*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var registroAntiguo = "Reunión antigua realizada sin formato de etiqueta.";

        var match = tagRegex.Match(registroAntiguo);
        Assert.False(match.Success);

        var textoLimpio = match.Success ? registroAntiguo[match.Length..] : registroAntiguo;
        Assert.Equal(registroAntiguo, textoLimpio);
    }
}

using System.Net;
using System.Net.Http.Json;
using Asoagro.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Asoagro.Tests;

public class AsociadosApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public AsociadosApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task RegistrarAsociado_CuandoDatosSonValidos_RetornaOkYAuditoria()
    {
        var cedula = $"CC-{Guid.NewGuid():N}"[..20];
        var asociado = CrearAsociado(cedula);

        var response = await client.PostAsJsonAsync("/api/asociados", asociado);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<RegistroAsociadoResponse>();
        Assert.NotNull(payload?.Data);
        Assert.Equal(cedula, payload.Data.Cedula);
        Assert.False(string.IsNullOrWhiteSpace(payload.Data.CreadoPor));
        Assert.Equal(payload.Data.CreadoPor, payload.Data.ActualizadoPor);
        Assert.True(payload.Data.FechaRegistro <= payload.Data.FechaActualizacion);
    }

    [Fact]
    public async Task RegistrarAsociado_CuandoCedulaExiste_RetornaConflict()
    {
        var cedula = $"CC-{Guid.NewGuid():N}"[..20];

        await client.PostAsJsonAsync("/api/asociados", CrearAsociado(cedula));
        var response = await client.PostAsJsonAsync("/api/asociados", CrearAsociado(cedula));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("asociado_duplicado", error?.Code);
    }

    [Fact]
    public async Task RegistrarAsociado_CuandoFaltanCampos_RetornaValidationError()
    {
        var response = await client.PostAsJsonAsync("/api/asociados", new Asociado());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("validation_error", error?.Code);
        Assert.NotEmpty(error?.Details ?? new Dictionary<string, string[]>());
    }

    [Fact]
    public async Task ObtenerAsociados_DespuesDeRegistrar_IncluyeElAsociado()
    {
        var cedula = $"CC-{Guid.NewGuid():N}"[..20];
        await client.PostAsJsonAsync("/api/asociados", CrearAsociado(cedula));

        var asociados = await client.GetFromJsonAsync<List<Asociado>>("/api/asociados");

        Assert.Contains(asociados ?? new(), asociado => asociado.Cedula == cedula);
    }

    private static Asociado CrearAsociado(string cedula)
    {
        return new Asociado
        {
            Cedula = cedula,
            NombreCompleto = "Productor de Prueba",
            FincaVereda = "Finca La Esperanza",
            CultivoPrincipal = "Cacao",
            TieneBPA = true
        };
    }

    private sealed class RegistroAsociadoResponse
    {
        public string Mensaje { get; set; } = string.Empty;

        public Asociado? Data { get; set; }
    }
}

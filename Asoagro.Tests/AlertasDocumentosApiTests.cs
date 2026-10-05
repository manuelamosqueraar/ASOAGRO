using System.Net;
using System.Net.Http.Json;
using Asoagro.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Asoagro.Tests;

public class AlertasDocumentosApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public AlertasDocumentosApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task ObtenerAlertas_RetornaAlertasPendientesOrdenadasPorUmbral()
    {
        var response = await client.GetAsync("/api/alertas-documentos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var alertas = await response.Content.ReadFromJsonAsync<List<AlertaDocumento>>();
        Assert.NotNull(alertas);
        Assert.Contains(alertas, alerta => alerta.DiasRestantes <= 7 && alerta.MensajeAlerta.Contains("crítica", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(alertas, alerta => alerta.DiasRestantes <= 15 && alerta.MensajeAlerta.Contains("Advertencia", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(alertas, alerta => alerta.DiasRestantes <= 30 && alerta.MensajeAlerta.Contains("preventivo", StringComparison.OrdinalIgnoreCase));
    }
}

using System.Security.Claims;
using Asoagro.Components;
using Asoagro.Configuration;
using Asoagro.Data;
using Asoagro.Models;
using Asoagro.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;


var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Autenticación con Cookies y Google OAuth
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleAuthenticationConfigured =
    !string.IsNullOrWhiteSpace(googleClientId) &&
    !string.IsNullOrWhiteSpace(googleClientSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = googleAuthenticationConfigured
        ? GoogleDefaults.AuthenticationScheme
        : CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
});

if (googleAuthenticationConfigured)
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId!;
            options.ClientSecret = googleClientSecret!;

            // Asignación de roles dinámica basada en el correo que inicia sesión
            options.Events.OnCreatingTicket = context =>
            {
                var email = context.Identity?.FindFirst(ClaimTypes.Email)?.Value?.ToLower() ?? "";
                var identity = (ClaimsIdentity?)context.Identity;

                if (identity != null)
                {
                    // ===== REEMPLAZA ESTOS CORREOS CON LOS DE TU EQUIPO =====
                    if (email == "manuela.mosqueraar@amigo.edu.co")
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, "Director"));
                    }
                    else if (email == "juan.padillamo@amigo.edu.co")
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, "Presidente"));
                    }
                    else if (email == "karen.argumedoco@amigo.edu.co")
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, "Tesorero"));
                    }
                    else if (email == "karenargumedo157@gmail.com")
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, "Invitado"));
                    }
                }

                return Task.CompletedTask;
            };

            options.Events.OnRemoteFailure = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GoogleOAuth");
                logger.LogError(
                    context.Failure,
                    "Google OAuth failed. Failure type: {FailureType}",
                    context.Failure?.GetType().Name ?? "unknown");

                context.HandleResponse();
                context.Response.Redirect("/login?error=google_oauth");
                return Task.CompletedTask;
            };
        });
}

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Servicios para componentes Razor interactivos y controladores API
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<AsoagroOptions>(builder.Configuration.GetSection(AsoagroOptions.SectionName));
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var details = context.ModelState
                .Where(item => item.Value?.Errors.Count > 0)
                .ToDictionary(
                    item => item.Key,
                    item => item.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

            return new BadRequestObjectResult(new ApiError
            {
                Code = "validation_error",
                Message = "La solicitud contiene datos inválidos.",
                Details = details
            });
        };
    });

static string? NormalizarConexionSupabase(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return null;
    var s = raw.Trim().Trim('"', '\'');
    if (s.Contains("<TU_CADENA_DE_CONEXION>", StringComparison.OrdinalIgnoreCase) ||
        s.Contains("<cadena-de-conexion-de-Supabase>", StringComparison.OrdinalIgnoreCase) ||
        s.Contains("<TU_USUARIO>", StringComparison.OrdinalIgnoreCase) ||
        s.Contains("[Asociaciones_aso]", StringComparison.OrdinalIgnoreCase) ||
        s.Contains("YOUR-PASSWORD", StringComparison.OrdinalIgnoreCase) ||
        s.Contains("<TU_CONTRASENA>", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    if (s.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        s.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(s);
        var userInfo = uri.UserInfo.Split(':', 2);
        var connBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/')) ? "postgres" : uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            SslMode = SslMode.Require
        };
        return connBuilder.ConnectionString;
    }

    return s;
}

static string? ObtenerConexionSupabase(IConfiguration configuration)
{
    var candidatos = new List<string?>
    {
        Environment.GetEnvironmentVariable("ASOAGRO_SUPABASE_CONNECTION"),
        Environment.GetEnvironmentVariable("ASOAGRO_SUPABASE_CONNECTION", EnvironmentVariableTarget.User),
        configuration["ASOAGRO_SUPABASE_CONNECTION"]
    };

    if (configuration is IConfigurationRoot configRoot)
    {
        foreach (var provider in configRoot.Providers.Reverse())
        {
            if (provider.TryGet("ASOAGRO_SUPABASE_CONNECTION", out var valor))
            {
                candidatos.Add(valor);
            }
        }
    }

    foreach (var candidato in candidatos)
    {
        var normalizada = NormalizarConexionSupabase(candidato);
        if (!string.IsNullOrWhiteSpace(normalizada))
        {
            return normalizada;
        }
    }

    return null;
}

var supabaseConnection = ObtenerConexionSupabase(builder.Configuration);

if (!string.IsNullOrWhiteSpace(supabaseConnection))
{
    builder.Services.AddDbContextFactory<AsoagroDbContext>(options =>
        options.UseNpgsql(supabaseConnection, npgsqlOptions =>
            npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)));

    builder.Services.AddSingleton<IAsociadoService, PostgresAsociadoService>();
    builder.Services.AddSingleton<ISeguimientoAsociadoService, PostgresSeguimientoAsociadoService>();
    builder.Services.AddSingleton<IAlertaDocumentoService, PostgresAlertaDocumentoService>();
}
else
{
    // Servicios de datos en memoria
    builder.Services.AddSingleton<IAsociadoService, InMemoryAsociadoService>();
    builder.Services.AddSingleton<ISeguimientoAsociadoService, InMemorySeguimientoAsociadoService>();
    builder.Services.AddSingleton<IAlertaDocumentoService, InMemoryAlertaDocumentoService>();
}

// Actas, estatutos y reglamentos (en memoria por ahora)
builder.Services.AddSingleton<IDocumentoInstitucionalService, InMemoryDocumentoInstitucionalService>();

// Servicio de gestión de sesión/roles de usuario
builder.Services.AddScoped<UserService>();

// Configuración de HttpClient para peticiones internas
builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
});
// Avisos en tiempo real: una sola lista compartida por todas las pantallas conectadas
builder.Services.AddSingleton<NotificacionService>();

// Revisa los vencimientos de documentos cada hora y crea los avisos
builder.Services.AddHostedService<AlertasVencimientoWorker>();
builder.Services.AddHostedService<AlertasSeguimientoWorker>();
var app = builder.Build();

if (string.IsNullOrWhiteSpace(supabaseConnection))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Falta configurar ASOAGRO_SUPABASE_CONNECTION. La aplicación no puede iniciar en producción usando datos en memoria.");
    }

    app.Logger.LogWarning(
        "No hay una conexión válida a Supabase configurada. La aplicación usará datos en memoria, que se perderán al reiniciar.");
}

if (!googleAuthenticationConfigured)
{
    app.Logger.LogWarning(
        "Google OAuth no está configurado. El inicio de sesión con Google estará deshabilitado.");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 2. Middlewares de Autenticación y Autorización en el orden correcto
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;

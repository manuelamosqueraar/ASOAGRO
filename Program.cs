using Microsoft.AspNetCore.HttpOverrides;
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

// Configuración de Forwarded Headers para proxies de la nube (Render, Railway, etc.)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// 1. Configuración de Autenticación con Cookies y Google OAuth
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleAuthenticationConfigured =
    !string.IsNullOrWhiteSpace(googleClientId) &&
    !string.IsNullOrWhiteSpace(googleClientSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.Events = new CookieAuthenticationEvents
    {
        OnValidatePrincipal = async context =>
        {
            var email = context.Principal?.FindFirstValue(ClaimTypes.Email)
                ?? context.Principal?.FindFirstValue("email")
                ?? context.Principal?.Identity?.Name;
            var authIat = context.Principal?.FindFirstValue("auth_iat");

            var monitor = context.HttpContext.RequestServices.GetRequiredService<SistemaMonitoreoService>();
            if (monitor.EstaSesionRevocada(email, authIat))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var adminSvc = context.HttpContext.RequestServices.GetRequiredService<AdminSistemaService>();
                var usuarioDb = await adminSvc.BuscarUsuarioPorCorreoAsync(email);
                if (usuarioDb != null && !usuarioDb.Activo)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        },
        OnRedirectToAccessDenied = async context =>
        {
            var adminSvc = context.HttpContext.RequestServices.GetRequiredService<AdminSistemaService>();
            var user = context.HttpContext.User;
            var nombre = user.Identity?.Name ?? "Anónimo";
            var correo = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var rol = string.Join(", ", user.FindAll(ClaimTypes.Role).Select(c => c.Value));
            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();

            await adminSvc.RegistrarAuditoriaAsync(
                nombre,
                correo,
                string.IsNullOrWhiteSpace(rol) ? "Sin rol" : rol,
                "Seguridad / Acceso",
                "Acceso denegado por falta de permisos",
                context.Request.Path.Value ?? "/",
                "Denegado",
                $"Solicitud rechazada hacia ruta protegida: {context.Request.Path.Value}",
                ip);

            context.Response.Redirect(context.RedirectUri);
        }
    };
});

if (googleAuthenticationConfigured)
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId!;
            options.ClientSecret = googleClientSecret!;

            // Asignación de roles dinámica basada en el correo que inicia sesión
            options.Events.OnCreatingTicket = async context =>
            {
                var email = context.Identity?.FindFirst(ClaimTypes.Email)?.Value?.ToLower() ?? "";
                var identity = (ClaimsIdentity?)context.Identity;

                if (identity != null)
                {
                    if (context.User.TryGetProperty("picture", out var pictureProp))
                    {
                        var pic = pictureProp.GetString();
                        if (!string.IsNullOrWhiteSpace(pic))
                        {
                            identity.AddClaim(new Claim("picture", pic));
                        }
                    }

                    identity.AddClaim(new Claim("auth_iat", DateTime.UtcNow.Ticks.ToString()));

                    var adminSvc = context.HttpContext.RequestServices.GetRequiredService<AdminSistemaService>();
                    var usuarioBd = await adminSvc.BuscarUsuarioPorCorreoAsync(email);
                    if (usuarioBd != null)
                    {
                        if (!usuarioBd.Activo)
                        {
                            await adminSvc.RegistrarAuditoriaAsync(
                                usuarioBd.NombreCompleto,
                                usuarioBd.Correo,
                                usuarioBd.Rol,
                                "Autenticación",
                                "Inicio de sesión Google OAuth rechazado (cuenta inactiva)",
                                usuarioBd.Correo,
                                "Denegado",
                                "La cuenta existe pero se encuentra desactivada por el Administrador.",
                                context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                                usuarioBd.Id,
                                usuarioBd.Id);
                            context.Fail("La cuenta se encuentra desactivada.");
                            return;
                        }

                        var rolCanonica = AuthRoleHelper.NormalizarRolCanonica(usuarioBd.Rol);
                        if (!string.IsNullOrWhiteSpace(rolCanonica))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, rolCanonica));
                        }
                        await adminSvc.RegistrarAccesoUsuarioAsync(email);
                    }
                    else
                    {
                        var rolFallback = AuthRoleHelper.ResolverRolPorCorreoFallback(email);
                        if (!string.IsNullOrWhiteSpace(rolFallback))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, rolFallback));
                        }
                    }

                    var emailSafe = string.IsNullOrWhiteSpace(email) ? "google-oauth@desconocido" : email;
                    var rolLog = identity.FindFirst(ClaimTypes.Role)?.Value ?? "Sin rol";
                    var nombreLog = identity.FindFirst(ClaimTypes.Name)?.Value ?? emailSafe;
                    await adminSvc.RegistrarAuditoriaAsync(
                        nombreLog,
                        emailSafe,
                        rolLog,
                        "Autenticación",
                        "Inicio de sesión con Google OAuth",
                        emailSafe,
                        "Autorizado",
                        "Autenticación federada mediante Google OAuth 2.0.",
                        context.HttpContext.Connection.RemoteIpAddress?.ToString());
                }
            };

            options.Events.OnRemoteFailure = async context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GoogleOAuth");
                logger.LogError(
                    context.Failure,
                    "Google OAuth failed. Failure type: {FailureType}",
                    context.Failure?.GetType().Name ?? "unknown");

                var adminSvc = context.HttpContext.RequestServices.GetRequiredService<AdminSistemaService>();
                await adminSvc.RegistrarAuditoriaAsync(
                    "Desconocido",
                    string.Empty,
                    "Sin rol",
                    "Autenticación",
                    "Fallo de inicio de sesión Google OAuth",
                    "OAuth2",
                    "Error",
                    $"Tipo de fallo: {context.Failure?.GetType().Name ?? "unknown"}",
                    context.HttpContext.Connection.RemoteIpAddress?.ToString());

                context.HandleResponse();
                var errorCode = context.Failure?.Message?.Contains("desactivada", StringComparison.OrdinalIgnoreCase) == true
                    ? "account_disabled"
                    : "google_oauth";
                context.Response.Redirect($"/login?error={errorCode}");
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
var rawSupabaseConnection = Environment.GetEnvironmentVariable("ASOAGRO_SUPABASE_CONNECTION")
    ?? Environment.GetEnvironmentVariable("ASOAGRO_SUPABASE_CONNECTION", EnvironmentVariableTarget.User)
    ?? builder.Configuration["ASOAGRO_SUPABASE_CONNECTION"];

static string? NormalizarConexionSupabase(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return null;
    var s = raw.Trim().Trim('"', '\'');
    if (s.Contains("<TU_CADENA_DE_CONEXION>", StringComparison.OrdinalIgnoreCase) ||
        s.Contains("<TU_USUARIO>", StringComparison.OrdinalIgnoreCase) ||
        s.Contains("[Asociaciones_aso]", StringComparison.OrdinalIgnoreCase) ||
        s.Contains("YOUR-PASSWORD", StringComparison.OrdinalIgnoreCase) ||
        s.Contains("<TU_CONTRASENA>", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    NpgsqlConnectionStringBuilder connBuilder;
    if (s.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        s.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(s);
        var userInfo = uri.UserInfo.Split(':', 2);
        connBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/')) ? "postgres" : uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            SslMode = SslMode.Require
        }; 
    }
    else
    {
        connBuilder = new NpgsqlConnectionStringBuilder(s);
    }

    // Configuración óptima para el pooler de Supabase (Supavisor)
    if (connBuilder.KeepAlive == 0)
    {
        connBuilder.KeepAlive = 15; // Evita desconexiones por inactividad del pooler
    }
    if (connBuilder.Timeout < 30)
    {
        connBuilder.Timeout = 30; // Margen para latencia internacional
    }
    if (connBuilder.CommandTimeout < 30)
    {
        connBuilder.CommandTimeout = 30;
    }

    return connBuilder.ConnectionString;
}

var supabaseConnection = NormalizarConexionSupabase(rawSupabaseConnection);

if (!string.IsNullOrWhiteSpace(supabaseConnection))
{
    builder.Services.AddDbContextFactory<AsoagroDbContext>(options =>
        options.UseNpgsql(supabaseConnection, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
            npgsqlOptions.CommandTimeout(30);
        }));

    builder.Services.AddSingleton<IDocumentoAsociacionService, InMemoryDocumentoAsociacionService>();
    builder.Services.AddSingleton<IAsociadoService, PostgresAsociadoService>();
    builder.Services.AddSingleton<ISeguimientoAsociadoService, PostgresSeguimientoAsociadoService>();
    builder.Services.AddSingleton<IAlertaDocumentoService, PostgresAlertaDocumentoService>();
}
else
{
    // Servicios de datos en memoria
    builder.Services.AddSingleton<IDocumentoAsociacionService, InMemoryDocumentoAsociacionService>();
    builder.Services.AddSingleton<IAsociadoService, InMemoryAsociadoService>();
    builder.Services.AddSingleton<ISeguimientoAsociadoService, InMemorySeguimientoAsociadoService>();
    builder.Services.AddSingleton<IAlertaDocumentoService, InMemoryAlertaDocumentoService>();
}

// Actas, estatutos y reglamentos (en memoria por ahora)
builder.Services.AddSingleton<IDocumentoInstitucionalService, InMemoryDocumentoInstitucionalService>();

// Gestión financiera y tesorería sincronizada
builder.Services.AddSingleton<IFinanzasService, InMemoryFinanzasService>();

// Punto de Venta (PDV) y comercialización agropecuaria
builder.Services.AddSingleton<IPuntoVentaService, InMemoryPuntoVentaService>();

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
builder.Services.AddSingleton<MunicipiosColombiaService>();
builder.Services.AddSingleton<SistemaMonitoreoService>();
builder.Services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<SistemaMonitoreoService>());
builder.Services.AddSingleton<AdminSistemaService>();

// Revisa los vencimientos de documentos cada hora y crea los avisos
builder.Services.AddHostedService<AlertasVencimientoWorker>();
builder.Services.AddHostedService<AlertasSeguimientoWorker>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var adminBootstrap = scope.ServiceProvider.GetRequiredService<AdminSistemaService>();
    await adminBootstrap.AsegurarCuentasYAdminInicialAsync();
}

if (string.IsNullOrWhiteSpace(supabaseConnection))
{
    app.Logger.LogWarning(
        "No hay una conexión válida a Supabase configurada (ASOAGRO_SUPABASE_CONNECTION). La aplicación iniciará usando datos en memoria.");
}

if (!googleAuthenticationConfigured)
{
    app.Logger.LogWarning(
        "Google OAuth no está configurado. El inicio de sesión con Google estará deshabilitado.");
}

app.UseForwardedHeaders();

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

// Exige cambio obligatorio de contraseña inicial antes de abrir cualquier panel
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true &&
        context.User.HasClaim("must_change_password", "true"))
    {
        var path = context.Request.Path.Value ?? "/";
        var esRutaPermitida =
            path.StartsWith("/recuperar", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Auth/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            path.Contains('.', StringComparison.Ordinal);

        if (!esRutaPermitida)
        {
            var correo = context.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var adminSvc = context.RequestServices.GetRequiredService<AdminSistemaService>();
            if (adminSvc.RequiereCambioContrasena(correo))
            {
                context.Response.Redirect("/recuperar?primerAcceso=true");
                return;
            }
        }
    }

    await next();
});

app.UseAntiforgery();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;

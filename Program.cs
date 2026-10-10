using System.Security.Claims;
using Asoagro.Components;
using Asoagro.Configuration;
using Asoagro.Models;
using Asoagro.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Autenticación con Cookies y Google OAuth
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
})
.AddGoogle(options =>
{
    // Obtiene las credenciales configuradas en appsettings.json
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

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
            else if (email == "manumosque743@gmail.com")
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, "Invitado"));
            }
        }

        return Task.CompletedTask;
    };
});

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

// Servicios de datos en memoria
builder.Services.AddSingleton<IAsociadoService, InMemoryAsociadoService>();
builder.Services.AddSingleton<IAlertaDocumentoService, InMemoryAlertaDocumentoService>();

// Servicio de gestión de sesión/roles de usuario
builder.Services.AddScoped<UserService>();

// Configuración de HttpClient para peticiones internas
builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
});

var app = builder.Build();

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
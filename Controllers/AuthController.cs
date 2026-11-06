using System.Security.Claims;
using Asoagro.Data;
using Asoagro.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Asoagro.Controllers
{
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly IDbContextFactory<AsoagroDbContext> dbContextFactory;
        private readonly ILogger<AuthController> logger;

        public AuthController(
            IConfiguration configuration,
            IDbContextFactory<AsoagroDbContext> dbContextFactory,
            ILogger<AuthController> logger)
        {
            this.configuration = configuration;
            this.dbContextFactory = dbContextFactory;
            this.logger = logger;
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            [FromForm] string usuario,
            [FromForm] string password,
            [FromForm] bool recordarme = false,
            [FromForm] string returnUrl = "/")
        {
            if (string.IsNullOrWhiteSpace(usuario))
            {
                return Redirect("/login?error=empty_user");
            }

            try
            {
                await using var db = await dbContextFactory.CreateDbContextAsync();
                var loginInput = usuario.Trim().ToLowerInvariant();

                var user = await db.Usuarios
                    .Include(u => u.Asociacion)
                    .FirstOrDefaultAsync(u =>
                        u.Correo.ToLower() == loginInput ||
                        u.DocumentoId.ToLower() == loginInput);

                bool validPassword = false;

                if (user != null)
                {
                    if (!user.Activo)
                    {
                        return Redirect("/login?error=user_inactive");
                    }

                    if (string.IsNullOrEmpty(user.ContrasenaHash))
                    {
                        // Si el usuario aún no tiene contraseña asignada en Supabase,
                        // permitimos el primer inicio y guardamos su contraseña cifrada
                        validPassword = !string.IsNullOrWhiteSpace(password);
                        if (validPassword)
                        {
                            user.ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(password);
                        }
                    }
                    else if (user.ContrasenaHash.StartsWith("$2"))
                    {
                        validPassword = BCrypt.Net.BCrypt.Verify(password, user.ContrasenaHash);
                    }
                    else
                    {
                        validPassword = user.ContrasenaHash == password;
                    }
                }

                if (!validPassword || user == null)
                {
                    logger.LogWarning("Intento fallido de inicio de sesión para el usuario: {Usuario}", usuario);
                    return Redirect("/login?error=invalid_credentials");
                }

                // Actualizar último acceso
                user.UltimoAcceso = DateTime.UtcNow;
                await db.SaveChangesAsync();

                // Crear claims y sesión
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.NombreCompleto),
                    new Claim(ClaimTypes.Email, user.Correo),
                    new Claim("documento_id", user.DocumentoId),
                    new Claim("asociacion_id", user.AsociacionId?.ToString() ?? "")
                };

                // Normalización de roles para asegurar compatibilidad total
                var rolDb = user.Rol?.ToLowerInvariant() ?? "directora";
                switch (rolDb)
                {
                    case "directora":
                    case "director":
                        claims.Add(new Claim(ClaimTypes.Role, "Directora"));
                        claims.Add(new Claim(ClaimTypes.Role, "Director"));
                        break;
                    case "secretaria":
                    case "secretario":
                        claims.Add(new Claim(ClaimTypes.Role, "Secretaria"));
                        claims.Add(new Claim(ClaimTypes.Role, "Secretario"));
                        break;
                    case "tesorero":
                    case "tesorera":
                        claims.Add(new Claim(ClaimTypes.Role, "Tesorero"));
                        claims.Add(new Claim(ClaimTypes.Role, "Tesorera"));
                        break;
                    case "presidente":
                        claims.Add(new Claim(ClaimTypes.Role, "Presidente"));
                        break;
                    case "administrador":
                    case "admin":
                        claims.Add(new Claim(ClaimTypes.Role, "Administrador"));
                        claims.Add(new Claim(ClaimTypes.Role, "Director"));
                        claims.Add(new Claim(ClaimTypes.Role, "Directora"));
                        claims.Add(new Claim(ClaimTypes.Role, "Tesorero"));
                        claims.Add(new Claim(ClaimTypes.Role, "Secretaria"));
                        claims.Add(new Claim(ClaimTypes.Role, "Presidente"));
                        break;
                    default:
                        claims.Add(new Claim(ClaimTypes.Role, user.Rol ?? "directora"));
                        break;
                }

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = recordarme,
                    ExpiresUtc = recordarme ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                // Redirección inteligente según el rol
                if (!string.IsNullOrEmpty(returnUrl) && returnUrl != "/" && !returnUrl.Contains("/login"))
                {
                    return LocalRedirect(returnUrl);
                }

                return rolDb switch
                {
                    "tesorero" or "tesorera" => LocalRedirect("/registrar-documentos"),
                    "presidente" => LocalRedirect("/actas-estatutos"),
                    "secretaria" or "secretario" => LocalRedirect("/actas-estatutos"),
                    _ => LocalRedirect("/consultar-asociados")
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error durante el proceso de autenticación de {Usuario}", usuario);
                return Redirect("/login?error=server_error");
            }
        }

        [HttpGet("login-google")]
        public IActionResult LoginGoogle([FromQuery] string returnUrl = "/")
        {
            var secret = configuration["Authentication:Google:ClientSecret"]
                ?? Environment.GetEnvironmentVariable("Authentication__Google__ClientSecret")
                ?? "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";

            if (string.IsNullOrWhiteSpace(secret))
            {
                logger.LogWarning("Se intentó iniciar sesión con Google pero Authentication__Google__ClientSecret no está configurado.");
                return Redirect("/login?error=google_oauth_not_configured");
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback", "Auth", new { returnUrl })
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback(string returnUrl = "/")
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return Redirect("/login?error=auth_failed");
            }

            var rolUsuario = result.Principal?.FindFirstValue(ClaimTypes.Role);

            if (!string.IsNullOrEmpty(rolUsuario))
            {
                if (rolUsuario == "Tesorero" || rolUsuario == "Tesorera")
                    return LocalRedirect("/registrar-documentos");

                if (rolUsuario == "Director" || rolUsuario == "Directora")
                    return LocalRedirect("/consultar-asociados");

                if (rolUsuario == "Presidente" || rolUsuario == "Secretaria" || rolUsuario == "Secretario")
                    return LocalRedirect("/actas-estatutos");

                if (rolUsuario == "Administrador")
                    return LocalRedirect("/consultar-asociados");

                if (rolUsuario == "Invitado")
                    return LocalRedirect("/");
            }

            if (!string.IsNullOrEmpty(returnUrl) && returnUrl != "/" && !returnUrl.Contains("/login"))
            {
                return LocalRedirect(returnUrl);
            }

            return LocalRedirect("/consultar-asociados");
        }

        [HttpGet("logout")]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/login");
        }
    }
}

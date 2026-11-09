using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Asoagro.Controllers
{
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly IConfiguration configuration;

        public AuthController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpGet("login-google")]
        public IActionResult LoginGoogle([FromQuery] string returnUrl = "/")
        {
            if (string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]))
            {
                return Problem(
                    "Google OAuth no está configurado. Define Authentication__Google__ClientSecret como variable de entorno.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
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

            // Obtenemos el rol que se le asignó automáticamente en el Program.cs según su correo
            var rolUsuario = result.Principal?.FindFirstValue(ClaimTypes.Role);

            // Redirección inteligente según el rol del equipo
            if (!string.IsNullOrEmpty(rolUsuario))
            {
                if (rolUsuario == "Administrador" || rolUsuario == "Admin")
                {
                    return LocalRedirect("/panel-administrador");
                }

                if (rolUsuario == "Tesorero" || rolUsuario == "Tesorera")
                {
                    return LocalRedirect("/panel-tesorero");
                }

                if (rolUsuario == "Secretaria" || rolUsuario == "Secretaría")
                {
                    return LocalRedirect("/consultar-asociados");
                }

                if (rolUsuario == "Presidenta" || rolUsuario == "Presidente")
                {
                    return LocalRedirect("/actas-estatutos");
                }

                if (rolUsuario == "AdministradorPdv" || rolUsuario == "PuntoVenta" || rolUsuario == "Vendedor")
                {
                    return LocalRedirect("/punto-de-venta");
                }
            }

            // Si viene con otra URL específica, respeta la ruta
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl != "/")
            {
                return LocalRedirect(returnUrl);
            }

            return LocalRedirect("/");
        }

        [HttpGet("login-local")]
        public async Task<IActionResult> LoginLocal([FromQuery] string rol = "", [FromQuery] string email = "", [FromQuery] string password = "", [FromQuery] string returnUrl = "")
        {
            var correo = !string.IsNullOrWhiteSpace(email) ? email.Trim() : "manuela.mosqueraar@amigo.edu.co";
            var nombre = "Manuela Andrea Mosquera";
            var rolAsignado = "Administrador";

            var lower = correo.ToLowerInvariant();
            var claims = new List<Claim>();

            // ===== USUARIO MAESTRO 'admin' CON ACCESO A TODAS LAS PESTAÑAS =====
            if (lower == "admin" || lower == "administrador" || lower == "admin@asoagro.org" || lower == "admin@asoagro-faaua.org")
            {
                // Validación de contraseña para el usuario admin
                if (!string.IsNullOrWhiteSpace(password) && password != "admin" && password != "admin123" && password != "asoagro2026")
                {
                    return Redirect("/login?error=invalid_credentials");
                }

                nombre = "Super Administrador ASOAGRO";
                correo = "admin@asoagro-faaua.org";
                rolAsignado = "Presidenta"; // Rol principal

                // Se le otorgan TODOS los roles del sistema para que desbloquee cada sección y autorización
                claims.Add(new Claim(ClaimTypes.Name, nombre));
                claims.Add(new Claim(ClaimTypes.Email, correo));
                claims.Add(new Claim(ClaimTypes.Role, "Presidenta"));
                claims.Add(new Claim(ClaimTypes.Role, "Presidente"));
                claims.Add(new Claim(ClaimTypes.Role, "Secretaria"));
                claims.Add(new Claim(ClaimTypes.Role, "Secretaría"));
                claims.Add(new Claim(ClaimTypes.Role, "Tesorero"));
                claims.Add(new Claim(ClaimTypes.Role, "Tesorera"));
                claims.Add(new Claim(ClaimTypes.Role, "AdministradorPdv"));
                claims.Add(new Claim(ClaimTypes.Role, "PuntoVenta"));
                claims.Add(new Claim(ClaimTypes.Role, "Vendedor"));
                claims.Add(new Claim(ClaimTypes.Role, "Administrador"));
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            else if (lower == "manumosque743@gmail.com" || lower.Contains("tesor") || rol.Equals("Tesorero", StringComparison.OrdinalIgnoreCase) || rol.Equals("Tesorera", StringComparison.OrdinalIgnoreCase))
            {
                rolAsignado = "Tesorero";
                nombre = "Manuela Mosquera";
                if (string.IsNullOrWhiteSpace(email)) correo = "manumosque743@gmail.com";
                claims.Add(new Claim(ClaimTypes.Name, nombre));
                claims.Add(new Claim(ClaimTypes.Email, correo));
                claims.Add(new Claim(ClaimTypes.Role, rolAsignado));
            }
            else if (lower == "manuela.mosqueraar@amigo.edu.co" || rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) || rol.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                rolAsignado = "Administrador";
                nombre = "Manuela Andrea Mosquera";
                if (string.IsNullOrWhiteSpace(email)) correo = "manuela.mosqueraar@amigo.edu.co";
                claims.Add(new Claim(ClaimTypes.Name, nombre));
                claims.Add(new Claim(ClaimTypes.Email, correo));
                claims.Add(new Claim(ClaimTypes.Role, rolAsignado));
            }
            else if (lower == "juan.padillamo@amigo.edu.co" || lower.Contains("presid") || rol.Equals("Presidenta", StringComparison.OrdinalIgnoreCase) || rol.Equals("Presidente", StringComparison.OrdinalIgnoreCase))
            {
                rolAsignado = "Presidenta";
                nombre = "Juan Padilla";
                if (string.IsNullOrWhiteSpace(email)) correo = "juan.padillamo@amigo.edu.co";
                claims.Add(new Claim(ClaimTypes.Name, nombre));
                claims.Add(new Claim(ClaimTypes.Email, correo));
                claims.Add(new Claim(ClaimTypes.Role, rolAsignado));
            }
            else if (lower == "karen.argumedoco@amigo.edu.co" || lower.Contains("secret") || rol.Equals("Secretaria", StringComparison.OrdinalIgnoreCase) || rol.Equals("Secretaría", StringComparison.OrdinalIgnoreCase))
            {
                rolAsignado = "Secretaria";
                nombre = "Karen Argumedo";
                if (string.IsNullOrWhiteSpace(email)) correo = "karen.argumedoco@amigo.edu.co";
                claims.Add(new Claim(ClaimTypes.Name, nombre));
                claims.Add(new Claim(ClaimTypes.Email, correo));
                claims.Add(new Claim(ClaimTypes.Role, rolAsignado));
            }
            else if (lower.Contains("pdv") || lower.Contains("pos") || lower.Contains("venta") || rol.Equals("AdministradorPdv", StringComparison.OrdinalIgnoreCase) || rol.Equals("PuntoVenta", StringComparison.OrdinalIgnoreCase))
            {
                rolAsignado = "AdministradorPdv";
                nombre = "Administrador Punto de Venta";
                if (string.IsNullOrWhiteSpace(email)) correo = "pdv@asoagro-faaua.org";
                claims.Add(new Claim(ClaimTypes.Name, nombre));
                claims.Add(new Claim(ClaimTypes.Email, correo));
                claims.Add(new Claim(ClaimTypes.Role, rolAsignado));
            }
            else
            {
                var parteLocal = correo.Split('@')[0].Replace('.', ' ');
                nombre = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parteLocal);
                rolAsignado = "Secretaria";
                claims.Add(new Claim(ClaimTypes.Name, nombre));
                claims.Add(new Claim(ClaimTypes.Email, correo));
                claims.Add(new Claim(ClaimTypes.Role, rolAsignado));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            if (rolAsignado == "Administrador")
            {
                return LocalRedirect("/panel-administrador");
            }
            if (rolAsignado == "Tesorero")
            {
                return LocalRedirect("/panel-tesorero");
            }
            if (rolAsignado == "Secretaria")
            {
                return LocalRedirect("/consultar-asociados");
            }
            if (rolAsignado == "Presidenta")
            {
                return LocalRedirect("/actas-estatutos");
            }
            if (rolAsignado == "AdministradorPdv")
            {
                return LocalRedirect("/punto-de-venta");
            }

            return LocalRedirect("/");
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/");
        }
    }
}
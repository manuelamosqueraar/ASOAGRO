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
        [HttpGet("login-google")]
        public IActionResult LoginGoogle([FromQuery] string returnUrl = "/")
        {
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
                if (rolUsuario == "Tesorero")
                {
                    return LocalRedirect("/registrar-documentos");
                }

                if (rolUsuario == "Director" || rolUsuario == "Presidente")
                {
                    // Los mandamos directo al panel principal de gestión donde SÍ sale el menú lateral completo
                    return LocalRedirect("/consultar-asociados");
                }
                else if (rolUsuario == "Invitado")
                {
                    // Si es invitado, lo podemos mandar a una vista general o donde prefieras
                    return LocalRedirect("/consultar-asociados");
                }
            }

            // Si viene con otra URL específica, respeta la ruta
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl != "/")
            {
                return LocalRedirect(returnUrl);
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
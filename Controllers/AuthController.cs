using Asoagro.Services;
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
        private readonly AdminSistemaService adminSistemaService;

        public AuthController(IConfiguration configuration, AdminSistemaService adminSistemaService)
        {
            this.configuration = configuration;
            this.adminSistemaService = adminSistemaService;
        }

        [HttpGet("login-google")]
        public IActionResult LoginGoogle([FromQuery] string returnUrl = "")
        {
            if (string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]))
            {
                return Problem(
                    "Google OAuth no está configurado. Define Authentication__Google__ClientSecret como variable de entorno.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var returnUrlSeguro = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : string.Empty;

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback", "Auth", new { returnUrl = returnUrlSeguro })
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback(string returnUrl = "")
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null)
            {
                return Redirect("/login?error=auth_failed");
            }

            var rolBruto = result.Principal.FindFirstValue(ClaimTypes.Role);
            var rolCanonica = AuthRoleHelper.NormalizarRolCanonica(rolBruto);
            if (string.IsNullOrWhiteSpace(rolCanonica))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Redirect("/login?error=no_role");
            }

            var emailGoogle = result.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            if (adminSistemaService.RequiereCambioContrasena(emailGoogle))
            {
                return LocalRedirect("/recuperar?primerAcceso=true");
            }

            return RedirigirSegunRolYReturnUrl(rolCanonica, returnUrl);
        }

        [HttpGet("login-local")]
        public async Task<IActionResult> LoginLocal(
            [FromQuery] string rol = "",
            [FromQuery] string email = "",
            [FromQuery] string password = "",
            [FromQuery] bool rememberMe = false,
            [FromQuery] string returnUrl = "")
        {
            var ipTerminal = HttpContext.Connection.RemoteIpAddress?.ToString();
            var correo = !string.IsNullOrWhiteSpace(email) ? email.Trim() : "secretaria@asoagro-faaua.org";
            var lower = correo.ToLowerInvariant();
            if (lower == "admin" || lower == "administrador" || lower == "admin@asoagro.org")
            {
                correo = "admin@asoagro-faaua.org";
                lower = correo;
            }

            var nombre = string.Empty;
            string? rolAsignado = null;
            var claims = new List<Claim>();

            var usuarioBd = await adminSistemaService.BuscarUsuarioPorCorreoAsync(correo);
            if (lower == "admin@asoagro-faaua.org" && usuarioBd == null)
            {
                await adminSistemaService.AsegurarCuentasYAdminInicialAsync();
                usuarioBd = await adminSistemaService.BuscarUsuarioPorCorreoAsync(correo);
            }

            if (lower == "admin@asoagro-faaua.org" && (usuarioBd == null || string.IsNullOrWhiteSpace(usuarioBd.ContrasenaHash)))
            {
                await adminSistemaService.RegistrarAuditoriaAsync(
                    "Administrador del Sistema ASOAGRO",
                    "admin@asoagro-faaua.org",
                    AuthRoleHelper.RolAdministrador,
                    "Autenticación",
                    "Inicio de sesión rechazado (contraseña inicial no configurada)",
                    "admin@asoagro-faaua.org",
                    "Denegado",
                    "La cuenta de Administrador aún no tiene una contraseña inicial configurada en entorno o secretos.",
                    ipTerminal,
                    usuarioBd?.Id,
                    usuarioBd?.Id);
                return Redirect("/login?error=admin_unconfigured");
            }

            if (usuarioBd != null)
            {
                if (!usuarioBd.Activo)
                {
                    await adminSistemaService.RegistrarAuditoriaAsync(
                        usuarioBd.NombreCompleto,
                        usuarioBd.Correo,
                        usuarioBd.Rol,
                        "Autenticación",
                        "Inicio de sesión rechazado (cuenta desactivada)",
                        usuarioBd.Correo,
                        "Denegado",
                        "Intento de ingreso sobre una cuenta desactivada por el Administrador.",
                        ipTerminal,
                        usuarioBd.Id,
                        usuarioBd.Id);
                    return Redirect("/login?error=account_disabled");
                }

                if (!string.IsNullOrWhiteSpace(usuarioBd.ContrasenaHash) &&
                    !adminSistemaService.VerificarPasswordHash(usuarioBd, password ?? string.Empty))
                {
                    await adminSistemaService.RegistrarAuditoriaAsync(
                        usuarioBd.NombreCompleto,
                        usuarioBd.Correo,
                        usuarioBd.Rol,
                        "Autenticación",
                        "Inicio de sesión fallido (credenciales inválidas)",
                        usuarioBd.Correo,
                        "Denegado",
                        "Contraseña incorrecta en inicio de sesión local.",
                        ipTerminal,
                        usuarioBd.Id,
                        usuarioBd.Id);
                    return Redirect("/login?error=invalid_credentials");
                }

                nombre = usuarioBd.NombreCompleto;
                correo = usuarioBd.Correo;
                rolAsignado = AuthRoleHelper.NormalizarRolCanonica(usuarioBd.Rol);
            }
            else
            {
                rolAsignado = AuthRoleHelper.ResolverRolPorCorreoFallback(correo, rol);
                if (!string.IsNullOrWhiteSpace(rolAsignado))
                {
                    nombre = rolAsignado switch
                    {
                        AuthRoleHelper.RolSecretaria => "Manuela Andrea Mosquera",
                        AuthRoleHelper.RolPresidenta => "Manuela Mosquera",
                        AuthRoleHelper.RolTesorero => "Juan Padilla",
                        AuthRoleHelper.RolAdministradorPdv => "Karen Argumedo",
                        AuthRoleHelper.RolAdministrador => "Administrador del Sistema ASOAGRO",
                        _ => correo
                    };

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        correo = rolAsignado switch
                        {
                            AuthRoleHelper.RolSecretaria => "manuela.mosqueraar@amigo.edu.co",
                            AuthRoleHelper.RolPresidenta => "manumosque743@gmail.com",
                            AuthRoleHelper.RolTesorero => "juan.padillamo@amigo.edu.co",
                            AuthRoleHelper.RolAdministradorPdv => "karen.argumedoco@amigo.edu.co",
                            AuthRoleHelper.RolAdministrador => "admin@asoagro-faaua.org",
                            _ => correo
                        };
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(rolAsignado))
            {
                await adminSistemaService.RegistrarAuditoriaAsync(
                    string.IsNullOrWhiteSpace(nombre) ? correo : nombre,
                    correo,
                    "Sin rol",
                    "Autenticación",
                    "Inicio de sesión rechazado (usuario o rol no reconocido)",
                    correo,
                    "Denegado",
                    "La cuenta no existe en base de datos ni posee un rol válido asignado.",
                    ipTerminal,
                    usuarioBd?.Id,
                    usuarioBd?.Id);
                return Redirect(usuarioBd != null ? "/login?error=no_role" : "/login?error=invalid_credentials");
            }

            var requiereCambioClave = adminSistemaService.RequiereCambioContrasena(correo);
            claims.Add(new Claim(ClaimTypes.Name, nombre));
            claims.Add(new Claim(ClaimTypes.Email, correo));
            claims.Add(new Claim(ClaimTypes.Role, rolAsignado));
            claims.Add(new Claim("auth_iat", DateTime.UtcNow.Ticks.ToString()));
            if (requiereCambioClave)
            {
                claims.Add(new Claim("must_change_password", "true"));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authProps = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(14) : null
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);
            await adminSistemaService.RegistrarAccesoUsuarioAsync(correo);
            await adminSistemaService.RegistrarAuditoriaAsync(
                nombre,
                correo,
                rolAsignado,
                "Autenticación",
                "Inicio de sesión",
                correo,
                "Autorizado",
                $"Sesión iniciada mediante formulario de acceso ({rolAsignado}).",
                ipTerminal,
                usuarioBd?.Id,
                usuarioBd?.Id);

            if (requiereCambioClave)
            {
                return LocalRedirect("/recuperar?primerAcceso=true");
            }

            return RedirigirSegunRolYReturnUrl(rolAsignado, returnUrl);
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("refrescar-sesion")]
        public async Task<IActionResult> RefrescarSesion([FromQuery] string returnUrl = "")
        {
            var correo = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(correo))
            {
                return Redirect("/login");
            }

            if (adminSistemaService.RequiereCambioContrasena(correo))
            {
                return LocalRedirect("/recuperar?primerAcceso=true");
            }

            var usuarioBd = await adminSistemaService.BuscarUsuarioPorCorreoAsync(correo);
            var rolCanonica = AuthRoleHelper.NormalizarRolCanonica(usuarioBd?.Rol ?? User.FindFirstValue(ClaimTypes.Role));
            if (string.IsNullOrWhiteSpace(rolCanonica))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Redirect("/login?error=no_role");
            }

            var nombre = usuarioBd?.NombreCompleto ?? User.Identity?.Name ?? correo;
            var nuevasClaims = new List<Claim>
            {
                new(ClaimTypes.Name, nombre),
                new(ClaimTypes.Email, correo),
                new(ClaimTypes.Role, rolCanonica),
                new("auth_iat", DateTime.UtcNow.Ticks.ToString())
            };

            var picture = User.FindFirstValue("picture");
            if (!string.IsNullOrWhiteSpace(picture))
            {
                nuevasClaims.Add(new Claim("picture", picture));
            }

            var identity = new ClaimsIdentity(nuevasClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return RedirigirSegunRolYReturnUrl(rolCanonica, returnUrl);
        }

        private IActionResult RedirigirSegunRolYReturnUrl(string rolCanonica, string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl)
                && Url.IsLocalUrl(returnUrl)
                && AuthRoleHelper.EsReturnUrlPermitidaParaRol(rolCanonica, returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            var panelDestino = AuthRoleHelper.ObtenerPanelPorRol(rolCanonica);
            if (!string.IsNullOrWhiteSpace(panelDestino))
            {
                return LocalRedirect(panelDestino);
            }

            return Redirect("/login?error=no_role");
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            var nombre = User.Identity?.Name ?? "Usuario";
            var correo = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var rol = User.FindFirstValue(ClaimTypes.Role) ?? "Sin rol";
            var ipTerminal = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (User.Identity?.IsAuthenticated == true)
            {
                await adminSistemaService.RegistrarAuditoriaAsync(
                    nombre,
                    correo,
                    rol,
                    "Autenticación",
                    "Cierre de sesión",
                    correo,
                    "Autorizado",
                    "El usuario cerró sesión voluntariamente.",
                    ipTerminal);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            return Redirect("/login");
        }
    }
}
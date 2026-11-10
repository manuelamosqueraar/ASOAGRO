using System.Security.Claims;

namespace Asoagro.Services;

/// <summary>
/// Centraliza la normalización de roles autenticados, las rutas iniciales por rol
/// y la validación de permisos para URLs de retorno tras el inicio de sesión.
/// </summary>
public static class AuthRoleHelper
{
    public const string RolAdministrador = "Administrador";
    public const string RolPresidenta = "Presidenta";
    public const string RolSecretaria = "Secretaria";
    public const string RolTesorero = "Tesorero";
    public const string RolAdministradorPdv = "AdministradorPdv";

    /// <summary>
    /// Normaliza variantes y mayúsculas/minúsculas de rol (DB, OAuth, claims)
    /// al nombre canónico esperado por los atributos [Authorize(Roles = ...)].
    /// </summary>
    public static string? NormalizarRolCanonica(string? rawRol)
    {
        if (string.IsNullOrWhiteSpace(rawRol))
        {
            return null;
        }

        return rawRol.Trim().ToLowerInvariant() switch
        {
            "administrador" or "admin" or "administrador_sistema" or "administrador del sistema" or "superadmin"
                => RolAdministrador,
            "presidenta" or "presidente" or "directora" or "director" or "presidencia"
                => RolPresidenta,
            "secretaria" or "secretaría" or "secretaria general" or "secretaría general"
                => RolSecretaria,
            "tesorero" or "tesorera" or "tesoreria" or "tesorería"
                => RolTesorero,
            "administradorpdv" or "administrador_pdv" or "administrador pdv" or "administrador punto de venta"
                or "puntoventa" or "punto_venta" or "punto de venta" or "vendedor" or "pdv" or "pos"
                => RolAdministradorPdv,
            _ => null
        };
    }

    /// <summary>
    /// Resuelve el rol institucional cuando una cuenta aún no está registrada en base de datos,
    /// siguiendo la asignación oficial del equipo ASOAGRO.
    /// </summary>
    public static string? ResolverRolPorCorreoFallback(string? email, string? rolExplicito = null)
    {
        var rolDesdeParametro = NormalizarRolCanonica(rolExplicito);
        if (!string.IsNullOrWhiteSpace(rolDesdeParametro))
        {
            return rolDesdeParametro;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var lower = email.Trim().ToLowerInvariant();

        if (lower is "admin@asoagro-faaua.org" or "admin@asoagro.com")
        {
            return RolAdministrador;
        }

        if (lower is "manuela.mosqueraar@amigo.edu.co" or "secretaria@asoagro-faaua.org")
        {
            return RolSecretaria;
        }

        if (lower is "manumosque743@gmail.com" or "presidenta@asoagro.com")
        {
            return RolPresidenta;
        }

        if (lower is "juan.padillamo@amigo.edu.co" or "tesoreria@asoagro-faaua.org")
        {
            return RolTesorero;
        }

        if (lower is "karen.argumedoco@amigo.edu.co" or "pdv@asoagro-faaua.org" or "puntoventa@asoagro.com"
            || lower.Contains("pdv") || lower.Contains("pos"))
        {
            return RolAdministradorPdv;
        }

        if (lower.Contains("tesor"))
        {
            return RolTesorero;
        }

        if (lower.Contains("presid") || lower.Contains("direct"))
        {
            return RolPresidenta;
        }

        if (lower.Contains("secret"))
        {
            return RolSecretaria;
        }

        if (lower.Contains("venta"))
        {
            return RolAdministradorPdv;
        }

        return null;
    }

    /// <summary>
    /// Devuelve la ruta del panel principal correspondiente al rol canónico autenticado.
    /// </summary>
    public static string? ObtenerPanelPorRol(string? rol)
    {
        var canonico = NormalizarRolCanonica(rol);
        return canonico switch
        {
            RolPresidenta => "/actas-estatutos",
            RolSecretaria => "/consultar-asociados",
            RolTesorero => "/panel-tesorero",
            RolAdministradorPdv => "/punto-de-venta",
            RolAdministrador => "/panel-administrador",
            _ => null
        };
    }

    public static string ObtenerPanelPorUsuario(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return "/login";
        }

        foreach (var claim in user.FindAll(ClaimTypes.Role))
        {
            var panel = ObtenerPanelPorRol(claim.Value);
            if (!string.IsNullOrWhiteSpace(panel))
            {
                return panel;
            }
        }

        return "/login?error=no_role";
    }

    /// <summary>
    /// Determina si una URL de retorno solicitada antes del login es un destino de panel/módulo válido
    /// y si el rol autenticado tiene permiso para acceder a ella.
    /// </summary>
    public static bool EsReturnUrlPermitidaParaRol(string? rol, string? returnUrl)
    {
        var canonico = NormalizarRolCanonica(rol);
        if (string.IsNullOrWhiteSpace(canonico) || string.IsNullOrWhiteSpace(returnUrl))
        {
            return false;
        }

        var pathSinQuery = returnUrl.Split('?', '#')[0].Trim().TrimStart('/').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(pathSinQuery)
            || pathSinQuery is "login" or "access-denied" or "recuperar" or "registro" or "not-found" or "error" or "home")
        {
            return false;
        }

        if (pathSinQuery.StartsWith("auth/", StringComparison.Ordinal))
        {
            return false;
        }

        if (pathSinQuery.StartsWith("panel-administrador", StringComparison.Ordinal) || pathSinQuery.StartsWith("admin/", StringComparison.Ordinal))
        {
            return canonico == RolAdministrador;
        }

        if (pathSinQuery.StartsWith("subir-actas-estatutos", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("actas-estatutos/documentos", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("actas-estatutos/registrar", StringComparison.Ordinal))
        {
            return canonico is RolSecretaria or RolPresidenta or RolAdministrador;
        }

        if (pathSinQuery.StartsWith("actas-estatutos", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("presidencia", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("panel-presidencia", StringComparison.Ordinal))
        {
            return canonico is RolPresidenta or RolAdministrador;
        }

        if (pathSinQuery.StartsWith("panel-tesorero", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("panel-tesorera", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("gestion-financiera", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("tesoreria", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("finanzas", StringComparison.Ordinal))
        {
            return canonico is RolTesorero or RolPresidenta or RolAdministrador;
        }

        if (pathSinQuery.StartsWith("punto-de-venta", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("panel-punto-venta", StringComparison.Ordinal))
        {
            return canonico is RolAdministradorPdv or RolPresidenta or RolTesorero or RolAdministrador;
        }

        if (pathSinQuery.StartsWith("asociaciones", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("directorio-asociaciones", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("consultar-asociados", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("registrar-asociado", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("editar-asociado", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("registrar-documentos", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("expediente-documental", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("consultar-estado-documentos", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("alertas-documentos", StringComparison.Ordinal)
            || pathSinQuery.StartsWith("seguimiento-asociados", StringComparison.Ordinal))
        {
            return canonico is RolSecretaria or RolPresidenta or RolAdministrador;
        }

        return false;
    }
}

namespace Asoagro.Models;

public class ErrorTecnicoItem
{
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public string Nivel { get; set; } = "Error";
    public string Categoria { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}

public class EstadoIntegracionItem
{
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = "Pendiente de configurar";
    public bool Operativo { get; set; }
    public string Detalle { get; set; } = string.Empty;
    public DateTime? UltimaVerificacionUtc { get; set; }
    public string? UltimoError { get; set; }
}

public class ConteoTablaSistema
{
    public string Tabla { get; set; } = string.Empty;
    public int? Registros { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class EstadoSistemaDto
{
    public DateTime FechaRevisionUtc { get; set; } = DateTime.UtcNow;
    public string Entorno { get; set; } = string.Empty;
    public string RuntimeDotNet { get; set; } = string.Empty;
    public string SistemaOperativo { get; set; } = string.Empty;
    public DateTime InicioAplicacionUtc { get; set; }
    public TimeSpan TiempoActivo { get; set; }

    public bool SupabaseConfigurado { get; set; }
    public bool SupabaseConectado { get; set; }
    public long? SupabaseLatenciaMs { get; set; }
    public string SupabaseMensaje { get; set; } = string.Empty;
    public DateTime? UltimoIntentoSupabaseUtc { get; set; }

    public bool AlmacenamientoLocalDisponible { get; set; }
    public string AlmacenamientoLocalDetalle { get; set; } = string.Empty;

    public bool GoogleOAuthConfigurado { get; set; }
    public string GoogleOAuthDetalle { get; set; } = string.Empty;

    public bool SmtpConfigurado { get; set; }
    public bool RespaldoAutomaticoConfigurado { get; set; }
    public string RespaldoDetalle { get; set; } = "No disponible: no hay un mecanismo automático de respaldo conectado en la aplicación.";

    public List<ConteoTablaSistema> TablasSupabase { get; set; } = new();
    public List<EstadoIntegracionItem> Integraciones { get; set; } = new();
    public List<ErrorTecnicoItem> ErroresRecientes { get; set; } = new();
}

public class UsuarioAdminDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string DocumentoId { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Rol { get; set; } = "Secretaria";
    public int? AsociacionId { get; set; }
    public string AsociacionNombre { get; set; } = "Federación FAAUA (Global)";
    public bool Activo { get; set; } = true;
    public bool TieneContrasenaLocal { get; set; }
    public bool RecuperacionPendiente { get; set; }
    public DateTime? UltimoAccesoUtc { get; set; }
    public DateTime? SesionRevocadaEnUtc { get; set; }
    public DateTime CreadoEnUtc { get; set; } = DateTime.UtcNow;
}

public class SolicitudUsuarioAdmin
{
    public int? Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string DocumentoId { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Rol { get; set; } = "Secretaria";
    public int? AsociacionId { get; set; }
    public bool Activo { get; set; } = true;
    public string? ContrasenaNueva { get; set; }
}

public class AuditoriaEventoDto
{
    public long Id { get; set; }
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public int? UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string UsuarioCorreo { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string RegistroAfectado { get; set; } = string.Empty;
    public string Resultado { get; set; } = "Autorizado";
    public string IpTerminal { get; set; } = "No disponible";
    public string Detalle { get; set; } = string.Empty;
    public string FuenteDatos { get; set; } = string.Empty;
}

public class FiltroAuditoria
{
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public string? TextoBusqueda { get; set; }
    public string? Rol { get; set; }
    public string? Modulo { get; set; }
    public string? Accion { get; set; }
    public string? Resultado { get; set; }
}

public class ResultadoOperacionAdmin
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

public class ParametroSistemaItem
{
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public bool Editable { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string? UltimoCambioPor { get; set; }
    public DateTime? UltimoCambioUtc { get; set; }
}

public class EstadoRecuperacionUsuario
{
    public string Correo { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiraEnUtc { get; set; }
    public string SolicitadoPor { get; set; } = string.Empty;
}


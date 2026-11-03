namespace Asoagro.Models;

public enum CategoriaNotificacion
{
    Productores,
    Documentos,
    Seguimiento
}

public enum NivelNotificacion
{
    Informativa,
    Advertencia,
    Urgente
}

// Aviso que aparece en la campana y, si llega en vivo, como aviso emergente.
public class Notificacion
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime FechaCreacion { get; } = DateTime.Now;

    public string Titulo { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;

    // Texto corto encima del aviso: "Registro", "Edición", "Por vencer", "Vencido"
    public string Etiqueta { get; init; } = "Aviso";

    // Página a la que lleva el aviso al pulsarlo
    public string Enlace { get; init; } = "/";

    // Nombre de un símbolo del sprite de iconos del layout: alerta, usuario-mas, ajustes, campana
    public string Icono { get; init; } = "campana";

    public CategoriaNotificacion Categoria { get; init; } = CategoriaNotificacion.Productores;
    public NivelNotificacion Nivel { get; init; } = NivelNotificacion.Informativa;

    // Roles que pueden ver el aviso. Si la lista está vacía, lo ve cualquier usuario que inició sesión.
    public IReadOnlyList<string> RolesDestino { get; init; } = Array.Empty<string>();
        // Roles que pueden abrir el enlace del aviso. Si la lista está vacía, lo puede abrir cualquier usuario con sesión.
    public IReadOnlyList<string> RolesEnlace { get; init; } = Array.Empty<string>();

    // Evita repetir el mismo aviso. Ejemplo: un documento que ya avisó que vence en 7 días.
    public string? ClaveUnica { get; init; }
}
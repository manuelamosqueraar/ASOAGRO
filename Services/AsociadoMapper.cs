using Asoagro.Models;

namespace Asoagro.Services;

public static class AsociadoMapper
{
    public static AsociadoModel ToModel(Asociado asociado) => new()
    {
        Id = asociado.Id,
        NombreCompleto = asociado.NombreCompleto,
        Cedula = asociado.Cedula,
        Telefono = asociado.Telefono ?? string.Empty,
        Correo = asociado.Correo ?? string.Empty,
        NombrePredio = string.IsNullOrWhiteSpace(asociado.NombrePredio)
            ? (asociado.FincaVereda ?? string.Empty)
            : asociado.NombrePredio,
        VeredaSector = string.IsNullOrWhiteSpace(asociado.VeredaSector)
            ? (asociado.FincaVereda ?? string.Empty)
            : asociado.VeredaSector,
        Municipio = asociado.Municipio ?? string.Empty,
        HectareasPredio = Convert.ToDouble(asociado.HectareasPredio ?? 0),
        CultivoPrincipal = asociado.CultivoPrincipal,
        TieneBPA = asociado.TieneBPA,
        EsVictima = asociado.EsVictima ? "Si" : "No",
        EstadoMembresia = string.IsNullOrWhiteSpace(asociado.EstadoMembresia) || asociado.EstadoMembresia == "Activo"
            ? "AlDia"
            : asociado.EstadoMembresia,
        FechaRegistro = asociado.FechaRegistro
    };

    public static Asociado ToEntity(AsociadoModel modelo) => new()
    {
        Id = modelo.Id,
        Cedula = modelo.Cedula,
        NombreCompleto = modelo.NombreCompleto,
        Telefono = modelo.Telefono,
        Correo = modelo.Correo,
        NombrePredio = modelo.NombrePredio,
        VeredaSector = modelo.VeredaSector,
        Municipio = modelo.Municipio,
        HectareasPredio = Convert.ToDecimal(modelo.HectareasPredio),
        FincaVereda = string.IsNullOrWhiteSpace(modelo.NombrePredio)
            ? modelo.VeredaSector
            : modelo.NombrePredio,
        CultivoPrincipal = modelo.CultivoPrincipal,
        TieneBPA = modelo.TieneBPA,
        EsVictima = string.Equals(modelo.EsVictima, "Si", StringComparison.OrdinalIgnoreCase),
        EstadoMembresia = modelo.EstadoMembresia,
        FechaRegistro = modelo.FechaRegistro
    };
}

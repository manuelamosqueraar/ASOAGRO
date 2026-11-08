using System.Collections.Concurrent;
using Asoagro.Models;

namespace Asoagro.Services;

public static class AsociadoMapper
{
    private sealed class MetadatosExtendidosAsociado
    {
        public string TipoDocumentoIdentidad { get; set; } = "CC";
        public string AsociacionPertenece { get; set; } = string.Empty;
        public string TenenciaTerreno { get; set; } = string.Empty;
        public string TipoActividadProductiva { get; set; } = string.Empty;
        public string ProduccionAproximada { get; set; } = string.Empty;
        public string CedulaArchivoNombre { get; set; } = string.Empty;
        public string CedulaArchivoTamano { get; set; } = string.Empty;
        public string CedulaArchivoUrl { get; set; } = string.Empty;
        public double HectareasLineaProductiva { get; set; }
        public List<string> CultivosSeleccionados { get; set; } = new();
        public string CertificadoBpaNombre { get; set; } = string.Empty;
        public string CertificadoBpaTamano { get; set; } = string.Empty;
        public string CertificadoBpaUrl { get; set; } = string.Empty;
        public List<string> GruposVulnerables { get; set; } = new();
        public Dictionary<string, string> SoportesGruposVulnerables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> SoportesGruposUrls { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<CuotaAporteAsociado> CuotasAportes { get; set; } = new();
        public List<AfiliacionAsociado> Afiliaciones { get; set; } = new();
        public bool AutorizacionDatosPersonales { get; set; } = true;
        public DateTime? FechaAutorizacionHabeasData { get; set; }
    }

    private static readonly ConcurrentDictionary<string, MetadatosExtendidosAsociado> CacheMetadatosPorCedula =
        new(StringComparer.OrdinalIgnoreCase);

    public static AsociadoModel ToModel(Asociado asociado)
    {
        var cedulaKey = (asociado.Cedula ?? string.Empty).Trim();
        CacheMetadatosPorCedula.TryGetValue(cedulaKey, out var meta);

        var cultivos = asociado.CultivosSeleccionados.Count > 0
            ? new List<string>(asociado.CultivosSeleccionados)
            : meta?.CultivosSeleccionados.Count > 0
                ? new List<string>(meta.CultivosSeleccionados)
                : AsociadoModel.DesglosarCultivos(asociado.CultivoPrincipal);

        var hectareasPredio = decimal.ToDouble(asociado.HectareasPredio ?? 0);
        var hectareasProductivas = asociado.HectareasLineaProductiva.HasValue
            ? decimal.ToDouble(asociado.HectareasLineaProductiva.Value)
            : meta is { HectareasLineaProductiva: > 0 }
                ? meta.HectareasLineaProductiva
                : hectareasPredio;

        var gruposVulnerables = asociado.GruposVulnerables.Count > 0
            ? new List<string>(asociado.GruposVulnerables)
            : meta?.GruposVulnerables.Count > 0
                ? new List<string>(meta.GruposVulnerables)
                : asociado.EsVictima
                    ? new List<string> { "VictimaConflicto" }
                    : new List<string>();

        var soportes = asociado.SoportesGruposVulnerables.Count > 0
            ? new Dictionary<string, string>(asociado.SoportesGruposVulnerables, StringComparer.OrdinalIgnoreCase)
            : meta?.SoportesGruposVulnerables.Count > 0
                ? new Dictionary<string, string>(meta.SoportesGruposVulnerables, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var soportesUrls = asociado.SoportesGruposUrls.Count > 0
            ? new Dictionary<string, string>(asociado.SoportesGruposUrls, StringComparer.OrdinalIgnoreCase)
            : meta?.SoportesGruposUrls.Count > 0
                ? new Dictionary<string, string>(meta.SoportesGruposUrls, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var certificadoBpa = !string.IsNullOrWhiteSpace(asociado.CertificadoBpaNombre)
            ? asociado.CertificadoBpaNombre!
            : meta?.CertificadoBpaNombre ?? string.Empty;

        var certificadoTamano = !string.IsNullOrWhiteSpace(asociado.CertificadoBpaTamano)
            ? asociado.CertificadoBpaTamano!
            : meta?.CertificadoBpaTamano ?? string.Empty;

        var certificadoUrl = !string.IsNullOrWhiteSpace(asociado.CertificadoBpaUrl)
            ? asociado.CertificadoBpaUrl!
            : meta?.CertificadoBpaUrl ?? string.Empty;

        var tenenciaTerreno = !string.IsNullOrWhiteSpace(asociado.TenenciaTerreno)
            ? asociado.TenenciaTerreno!
            : meta?.TenenciaTerreno ?? string.Empty;

        var tipoActividadProductiva = !string.IsNullOrWhiteSpace(asociado.TipoActividadProductiva)
            ? asociado.TipoActividadProductiva!
            : meta?.TipoActividadProductiva ?? string.Empty;

        var produccionAproximada = !string.IsNullOrWhiteSpace(asociado.ProduccionAproximada)
            ? asociado.ProduccionAproximada!
            : meta?.ProduccionAproximada ?? string.Empty;

        var cedulaArchivoNombre = !string.IsNullOrWhiteSpace(asociado.CedulaArchivoNombre)
            ? asociado.CedulaArchivoNombre!
            : meta?.CedulaArchivoNombre ?? string.Empty;

        var cedulaArchivoTamano = !string.IsNullOrWhiteSpace(asociado.CedulaArchivoTamano)
            ? asociado.CedulaArchivoTamano!
            : meta?.CedulaArchivoTamano ?? string.Empty;

        var cedulaArchivoUrl = !string.IsNullOrWhiteSpace(asociado.CedulaArchivoUrl)
            ? asociado.CedulaArchivoUrl!
            : meta?.CedulaArchivoUrl ?? string.Empty;

        var tipoDocumentoIdentidad = !string.IsNullOrWhiteSpace(asociado.TipoDocumentoIdentidad)
            ? asociado.TipoDocumentoIdentidad!
            : !string.IsNullOrWhiteSpace(meta?.TipoDocumentoIdentidad)
                ? meta.TipoDocumentoIdentidad
                : "CC";

        var cuotasAportes = asociado.CuotasAportes.Count > 0
            ? asociado.CuotasAportes.Select(c => c.Clonar()).ToList()
            : meta?.CuotasAportes.Count > 0
                ? meta.CuotasAportes.Select(c => c.Clonar()).ToList()
                : new List<CuotaAporteAsociado>();

        foreach (var cuota in cuotasAportes)
        {
            cuota.ActualizarEstadoYSoportes();
        }

        var cultivoTexto = cultivos.Count > 0
            ? string.Join(", ", cultivos)
            : asociado.CultivoPrincipal;

        var esVictima = asociado.EsVictima || gruposVulnerables.Count > 0;
        var asociacionPertenece = !string.IsNullOrWhiteSpace(asociado.AsociacionPertenece)
            ? asociado.AsociacionPertenece!
            : meta?.AsociacionPertenece ?? string.Empty;

        return new AsociadoModel
        {
            Id = asociado.Id,
            NombreCompleto = asociado.NombreCompleto,
            TipoDocumentoIdentidad = tipoDocumentoIdentidad,
            Cedula = cedulaKey,
            AsociacionPertenece = asociacionPertenece,
            Telefono = asociado.Telefono ?? string.Empty,
            Correo = asociado.Correo ?? string.Empty,
            CedulaArchivoNombre = cedulaArchivoNombre,
            CedulaArchivoTamano = cedulaArchivoTamano,
            CedulaArchivoUrl = cedulaArchivoUrl,
            NombrePredio = !string.IsNullOrWhiteSpace(asociado.NombrePredio)
                ? asociado.NombrePredio
                : ExtraerNombrePredio(asociado.FincaVereda),
            VeredaSector = !string.IsNullOrWhiteSpace(asociado.VeredaSector)
                ? asociado.VeredaSector
                : ExtraerVeredaSector(asociado.FincaVereda),
            Municipio = asociado.Municipio ?? string.Empty,
            TenenciaTerreno = tenenciaTerreno,
            HectareasPredio = hectareasPredio,
            HectareasLineaProductiva = hectareasProductivas,
            TipoActividadProductiva = tipoActividadProductiva,
            ProduccionAproximada = produccionAproximada,
            CultivoPrincipal = cultivoTexto,
            CultivosSeleccionados = cultivos,
            TieneBPA = asociado.TieneBPA,
            CertificadoBpaNombre = asociado.TieneBPA ? certificadoBpa : string.Empty,
            CertificadoBpaTamano = asociado.TieneBPA ? certificadoTamano : string.Empty,
            CertificadoBpaUrl = asociado.TieneBPA ? certificadoUrl : string.Empty,
            EsVictima = esVictima ? "Si" : "No",
            GruposVulnerables = gruposVulnerables,
            SoportesGruposVulnerables = soportes,
            SoportesGruposUrls = soportesUrls,
            CuotasAportes = cuotasAportes,
            Afiliaciones = asociado.Afiliaciones.Count > 0
                ? new List<AfiliacionAsociado>(asociado.Afiliaciones)
                : meta?.Afiliaciones.Count > 0
                    ? new List<AfiliacionAsociado>(meta.Afiliaciones)
                    : !string.IsNullOrWhiteSpace(asociacionPertenece)
                        ? new List<AfiliacionAsociado> { new AfiliacionAsociado { AsociadoId = asociado.Id, AsociadoCedula = cedulaKey, AsociacionNombre = asociacionPertenece, Estado = "Activa" } }
                        : new List<AfiliacionAsociado>(),
            AutorizacionDatosPersonales = asociado.AutorizacionDatosPersonales,
            FechaAutorizacionHabeasData = asociado.FechaAutorizacionHabeasData ?? meta?.FechaAutorizacionHabeasData ?? DateTime.Now,
            EstadoMembresia = string.IsNullOrWhiteSpace(asociado.EstadoMembresia)
                ? "AlDia"
                : asociado.EstadoMembresia,
            FechaRegistro = asociado.FechaRegistro
        };
    }

    public static Asociado ToEntity(AsociadoModel model, string usuarioActual = "Sistema")
    {
        model.SincronizarListaDesdeCultivoPrincipal();

        var fincaVereda = ConstruirFincaVereda(model.NombrePredio, model.VeredaSector, model.Municipio);
        var cedulaLimpia = model.Cedula.Trim();
        var tieneCondicionVulnerable = string.Equals(model.EsVictima, "Si", StringComparison.OrdinalIgnoreCase)
            || model.GruposVulnerables.Count > 0;

        var cultivoParaBd = string.IsNullOrWhiteSpace(model.CultivoPrincipal)
            ? string.Empty
            : model.CultivoPrincipal.Trim();

        if (cultivoParaBd.Length > 50)
        {
            cultivoParaBd = cultivoParaBd[..50].TrimEnd(',', ' ');
        }

        var cuotasClonadas = model.CuotasAportes.Select(c =>
        {
            var copia = c.Clonar();
            copia.ActualizarEstadoYSoportes();
            return copia;
        }).ToList();

        if (!string.IsNullOrWhiteSpace(cedulaLimpia))
        {
            CacheMetadatosPorCedula[cedulaLimpia] = new MetadatosExtendidosAsociado
            {
                TipoDocumentoIdentidad = string.IsNullOrWhiteSpace(model.TipoDocumentoIdentidad) ? "CC" : model.TipoDocumentoIdentidad.Trim(),
                AsociacionPertenece = model.AsociacionPertenece.Trim(),
                TenenciaTerreno = model.TenenciaTerreno.Trim(),
                TipoActividadProductiva = model.TipoActividadProductiva.Trim(),
                ProduccionAproximada = model.ProduccionAproximada.Trim(),
                CedulaArchivoNombre = model.CedulaArchivoNombre.Trim(),
                CedulaArchivoTamano = model.CedulaArchivoTamano.Trim(),
                CedulaArchivoUrl = model.CedulaArchivoUrl.Trim(),
                HectareasLineaProductiva = model.HectareasLineaProductiva > 0
                    ? model.HectareasLineaProductiva
                    : model.HectareasPredio,
                CultivosSeleccionados = new List<string>(model.CultivosSeleccionados),
                CertificadoBpaNombre = model.TieneBPA ? model.CertificadoBpaNombre : string.Empty,
                CertificadoBpaTamano = model.TieneBPA ? model.CertificadoBpaTamano : string.Empty,
                CertificadoBpaUrl = model.TieneBPA ? model.CertificadoBpaUrl : string.Empty,
                GruposVulnerables = new List<string>(model.GruposVulnerables),
                SoportesGruposVulnerables = new Dictionary<string, string>(
                    model.SoportesGruposVulnerables,
                    StringComparer.OrdinalIgnoreCase),
                SoportesGruposUrls = new Dictionary<string, string>(
                    model.SoportesGruposUrls,
                    StringComparer.OrdinalIgnoreCase),
                CuotasAportes = cuotasClonadas.Select(c => c.Clonar()).ToList(),
                Afiliaciones = new List<AfiliacionAsociado>(model.Afiliaciones),
                AutorizacionDatosPersonales = model.AutorizacionDatosPersonales,
                FechaAutorizacionHabeasData = model.FechaAutorizacionHabeasData
            };
        }

        return new Asociado
        {
            Id = model.Id,
            TipoDocumentoIdentidad = string.IsNullOrWhiteSpace(model.TipoDocumentoIdentidad) ? "CC" : model.TipoDocumentoIdentidad.Trim(),
            AsociacionPertenece = LimpiarTexto(model.AsociacionPertenece),
            Afiliaciones = new List<AfiliacionAsociado>(model.Afiliaciones),
            AutorizacionDatosPersonales = model.AutorizacionDatosPersonales,
            FechaAutorizacionHabeasData = model.FechaAutorizacionHabeasData,
            Cedula = cedulaLimpia,
            NombreCompleto = model.NombreCompleto.Trim(),
            Telefono = LimpiarTexto(model.Telefono),
            Correo = LimpiarTexto(model.Correo),
            CedulaArchivoNombre = LimpiarTexto(model.CedulaArchivoNombre),
            CedulaArchivoTamano = LimpiarTexto(model.CedulaArchivoTamano),
            CedulaArchivoUrl = LimpiarTexto(model.CedulaArchivoUrl),
            NombrePredio = LimpiarTexto(model.NombrePredio),
            VeredaSector = LimpiarTexto(model.VeredaSector),
            Municipio = LimpiarTexto(model.Municipio),
            TenenciaTerreno = LimpiarTexto(model.TenenciaTerreno),
            HectareasPredio = model.HectareasPredio > 0
                ? Convert.ToDecimal(model.HectareasPredio)
                : null,
            HectareasLineaProductiva = model.HectareasLineaProductiva > 0
                ? Convert.ToDecimal(model.HectareasLineaProductiva)
                : model.HectareasPredio > 0
                    ? Convert.ToDecimal(model.HectareasPredio)
                    : null,
            TipoActividadProductiva = LimpiarTexto(model.TipoActividadProductiva),
            ProduccionAproximada = LimpiarTexto(model.ProduccionAproximada),
            FincaVereda = fincaVereda,
            CultivoPrincipal = cultivoParaBd,
            CultivosSeleccionados = new List<string>(model.CultivosSeleccionados),
            TieneBPA = model.TieneBPA,
            CertificadoBpaNombre = model.TieneBPA ? LimpiarTexto(model.CertificadoBpaNombre) : null,
            CertificadoBpaTamano = model.TieneBPA ? LimpiarTexto(model.CertificadoBpaTamano) : null,
            CertificadoBpaUrl = model.TieneBPA ? LimpiarTexto(model.CertificadoBpaUrl) : null,
            EsVictima = tieneCondicionVulnerable,
            GruposVulnerables = new List<string>(model.GruposVulnerables),
            SoportesGruposVulnerables = new Dictionary<string, string>(
                model.SoportesGruposVulnerables,
                StringComparer.OrdinalIgnoreCase),
            SoportesGruposUrls = new Dictionary<string, string>(
                model.SoportesGruposUrls,
                StringComparer.OrdinalIgnoreCase),
            CuotasAportes = cuotasClonadas,
            EstadoMembresia = string.IsNullOrWhiteSpace(model.EstadoMembresia)
                ? "AlDia"
                : model.EstadoMembresia.Trim(),
            FechaRegistro = model.FechaRegistro == default ? DateTime.UtcNow : model.FechaRegistro,
            FechaActualizacion = DateTime.UtcNow,
            CreadoPor = usuarioActual,
            ActualizadoPor = usuarioActual
        };
    }

    private static string ConstruirFincaVereda(string nombrePredio, string veredaSector, string municipio)
    {
        var partes = new[]
        {
            LimpiarTexto(nombrePredio),
            LimpiarTexto(veredaSector),
            LimpiarTexto(municipio)
        }.Where(parte => !string.IsNullOrWhiteSpace(parte));

        return string.Join(" - ", partes);
    }

    private static string ExtraerNombrePredio(string fincaVereda)
    {
        if (string.IsNullOrWhiteSpace(fincaVereda))
        {
            return string.Empty;
        }

        var partes = fincaVereda.Split(" - ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return partes.Length > 0 ? partes[0] : fincaVereda;
    }

    private static string ExtraerVeredaSector(string fincaVereda)
    {
        if (string.IsNullOrWhiteSpace(fincaVereda))
        {
            return string.Empty;
        }

        var partes = fincaVereda.Split(" - ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return partes.Length > 1 ? string.Join(" - ", partes.Skip(1)) : string.Empty;
    }

    private static string? LimpiarTexto(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}

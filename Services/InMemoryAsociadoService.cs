using Asoagro.Configuration;
using Asoagro.Models;
using Microsoft.Extensions.Options;

namespace Asoagro.Services;

public class InMemoryAsociadoService : IAsociadoService
{
    private readonly List<Asociado> asociados = new()
    {
        new Asociado
        {
            Id = 1,
            Cedula = "10234567",
            NombreCompleto = "Carlos Julio Ramírez",
            AsociacionPertenece = "ASOAGRO - Asociación Agropecuaria de Urabá",
            FincaVereda = "Finca La Esperanza - Vereda El Silencio",
            Telefono = "3104567890",
            Correo = "carlos.ramirez@asoagro.com",
            CedulaArchivoNombre = "Cedula_CarlosRamirez_10234567.pdf",
            CedulaArchivoTamano = "310 KB",
            NombrePredio = "Finca La Esperanza",
            VeredaSector = "Vereda El Silencio",
            Municipio = "Apartado",
            TenenciaTerreno = "Propio",
            HectareasPredio = 6.5m,
            HectareasLineaProductiva = 5.2m,
            TipoActividadProductiva = "Agrícola",
            ProduccionAproximada = "18 toneladas/año",
            CultivoPrincipal = "Platano, Cacao",
            CultivosSeleccionados = new List<string> { "Platano", "Cacao" },
            TieneBPA = true,
            CertificadoBpaNombre = "Certificado_ICA_BPA_CarlosRamirez.pdf",
            CertificadoBpaTamano = "420 KB",
            EsVictima = true,
            GruposVulnerables = new List<string> { "VictimaConflicto" },
            SoportesGruposVulnerables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["VictimaConflicto"] = "Certificado_RUV_10234567.pdf"
            },
            CuotasAportes = new List<CuotaAporteAsociado>
            {
                new CuotaAporteAsociado
                {
                    Id = "cuota-carlos-1",
                    TipoCuota = "Afiliación",
                    Periodicidad = "Pago único",
                    ValorCuota = 60000m,
                    FechaVencimiento = DateTime.Today.AddMonths(-2),
                    CantidadCuotasPendientes = 0,
                    ValorTotalPendiente = 0m,
                    ValorTotalPagado = 60000m,
                    EstadoPago = "Pagado · Al día",
                    Soportes = new List<SoporteCuotaAsociado>
                    {
                        new SoporteCuotaAsociado
                        {
                            Id = "sop-carlos-1",
                            CuotaId = "cuota-carlos-1",
                            TipoCuota = "Afiliación",
                            Periodicidad = "Pago único",
                            ArchivoNombre = "Comprobante_Afiliacion_CarlosRamirez.pdf",
                            ArchivoTamano = "240 KB"
                        }
                    }
                },
                new CuotaAporteAsociado
                {
                    Id = "cuota-carlos-2",
                    TipoCuota = "Sostenimiento",
                    Periodicidad = "Mensual",
                    ValorCuota = 35000m,
                    FechaVencimiento = DateTime.Today.AddDays(18),
                    CantidadCuotasPendientes = 0,
                    ValorTotalPendiente = 0m,
                    ValorTotalPagado = 70000m,
                    PagadoEditadoManualmente = true,
                    EstadoPago = "Pagado · Al día",
                    Soportes = new List<SoporteCuotaAsociado>
                    {
                        new SoporteCuotaAsociado
                        {
                            Id = "sop-carlos-2",
                            CuotaId = "cuota-carlos-2",
                            TipoCuota = "Sostenimiento",
                            Periodicidad = "Mensual",
                            ArchivoNombre = "Recibo_Sostenimiento_Octubre_CarlosRamirez.pdf",
                            ArchivoTamano = "195 KB"
                        }
                    }
                }
            },
            Afiliaciones = new List<AfiliacionAsociado>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AsociadoId = 1,
                    AsociadoCedula = "10234567",
                    AsociacionNombre = "ASOAGRO - Asociación Agropecuaria de Urabá",
                    FechaSolicitud = DateTime.Today.AddMonths(-6),
                    FechaAprobacion = DateTime.Today.AddMonths(-6),
                    Estado = "Activa",
                    AprobadoPor = "Secretaría FAAUA",
                    CumpleRequisitosEstatutos = true
                }
            },
            AutorizacionDatosPersonales = true,
            FechaAutorizacionHabeasData = DateTime.Today.AddMonths(-6),
            EstadoMembresia = "AlDia",
            FechaRegistro = DateTime.UtcNow.AddDays(-20),
            FechaActualizacion = DateTime.UtcNow.AddDays(-5),
            CreadoPor = "Semilla",
            ActualizadoPor = "Semilla"
        },
        new Asociado
        {
            Id = 2,
            Cedula = "78901234",
            NombreCompleto = "María Elena Mendoza",
            FincaVereda = "El Porvenir - Vereda San José",
            Telefono = "3147890123",
            Correo = "maria.mendoza@asoagro.com",
            CedulaArchivoNombre = "Cedula_MariaMendoza_78901234.pdf",
            CedulaArchivoTamano = "285 KB",
            NombrePredio = "El Porvenir",
            VeredaSector = "Vereda San José",
            Municipio = "Turbo",
            TenenciaTerreno = "Arrendado",
            HectareasPredio = 4.2m,
            HectareasLineaProductiva = 3.8m,
            TipoActividadProductiva = "Agrícola",
            ProduccionAproximada = "9.5 toneladas/año",
            CultivoPrincipal = "Cacao, Aguacate",
            CultivosSeleccionados = new List<string> { "Cacao", "Aguacate" },
            TieneBPA = false,
            EsVictima = true,
            GruposVulnerables = new List<string> { "MujerCabezaHogar" },
            SoportesGruposVulnerables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["MujerCabezaHogar"] = "Declaracion_CabezaHogar_MariaMendoza.pdf"
            },
            CuotasAportes = new List<CuotaAporteAsociado>
            {
                new CuotaAporteAsociado
                {
                    Id = "cuota-maria-1",
                    TipoCuota = "Sostenimiento",
                    Periodicidad = "Bimensual",
                    ValorCuota = 45000m,
                    FechaVencimiento = DateTime.Today.AddDays(12),
                    CantidadCuotasPendientes = 1,
                    ValorTotalPendiente = 45000m,
                    ValorTotalPagado = 45000m,
                    PagadoEditadoManualmente = true,
                    EstadoPago = "Pago parcial",
                    Soportes = new List<SoporteCuotaAsociado>
                    {
                        new SoporteCuotaAsociado
                        {
                            Id = "sop-maria-1",
                            CuotaId = "cuota-maria-1",
                            TipoCuota = "Sostenimiento",
                            Periodicidad = "Bimensual",
                            ArchivoNombre = "Comprobante_Abono_Sostenimiento_MariaMendoza.pdf",
                            ArchivoTamano = "210 KB"
                        }
                    }
                }
            },
            EstadoMembresia = "Pendiente",
            FechaRegistro = DateTime.UtcNow.AddDays(-12),
            FechaActualizacion = DateTime.UtcNow.AddDays(-2),
            CreadoPor = "Semilla",
            ActualizadoPor = "Semilla"
        },
        new Asociado
        {
            Id = 3,
            Cedula = "1045123987",
            NombreCompleto = "Jorge Iván Arboleda",
            AsociacionPertenece = "ASOPLÁTANO - Asociación de Productores de Plátano de Urabá",
            FincaVereda = "Finca Villa María - Vereda La Balsa",
            Telefono = "3129876543",
            Correo = "jorge.arboleda@asoplatanouraba.org",
            NombrePredio = "Finca Villa María",
            VeredaSector = "Vereda La Balsa",
            Municipio = "Carepa",
            TenenciaTerreno = "Propio",
            HectareasPredio = 8.0m,
            HectareasLineaProductiva = 7.5m,
            TipoActividadProductiva = "Agrícola",
            ProduccionAproximada = "26 toneladas/año",
            CultivoPrincipal = "Platano",
            CultivosSeleccionados = new List<string> { "Platano" },
            TieneBPA = true,
            EsVictima = false,
            Afiliaciones = new List<AfiliacionAsociado>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AsociadoId = 3,
                    AsociadoCedula = "98765432",
                    AsociacionNombre = "ASOPLÁTANO - Asociación de Productores de Plátano de Urabá",
                    FechaSolicitud = DateTime.Today.AddMonths(-4),
                    FechaAprobacion = DateTime.Today.AddMonths(-4),
                    Estado = "Activa",
                    AprobadoPor = "Secretaría FAAUA",
                    CumpleRequisitosEstatutos = true
                }
            },
            AutorizacionDatosPersonales = true,
            FechaAutorizacionHabeasData = DateTime.Today.AddMonths(-4),
            EstadoMembresia = "AlDia",
            FechaRegistro = DateTime.UtcNow.AddDays(-30),
            FechaActualizacion = DateTime.UtcNow.AddDays(-10),
            CreadoPor = "Semilla",
            ActualizadoPor = "Semilla"
        }
    };

    private readonly object sync = new();
    private readonly string usuarioAuditoria;
    private int siguienteId = 4;

    public InMemoryAsociadoService(IOptions<AsoagroOptions>? options = null)
    {
        usuarioAuditoria = options?.Value.Audit.DefaultUser ?? "Sistema";
    }

    public bool UsaAlmacenamientoPermanente => false;

    public IReadOnlyCollection<Asociado> ObtenerTodos()
    {
        lock (sync)
        {
            return asociados
                .OrderByDescending(a => a.FechaRegistro)
                .Select(Clonar)
                .ToList();
        }
    }

    public ResultadoRegistroAsociado Registrar(Asociado asociado)
    {
        var cedulaLimpia = asociado.Cedula.Trim();
        var ahora = DateTime.UtcNow;

        lock (sync)
        {
            var existente = asociados.FirstOrDefault(a => string.Equals(a.Cedula, cedulaLimpia, StringComparison.OrdinalIgnoreCase));
            if (existente != null)
            {
                var nuevaAsoc = asociado.AsociacionPertenece?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(nuevaAsoc))
                {
                    var afiliacionExistente = existente.Afiliaciones.FirstOrDefault(af =>
                        string.Equals(af.AsociacionNombre, nuevaAsoc, StringComparison.OrdinalIgnoreCase));

                    if (afiliacionExistente != null && afiliacionExistente.Estado == "Activa")
                    {
                        return new ResultadoRegistroAsociado
                        {
                            Exitoso = false,
                            Mensaje = $"El productor {existente.NombreCompleto} ya cuenta con afiliación activa en {nuevaAsoc}."
                        };
                    }

                    if (afiliacionExistente != null)
                    {
                        afiliacionExistente.Estado = "Activa";
                        afiliacionExistente.FechaAprobacion = ahora;
                    }
                    else
                    {
                        existente.Afiliaciones.Add(new AfiliacionAsociado
                        {
                            Id = Guid.NewGuid(),
                            AsociadoId = existente.Id,
                            AsociadoCedula = cedulaLimpia,
                            AsociacionNombre = nuevaAsoc,
                            FechaSolicitud = ahora,
                            FechaAprobacion = ahora,
                            Estado = "Activa",
                            AprobadoPor = usuarioAuditoria,
                            CumpleRequisitosEstatutos = true,
                            FechaRegistro = ahora
                        });
                    }

                    existente.AsociacionPertenece = nuevaAsoc;
                    existente.FechaActualizacion = ahora;
                    existente.ActualizadoPor = usuarioAuditoria;

                    return new ResultadoRegistroAsociado
                    {
                        Exitoso = true,
                        Mensaje = $"Perfil único vinculado: El productor {existente.NombreCompleto} fue asociado exitosamente a {nuevaAsoc} conservando su expediente único.",
                        Asociado = Clonar(existente)
                    };
                }

                return new ResultadoRegistroAsociado
                {
                    Exitoso = false,
                    Mensaje = $"El productor con cédula {cedulaLimpia} ya existe en el sistema. Seleccione la asociación a la que desea afiliarlo."
                };
            }

            var entidad = Clonar(asociado);
            entidad.Id = siguienteId++;
            entidad.Cedula = cedulaLimpia;
            entidad.AsociacionPertenece = asociado.AsociacionPertenece?.Trim();
            entidad.NombreCompleto = asociado.NombreCompleto.Trim();
            entidad.FincaVereda = asociado.FincaVereda.Trim();
            entidad.CultivoPrincipal = asociado.CultivoPrincipal.Trim();
            entidad.Telefono = asociado.Telefono?.Trim();
            entidad.Correo = asociado.Correo?.Trim();
            entidad.NombrePredio = string.IsNullOrWhiteSpace(asociado.NombrePredio)
                ? entidad.FincaVereda
                : asociado.NombrePredio.Trim();
            entidad.VeredaSector = string.IsNullOrWhiteSpace(asociado.VeredaSector)
                ? entidad.FincaVereda
                : asociado.VeredaSector.Trim();
            entidad.Municipio = asociado.Municipio?.Trim();
            entidad.FechaRegistro = ahora;
            entidad.FechaActualizacion = ahora;
            entidad.CreadoPor = usuarioAuditoria;
            entidad.ActualizadoPor = usuarioAuditoria;

            if (entidad.Afiliaciones.Count == 0 && !string.IsNullOrWhiteSpace(entidad.AsociacionPertenece))
            {
                entidad.Afiliaciones.Add(new AfiliacionAsociado
                {
                    Id = Guid.NewGuid(),
                    AsociadoId = entidad.Id,
                    AsociadoCedula = cedulaLimpia,
                    AsociacionNombre = entidad.AsociacionPertenece,
                    FechaSolicitud = ahora,
                    FechaAprobacion = ahora,
                    Estado = "Activa",
                    AprobadoPor = usuarioAuditoria,
                    CumpleRequisitosEstatutos = true,
                    FechaRegistro = ahora
                });
            }

            asociados.Add(entidad);

            return new ResultadoRegistroAsociado
            {
                Exitoso = true,
                Mensaje = "Asociado registrado exitosamente en memoria de Asoagro",
                Asociado = Clonar(entidad)
            };
        }
    }

    public ResultadoRegistroAsociado Actualizar(Asociado asociado)
    {
        var cedulaLimpia = asociado.Cedula.Trim();
        var ahora = DateTime.UtcNow;

        lock (sync)
        {
            var existente = asociados.FirstOrDefault(a => a.Id == asociado.Id)
                ?? asociados.FirstOrDefault(a => string.Equals(a.Cedula, cedulaLimpia, StringComparison.OrdinalIgnoreCase));

            if (existente is null)
            {
                return new ResultadoRegistroAsociado
                {
                    Exitoso = false,
                    Mensaje = "No se encontró el asociado que deseas actualizar."
                };
            }

            if (asociados.Any(a => a.Id != existente.Id && string.Equals(a.Cedula, cedulaLimpia, StringComparison.OrdinalIgnoreCase)))
            {
                return new ResultadoRegistroAsociado
                {
                    Exitoso = false,
                    Mensaje = "Ya existe otro asociado registrado con esa cédula."
                };
            }

            existente.Cedula = cedulaLimpia;
            existente.AsociacionPertenece = asociado.AsociacionPertenece?.Trim();
            existente.NombreCompleto = asociado.NombreCompleto.Trim();
            existente.FincaVereda = asociado.FincaVereda.Trim();
            existente.Telefono = asociado.Telefono?.Trim();
            existente.Correo = asociado.Correo?.Trim();
            existente.CedulaArchivoNombre = asociado.CedulaArchivoNombre;
            existente.CedulaArchivoTamano = asociado.CedulaArchivoTamano;
            existente.CedulaArchivoUrl = asociado.CedulaArchivoUrl;
            existente.NombrePredio = asociado.NombrePredio?.Trim();
            existente.VeredaSector = asociado.VeredaSector?.Trim();
            existente.Municipio = asociado.Municipio?.Trim();
            existente.TenenciaTerreno = asociado.TenenciaTerreno?.Trim();
            existente.HectareasPredio = asociado.HectareasPredio;
            existente.HectareasLineaProductiva = asociado.HectareasLineaProductiva;
            existente.TipoActividadProductiva = asociado.TipoActividadProductiva?.Trim();
            existente.ProduccionAproximada = asociado.ProduccionAproximada?.Trim();
            existente.CultivoPrincipal = asociado.CultivoPrincipal.Trim();
            existente.CultivosSeleccionados = new List<string>(asociado.CultivosSeleccionados);
            existente.TieneBPA = asociado.TieneBPA;
            existente.CertificadoBpaNombre = asociado.CertificadoBpaNombre;
            existente.CertificadoBpaTamano = asociado.CertificadoBpaTamano;
            existente.CertificadoBpaUrl = asociado.CertificadoBpaUrl;
            existente.EsVictima = asociado.EsVictima;
            existente.GruposVulnerables = new List<string>(asociado.GruposVulnerables);
            existente.SoportesGruposVulnerables = new Dictionary<string, string>(
                asociado.SoportesGruposVulnerables,
                StringComparer.OrdinalIgnoreCase);
            existente.SoportesGruposUrls = new Dictionary<string, string>(
                asociado.SoportesGruposUrls,
                StringComparer.OrdinalIgnoreCase);
            existente.CuotasAportes = asociado.CuotasAportes.Select(c => c.Clonar()).ToList();
            existente.EstadoMembresia = asociado.EstadoMembresia;
            existente.FechaActualizacion = ahora;
            existente.ActualizadoPor = usuarioAuditoria;

            return new ResultadoRegistroAsociado
            {
                Exitoso = true,
                Mensaje = "El expediente se actualizó correctamente.",
                Asociado = Clonar(existente)
            };
        }
    }

    private static Asociado Clonar(Asociado origen)
    {
        return new Asociado
        {
            Id = origen.Id,
            Cedula = origen.Cedula,
            AsociacionPertenece = origen.AsociacionPertenece,
            NombreCompleto = origen.NombreCompleto,
            FincaVereda = origen.FincaVereda,
            Telefono = origen.Telefono,
            Correo = origen.Correo,
            CedulaArchivoNombre = origen.CedulaArchivoNombre,
            CedulaArchivoTamano = origen.CedulaArchivoTamano,
            CedulaArchivoUrl = origen.CedulaArchivoUrl,
            NombrePredio = origen.NombrePredio,
            VeredaSector = origen.VeredaSector,
            Municipio = origen.Municipio,
            TenenciaTerreno = origen.TenenciaTerreno,
            HectareasPredio = origen.HectareasPredio,
            HectareasLineaProductiva = origen.HectareasLineaProductiva,
            TipoActividadProductiva = origen.TipoActividadProductiva,
            ProduccionAproximada = origen.ProduccionAproximada,
            CultivoPrincipal = origen.CultivoPrincipal,
            CultivosSeleccionados = new List<string>(origen.CultivosSeleccionados),
            TieneBPA = origen.TieneBPA,
            CertificadoBpaNombre = origen.CertificadoBpaNombre,
            CertificadoBpaTamano = origen.CertificadoBpaTamano,
            CertificadoBpaUrl = origen.CertificadoBpaUrl,
            EsVictima = origen.EsVictima,
            GruposVulnerables = new List<string>(origen.GruposVulnerables),
            SoportesGruposVulnerables = new Dictionary<string, string>(
                origen.SoportesGruposVulnerables,
                StringComparer.OrdinalIgnoreCase),
            SoportesGruposUrls = new Dictionary<string, string>(
                origen.SoportesGruposUrls,
                StringComparer.OrdinalIgnoreCase),
            CuotasAportes = origen.CuotasAportes.Select(c => c.Clonar()).ToList(),
            Afiliaciones = origen.Afiliaciones.Select(a => new AfiliacionAsociado
            {
                Id = a.Id,
                AsociadoId = a.AsociadoId,
                AsociadoCedula = a.AsociadoCedula,
                AsociacionNombre = a.AsociacionNombre,
                FechaSolicitud = a.FechaSolicitud,
                FechaAprobacion = a.FechaAprobacion,
                FechaRetiro = a.FechaRetiro,
                Estado = a.Estado,
                AprobadoPor = a.AprobadoPor,
                MotivoRetiro = a.MotivoRetiro,
                Observaciones = a.Observaciones,
                CumpleRequisitosEstatutos = a.CumpleRequisitosEstatutos,
                RequisitosCumplidos = new List<string>(a.RequisitosCumplidos),
                SoportesAnexos = a.SoportesAnexos,
                FechaRegistro = a.FechaRegistro
            }).ToList(),
            AutorizacionDatosPersonales = origen.AutorizacionDatosPersonales,
            FechaAutorizacionHabeasData = origen.FechaAutorizacionHabeasData,
            EstadoMembresia = origen.EstadoMembresia,
            FechaRegistro = origen.FechaRegistro,
            FechaActualizacion = origen.FechaActualizacion,
            CreadoPor = origen.CreadoPor,
            ActualizadoPor = origen.ActualizadoPor
        };
    }
}

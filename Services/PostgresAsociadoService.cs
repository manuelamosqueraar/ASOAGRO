using Asoagro.Configuration;
using Asoagro.Data;
using Asoagro.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Asoagro.Services;

public class PostgresAsociadoService : IAsociadoService
{
    private readonly IDbContextFactory<AsoagroDbContext> dbContextFactory;
    private readonly AsoagroOptions options;

    public PostgresAsociadoService(
        IDbContextFactory<AsoagroDbContext> dbContextFactory,
        IOptions<AsoagroOptions> options)
    {
        this.dbContextFactory = dbContextFactory;
        this.options = options.Value;
    }

    public bool UsaAlmacenamientoPermanente => true;

    public IReadOnlyCollection<Asociado> ObtenerTodos()
    {
        using var context = dbContextFactory.CreateDbContext();
        var lista = context.Asociados
            .Include(a => a.Asociacion)
            .Include(a => a.AsociadoCultivos).ThenInclude(ac => ac.Cultivo)
            .Include(a => a.AsociadoGruposVulnerables).ThenInclude(ag => ag.GrupoVulnerable)
            .Include(a => a.CuotasAsociados).ThenInclude(ca => ca.TipoCuota)
            .AsSplitQuery()
            .AsNoTracking()
            .OrderByDescending(a => a.FechaRegistro)
            .ToList();

        foreach (var a in lista)
        {
            if (a.Asociacion != null && string.IsNullOrWhiteSpace(a.AsociacionPertenece))
            {
                a.AsociacionPertenece = a.Asociacion.Nombre;
            }

            if (a.AsociadoCultivos.Count > 0 && a.CultivosSeleccionados.Count == 0)
            {
                a.CultivosSeleccionados = a.AsociadoCultivos
                    .Select(ac => ac.Cultivo?.Nombre ?? string.Empty)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();
            }

            if (a.AsociadoGruposVulnerables.Count > 0 && a.GruposVulnerables.Count == 0)
            {
                a.GruposVulnerables = a.AsociadoGruposVulnerables
                    .Select(ag => ag.GrupoVulnerable?.Nombre ?? string.Empty)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();
            }

            if (a.CuotasAsociados.Count > 0 && a.CuotasAportes.Count == 0)
            {
                a.CuotasAportes = a.CuotasAsociados.Select(ca => new CuotaAporteAsociado
                {
                    Id = ca.Id.ToString(),
                    TipoCuota = ca.TipoCuota?.Nombre ?? "Sostenimiento",
                    Periodicidad = ca.Periodicidad ?? "Mensual",
                    ValorCuota = ca.Valor,
                    ValorTotalPagado = ca.ValorPagado,
                    ValorTotalPendiente = Math.Max(0m, ca.Valor - ca.ValorPagado),
                    CantidadCuotasPendientes = (ca.Valor - ca.ValorPagado) > 0 ? 1 : 0,
                    FechaVencimiento = ca.FechaVencimiento.HasValue ? ca.FechaVencimiento.Value.ToDateTime(TimeOnly.MinValue) : null,
                    EstadoPago = ca.Estado
                }).ToList();
            }
        }

        return lista;
    }

    public ResultadoRegistroAsociado Registrar(Asociado asociado)
    {
        using var context = dbContextFactory.CreateDbContext();

        try
        {
            asociado.Cedula = asociado.Cedula?.Trim() ?? string.Empty;
            asociado.NombreCompleto = asociado.NombreCompleto?.Trim() ?? string.Empty;
            asociado.FincaVereda = asociado.FincaVereda?.Trim() ?? string.Empty;
            asociado.CultivoPrincipal = asociado.CultivoPrincipal?.Trim() ?? string.Empty;
            asociado.Telefono = asociado.Telefono?.Trim() ?? string.Empty;
            asociado.Correo = asociado.Correo?.Trim() ?? string.Empty;
            asociado.NombrePredio = string.IsNullOrWhiteSpace(asociado.NombrePredio)
                ? asociado.FincaVereda
                : asociado.NombrePredio.Trim();
            asociado.VeredaSector = string.IsNullOrWhiteSpace(asociado.VeredaSector)
                ? asociado.FincaVereda
                : asociado.VeredaSector.Trim();
            asociado.Municipio = asociado.Municipio?.Trim() ?? string.Empty;

            if (context.Asociados.Any(a => a.Cedula == asociado.Cedula))
            {
                return new ResultadoRegistroAsociado
                {
                    Exitoso = false,
                    Mensaje = "Ya existe un asociado registrado con esa cédula."
                };
            }

            // Resolver asociacion_id obligatoria según el esquema de Supabase
            if (asociado.AsociacionId <= 0)
            {
                var nombreAsoc = asociado.AsociacionPertenece?.Trim() ?? string.Empty;
                var todasAsocs = context.Asociaciones.ToList();
                AsociacionEntity? asocEncontrada = null;

                if (!string.IsNullOrWhiteSpace(nombreAsoc) && todasAsocs.Count > 0)
                {
                    // 1. Coincidencia exacta
                    asocEncontrada = todasAsocs.FirstOrDefault(a =>
                        string.Equals(a.Nombre.Trim(), nombreAsoc, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a.Nit?.Trim(), nombreAsoc, StringComparison.OrdinalIgnoreCase));

                    // 2. Coincidencia parcial o por contención
                    asocEncontrada ??= todasAsocs.FirstOrDefault(a =>
                        nombreAsoc.Contains(a.Nombre.Trim(), StringComparison.OrdinalIgnoreCase) ||
                        a.Nombre.Contains(nombreAsoc, StringComparison.OrdinalIgnoreCase));

                    // 3. Coincidencia por partes separadas por '-'
                    if (asocEncontrada == null && nombreAsoc.Contains('-'))
                    {
                        var partes = nombreAsoc.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        asocEncontrada = todasAsocs.FirstOrDefault(a =>
                            partes.Any(p => string.Equals(a.Nombre.Trim(), p, StringComparison.OrdinalIgnoreCase)));
                    }
                }

                asocEncontrada ??= todasAsocs.OrderBy(a => a.Id).FirstOrDefault();

                if (asocEncontrada == null)
                {
                    // Si no hay asociaciones en la tabla, crear la entidad principal para satisfacer la FK
                    asocEncontrada = new AsociacionEntity
                    {
                        Nombre = !string.IsNullOrWhiteSpace(nombreAsoc) ? nombreAsoc : "Asociación Agropecuaria de Urabá",
                        Nit = "900543210-8",
                        Activo = true,
                        CreadoEn = DateTime.UtcNow
                    };
                    context.Asociaciones.Add(asocEncontrada);
                    context.SaveChanges();
                }

                asociado.AsociacionId = asocEncontrada.Id;
            }

            asociado.Id = 0;
            asociado.EstadoMembresia = AsociadoMapper.MapearEstadoMembresiaParaBd(asociado.EstadoMembresia);
            asociado.FechaRegistro = DateTime.UtcNow;
            asociado.FechaActualizacion = asociado.FechaRegistro;
            asociado.CreadoPor = options.Audit.DefaultUser;
            asociado.ActualizadoPor = options.Audit.DefaultUser;

            // Limpiar navegaciones circulares antes de agregar la entidad principal
            var cultivosTemp = new List<string>(asociado.CultivosSeleccionados);
            var gruposTemp = new List<string>(asociado.GruposVulnerables);
            var cuotasTemp = asociado.CuotasAportes.Select(c => c.Clonar()).ToList();
            asociado.AsociadoCultivos.Clear();
            asociado.AsociadoGruposVulnerables.Clear();
            asociado.CuotasAsociados.Clear();

            context.Asociados.Add(asociado);
            context.SaveChanges();

            // Guardar cultivos asociados
            if (cultivosTemp.Count > 0)
            {
                try
                {
                    var catalogoCultivos = context.Cultivos.ToList();
                    foreach (var nombreCultivo in cultivosTemp)
                    {
                        var cultivoExistente = catalogoCultivos.FirstOrDefault(c =>
                            string.Equals(c.Nombre, nombreCultivo, StringComparison.OrdinalIgnoreCase));

                        if (cultivoExistente != null)
                        {
                            context.AsociadoCultivos.Add(new AsociadoCultivoEntity
                            {
                                AsociadoId = asociado.Id,
                                CultivoId = cultivoExistente.Id,
                                Hectareas = asociado.HectareasPredio,
                                TieneBpa = asociado.TieneBPA,
                                CertificadoBpaNombre = asociado.CertificadoBpaNombre,
                                CertificadoBpaRuta = asociado.CertificadoBpaUrl,
                                CreadoEn = DateTime.UtcNow
                            });
                        }
                    }
                    context.SaveChanges();
                }
                catch { }
            }

            // Guardar grupos vulnerables
            if (gruposTemp.Count > 0)
            {
                try
                {
                    var catalogoGrupos = context.GruposVulnerables.ToList();
                    foreach (var nombreGrupo in gruposTemp)
                    {
                        var grupoExistente = catalogoGrupos.FirstOrDefault(g =>
                            string.Equals(g.Nombre, nombreGrupo, StringComparison.OrdinalIgnoreCase));

                        if (grupoExistente != null)
                        {
                            asociado.SoportesGruposUrls.TryGetValue(nombreGrupo, out var urlSoporte);
                            asociado.SoportesGruposVulnerables.TryGetValue(nombreGrupo, out var nombreSoporte);

                            context.AsociadoGruposVulnerables.Add(new AsociadoGrupoVulnerableEntity
                            {
                                AsociadoId = asociado.Id,
                                GrupoId = grupoExistente.Id,
                                ArchivoNombre = nombreSoporte,
                                ArchivoRuta = urlSoporte,
                                CreadoEn = DateTime.UtcNow
                            });
                        }
                    }
                    context.SaveChanges();
                }
                catch { }
            }

            // Guardar cuotas y aportes en Supabase
            if (cuotasTemp.Count > 0)
            {
                try
                {
                    var catalogoTipos = context.TiposCuota.ToList();
                    foreach (var cuota in cuotasTemp)
                    {
                        var tipoExistente = catalogoTipos.FirstOrDefault(t =>
                            string.Equals(t.Nombre, cuota.TipoCuota, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(t.Codigo, cuota.TipoCuota, StringComparison.OrdinalIgnoreCase));

                        if (tipoExistente == null)
                        {
                            tipoExistente = new TipoCuotaEntity
                            {
                                Codigo = cuota.TipoCuota.Trim().ToUpperInvariant(),
                                Nombre = cuota.TipoCuota.Trim(),
                                Activo = true,
                                PermitePeriodicidad = true
                            };
                            context.TiposCuota.Add(tipoExistente);
                            context.SaveChanges();
                            catalogoTipos.Add(tipoExistente);
                        }

                        var fechaVenc = cuota.FechaVencimiento.HasValue
                            ? DateOnly.FromDateTime(cuota.FechaVencimiento.Value)
                            : (DateOnly?)null;

                        var fechaPago = cuota.ValorTotalPagado > 0
                            ? DateOnly.FromDateTime(DateTime.Today)
                            : (DateOnly?)null;

                        context.CuotasAsociados.Add(new CuotaAsociadoEntity
                        {
                            AsociadoId = asociado.Id,
                            TipoCuotaId = tipoExistente.Id,
                            Periodicidad = cuota.Periodicidad,
                            Valor = cuota.ValorCuota,
                            ValorPagado = cuota.ValorTotalPagado,
                            FechaVencimiento = fechaVenc,
                            FechaPago = fechaPago,
                            Estado = cuota.CalcularEstadoAutomatico(),
                            CreadoEn = DateTime.UtcNow
                        });
                    }
                    context.SaveChanges();
                }
                catch { }
            }

            asociado.CuotasAportes = cuotasTemp;
            return new ResultadoRegistroAsociado
            {
                Exitoso = true,
                Mensaje = "Asociado registrado exitosamente en el servidor de Asoagro",
                Asociado = asociado
            };
        }
        catch (Exception ex)
        {
            return new ResultadoRegistroAsociado
            {
                Exitoso = false,
                Mensaje = $"Error al registrar el asociado en Supabase: {ex.InnerException?.Message ?? ex.Message}"
            };
        }
    }

    public ResultadoRegistroAsociado Actualizar(Asociado asociado)
    {
        using var context = dbContextFactory.CreateDbContext();
        var registro = context.Asociados.FirstOrDefault(a => a.Id == asociado.Id);
        if (registro is null)
        {
            return new ResultadoRegistroAsociado
            {
                Exitoso = false,
                Mensaje = "No se encontró el asociado que deseas actualizar."
            };
        }

        asociado.Cedula = asociado.Cedula.Trim();
        asociado.NombreCompleto = asociado.NombreCompleto.Trim();
        asociado.FincaVereda = asociado.FincaVereda.Trim();
        asociado.CultivoPrincipal = asociado.CultivoPrincipal.Trim();
        asociado.Telefono = asociado.Telefono?.Trim();
        asociado.Correo = asociado.Correo?.Trim();
        asociado.NombrePredio = asociado.NombrePredio?.Trim();
        asociado.VeredaSector = asociado.VeredaSector?.Trim();
        asociado.Municipio = asociado.Municipio?.Trim();

        if (context.Asociados.Any(a => a.Id != asociado.Id && a.Cedula == asociado.Cedula))
        {
            return new ResultadoRegistroAsociado
            {
                Exitoso = false,
                Mensaje = "Ya existe otro asociado registrado con esa cédula."
            };
        }

        // Resolver asociacion si fue modificada
        if (asociado.AsociacionId > 0)
        {
            registro.AsociacionId = asociado.AsociacionId;
        }
        else if (!string.IsNullOrWhiteSpace(asociado.AsociacionPertenece))
        {
            var nombreAsoc = asociado.AsociacionPertenece.Trim();
            var asoc = context.Asociaciones.FirstOrDefault(a =>
                a.Nombre.ToLower() == nombreAsoc.ToLower() || a.Nit == nombreAsoc);
            if (asoc != null)
            {
                registro.AsociacionId = asoc.Id;
            }
        }

        registro.Cedula = asociado.Cedula;
        registro.NombreCompleto = asociado.NombreCompleto;
        registro.FincaVereda = asociado.FincaVereda;
        registro.Telefono = asociado.Telefono;
        registro.Correo = asociado.Correo;
        registro.NombrePredio = asociado.NombrePredio;
        registro.VeredaSector = asociado.VeredaSector;
        registro.Municipio = asociado.Municipio;
        registro.HectareasPredio = asociado.HectareasPredio;
        registro.CultivoPrincipal = asociado.CultivoPrincipal;
        registro.TieneBPA = asociado.TieneBPA;
        registro.EsVictima = asociado.EsVictima;
        registro.EstadoMembresia = AsociadoMapper.MapearEstadoMembresiaParaBd(asociado.EstadoMembresia);
        registro.FechaActualizacion = DateTime.UtcNow;
        registro.ActualizadoPor = options.Audit.DefaultUser;
        context.SaveChanges();

        return new ResultadoRegistroAsociado
        {
            Exitoso = true,
            Mensaje = "El expediente se actualizó correctamente.",
            Asociado = registro
        };
    }
}

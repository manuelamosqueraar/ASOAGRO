using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Asoagro.Data;
using Asoagro.Models;
using Microsoft.EntityFrameworkCore;

namespace Asoagro.Services;

public class PostgresSeguimientoAsociadoService(
    IDbContextFactory<AsoagroDbContext> dbContextFactory) : ISeguimientoAsociadoService
{
    private static readonly ConcurrentDictionary<int, string> CacheHoras = new();
    private static readonly Regex TagHoraRegex = new(@"^\[HORA:\s*([0-2]?[0-9]:[0-5][0-9])\]\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool UsaAlmacenamientoPermanente => true;

    public IReadOnlyCollection<SeguimientoAsociado> ObtenerTodos()
    {
        using var context = dbContextFactory.CreateDbContext();
        var lista = context.SeguimientosAsociados
            .AsNoTracking()
            .Include(seguimiento => seguimiento.Asociado)
            .OrderByDescending(seguimiento => seguimiento.FechaGestion)
            .ToList();

        foreach (var seg in lista)
        {
            var desc = seg.Descripcion ?? string.Empty;
            var match = TagHoraRegex.Match(desc);
            if (match.Success)
            {
                // Extraer la hora estructurada y ocultar la etiqueta para la UI y reportes
                seg.HoraReunion = match.Groups[1].Value;
                seg.Descripcion = desc[match.Length..];
                CacheHoras[seg.Id] = seg.HoraReunion;
            }
            else if (CacheHoras.TryGetValue(seg.Id, out var horaCache))
            {
                // Compatibilidad con registros cargados previamente en memoria
                seg.HoraReunion = horaCache;
            }
            else
            {
                seg.HoraReunion = string.Empty;
            }
        }

        return lista;
    }

    public ResultadoRegistroSeguimientoAsociado Registrar(SeguimientoAsociado seguimiento)
    {
        using var context = dbContextFactory.CreateDbContext();
        if (!context.Asociados.Any(asociado => asociado.Id == seguimiento.AsociadoId))
        {
            return new ResultadoRegistroSeguimientoAsociado
            {
                Exitoso = false,
                Mensaje = "El asociado seleccionado ya no existe en el padrón."
            };
        }

        // Validar rango horario permitido (08:00 a 17:00) si se especificó hora
        string? horaNormalizada = null;
        if (!string.IsNullOrWhiteSpace(seguimiento.HoraReunion))
        {
            var horaStr = seguimiento.HoraReunion.Trim();
            if (TimeOnly.TryParse(horaStr, out var timeVal))
            {
                if (timeVal < new TimeOnly(8, 0) || timeVal > new TimeOnly(17, 0))
                {
                    return new ResultadoRegistroSeguimientoAsociado
                    {
                        Exitoso = false,
                        Mensaje = "La hora de la próxima reunión debe estar entre las 08:00 y las 17:00."
                    };
                }
                horaNormalizada = timeVal.ToString("HH:mm");
            }
            else
            {
                return new ResultadoRegistroSeguimientoAsociado
                {
                    Exitoso = false,
                    Mensaje = "La hora de la reunión no tiene un formato válido (HH:mm)."
                };
            }
        }

        var textoOriginal = (seguimiento.Descripcion ?? string.Empty).Trim();
        string descripcionParaBd;

        // Estructurar la etiqueta sin exceder el límite de 1000 caracteres de la columna
        if (!string.IsNullOrWhiteSpace(horaNormalizada))
        {
            var tag = $"[HORA:{horaNormalizada}]\n";
            var maxTexto = 1000 - tag.Length;
            var textoAjustado = textoOriginal.Length > maxTexto ? textoOriginal[..maxTexto] : textoOriginal;
            descripcionParaBd = tag + textoAjustado;
        }
        else
        {
            descripcionParaBd = textoOriginal.Length > 1000 ? textoOriginal[..1000] : textoOriginal;
        }

        seguimiento.Id = 0;
        seguimiento.Asociado = null;
        seguimiento.FechaGestion = DateTime.UtcNow;
        seguimiento.Descripcion = descripcionParaBd;

        context.SeguimientosAsociados.Add(seguimiento);
        context.SaveChanges();

        // Restaurar para la vista y notificaciones el texto original sin la etiqueta técnica
        seguimiento.Descripcion = textoOriginal;
        if (!string.IsNullOrWhiteSpace(horaNormalizada))
        {
            seguimiento.HoraReunion = horaNormalizada;
            CacheHoras[seguimiento.Id] = horaNormalizada;
        }

        return new ResultadoRegistroSeguimientoAsociado
        {
            Exitoso = true,
            Mensaje = "La gestión quedó guardada en el historial."
        };
    }
}

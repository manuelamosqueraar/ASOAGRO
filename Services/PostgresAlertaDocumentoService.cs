using Asoagro.Data;
using Asoagro.Models;
using Microsoft.EntityFrameworkCore;

namespace Asoagro.Services;

public class PostgresAlertaDocumentoService : IAlertaDocumentoService
{
    private readonly IDbContextFactory<AsoagroDbContext> dbContextFactory;

    public PostgresAlertaDocumentoService(IDbContextFactory<AsoagroDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public IReadOnlyCollection<AlertaDocumento> ObtenerAlertasPendientes(DateTime fechaReferencia)
    {
        using var context = dbContextFactory.CreateDbContext();
        var fechaBase = fechaReferencia.Date;

        var documentos = context.Documentos
            .Include(d => d.Asociado)
            .Include(d => d.TipoDocumentoNav)
            .AsNoTracking()
            .Where(d => d.FechaVencimiento != null)
            .ToList();

        var alertas = documentos
            .Select(d =>
            {
                var fechaVencimiento = d.FechaVencimiento!.Value.Date;
                var diasRestantes = (fechaVencimiento - fechaBase).Days;
                return new
                {
                    Documento = d,
                    FechaVencimiento = fechaVencimiento,
                    DiasRestantes = diasRestantes
                };
            })
            .Where(x => x.DiasRestantes >= 0 && x.DiasRestantes <= 30)
            .OrderBy(x => x.DiasRestantes)
            .Select(x => new AlertaDocumento
            {
                NombreAsociado = x.Documento.Asociado?.NombreCompleto ?? x.Documento.DocumentoAsociado,
                TipoDocumento = x.Documento.TipoDocumentoNav?.Nombre ?? x.Documento.TipoDocumento,
                FechaVencimiento = x.FechaVencimiento,
                DiasRestantes = x.DiasRestantes,
                MensajeAlerta = ObtenerMensaje(x.DiasRestantes)
            })
            .ToList();

        return alertas;
    }

    private static string ObtenerMensaje(int diasRestantes)
    {
        if (diasRestantes <= 7) return "Alerta crítica: vence en menos de 7 días";
        if (diasRestantes <= 15) return "Advertencia: vence en menos de 15 días";
        return "Aviso preventivo: vence en menos de 30 días";
    }
}


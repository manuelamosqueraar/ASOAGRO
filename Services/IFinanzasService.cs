using System;
using System.Collections.Generic;
using Asoagro.Models;

namespace Asoagro.Services;

public interface IFinanzasService
{
    IReadOnlyList<MovimientoFinanciero> ObtenerTodos(string? asociacion = null, string? periodo = null, TipoMovimientoFinanciero? tipo = null, string? estado = null);
    MovimientoFinanciero? ObtenerPorId(Guid id);
    ResumenFinanciero ObtenerResumen(string? asociacion = null, string? periodo = null);
    IReadOnlyList<string> ObtenerAsociaciones();
    IReadOnlyList<string> ObtenerPeriodosDisponibles();
    AsociacionInstitucional? ObtenerConfiguracionTributaria(string asociacion);

    // Operaciones con validación estricta de permisos por rol
    ResultadoOperacionFinanciera RegistrarMovimiento(MovimientoFinanciero movimiento, string usuario, string rol);
    ResultadoOperacionFinanciera ActualizarMovimiento(Guid id, Action<MovimientoFinanciero> actualizador, string usuario, string rol);
    ResultadoOperacionFinanciera ValidarMovimiento(Guid id, string usuario, string rol, string? observaciones = null);
    ResultadoOperacionFinanciera ObservarMovimiento(Guid id, string observacion, string usuario, string rol);
    ResultadoOperacionFinanciera MarcarPagado(Guid id, string usuario, string rol, string? comprobante = null, decimal? valorAbonado = null);
    ResultadoOperacionFinanciera AnularMovimiento(Guid id, string motivoAnulacion, string usuario, string rol);

    // Generación de PDFs formales de comprobantes y balances
    byte[] GenerarComprobantePdf(Guid id, string funcionario = "Tesorera General FAAUA");
    byte[] GenerarReporteConsolidadoPdf(string? asociacion = null, string? periodo = null, string funcionario = "Tesorera General FAAUA");
    ArchivoDescargaFinanciera ObtenerSoporteDescarga(Guid id, bool esComprobantePago = false, string funcionario = "Tesorera General FAAUA");

    // Evento de reactividad para sincronizar vistas en tiempo real
    event Action? DatosFinancierosActualizados;
}

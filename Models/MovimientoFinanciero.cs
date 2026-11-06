using System;
using System.Collections.Generic;

namespace Asoagro.Models;

public enum TipoMovimientoFinanciero
{
    Ingreso,
    CostoGasto,
    PagoDespacho
}

public static class SubtiposMovimientoFinanciero
{
    // Ingresos
    public const string VentaProduccion = "Venta de producción o cosecha";
    public const string VentaServicios = "Venta de servicios agrícolas o maquinaria";
    public const string CuotaOrdinaria = "Cuota ordinaria de asociados";
    public const string CuotaExtraordinaria = "Cuota extraordinaria";
    public const string AporteSocial = "Aporte social o fondo común";
    public const string RecaudoCartera = "Recaudo de cartera / Cuentas por cobrar";
    public const string SubsidioDonacion = "Subsidio, incentivo o donación estatal";
    public const string OtroIngreso = "Otro ingreso legítimo";

    // Costos y Gastos
    public const string InsumosFertilizantes = "Insumos, semillas y fertilizantes";
    public const string ManoDeObra = "Mano de obra, jornales y cuadrillas";
    public const string TransporteFlete = "Transporte, flete y logística de acopio";
    public const string AsistenciaTecnica = "Asistencia técnica y certificación BPA";
    public const string EmpaquesEmbalajes = "Empaques, cajas y materiales de despacho";
    public const string GastosAdministrativos = "Gastos administrativos y asambleas";
    public const string ServiciosPublicos = "Servicios públicos y arrendamiento sede";
    public const string MantenimientoEquipos = "Mantenimiento de maquinaria y bodegas";
    public const string HonorariosProfesionales = "Honorarios contables, jurídicos o técnicos";
    public const string OtroCostoGasto = "Otro costo o gasto justificado";

    // Pagos y Despachos
    public const string PagoProveedor = "Pago a proveedor de insumos o servicios";
    public const string LiquidacionAsociado = "Liquidación y pago de cosecha al asociado";
    public const string AbonoObligacion = "Abono o cancelación de obligación crediticia";
    public const string AnticipoDespacho = "Anticipo de flete o despacho";
    public const string DespachoCarga = "Despacho consolidado de carga";
}

public static class TiposSoporteFinanciero
{
    public const string FacturaElectronicaProveedor = "Factura electrónica de proveedor (con validación DIAN)";
    public const string DocumentoSoporteNoObligados = "Documento soporte en adquisiciones a no obligados a facturar (DIAN)";
    public const string FacturaVentaAsociacion = "Factura de venta expedida por la asociación";
    public const string ComprobanteInternoAsoagro = "Comprobante interno de operación / recibo ASOAGRO (No tributario)";
    public const string ReciboCajaMenor = "Recibo de caja menor / Comprobante de egreso menor";
    public const string ExtractoConsignacion = "Comprobante de transferencia o consignación bancaria";
    public const string CuentaCobroConRut = "Cuenta de cobro con RUT anexo";
    public const string SoportePesajeBascula = "Tiquete de báscula / Soporte de pesaje y despacho";

    public static readonly string[] Todos = new[]
    {
        FacturaElectronicaProveedor,
        DocumentoSoporteNoObligados,
        FacturaVentaAsociacion,
        ComprobanteInternoAsoagro,
        ReciboCajaMenor,
        ExtractoConsignacion,
        CuentaCobroConRut,
        SoportePesajeBascula
    };
}

public static class EstadosMovimientoFinanciero
{
    public const string Registrado = "Registrado";
    public const string PendienteRevision = "Pendiente de revisión";
    public const string Validado = "Validado";
    public const string Pagado = "Pagado";
    public const string AbonoParcial = "Abono parcial";
    public const string PorCobrar = "Por cobrar";
    public const string PorPagar = "Por pagar";
    public const string Observado = "Observado";
    public const string Anulado = "Anulado";

    public static readonly string[] Todos = new[]
    {
        Registrado,
        PendienteRevision,
        Validado,
        Pagado,
        AbonoParcial,
        PorCobrar,
        PorPagar,
        Observado,
        Anulado
    };
}

public static class MetodosPagoFinanciero
{
    public const string TransferenciaBancaria = "Transferencia bancaria";
    public const string Consignacion = "Consignación bancaria";
    public const string Efectivo = "Efectivo en caja";
    public const string Cheque = "Cheque comercial";
    public const string EnEspecie = "En especie (producto agrícola)";
    public const string PagoMovil = "Pago electrónico / PSE / Nequi / Daviplata";

    public static readonly string[] Todos = new[]
    {
        TransferenciaBancaria,
        Consignacion,
        Efectivo,
        Cheque,
        PagoMovil,
        EnEspecie
    };
}

public class RegistroAuditoriaFinanciera
{
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string Usuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
}

public class MovimientoFinanciero
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CodigoReferencia { get; set; } = string.Empty;
    public string NombreAsociacion { get; set; } = string.Empty;
    public string NitAsociacion { get; set; } = string.Empty;

    // Clasificación de la operación
    public TipoMovimientoFinanciero Tipo { get; set; } = TipoMovimientoFinanciero.Ingreso;
    public string Subtipo { get; set; } = SubtiposMovimientoFinanciero.VentaProduccion;
    public string Categoria { get; set; } = "Venta de producción";
    public string Concepto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string OrigenOMotivo { get; set; } = string.Empty;
    public string ActividadOProyecto { get; set; } = "Operación general de la asociación";

    // Detalle específico para costos y gastos
    public string? QueSePago { get; set; }
    public string? AQuienSePago
    {
        get => BeneficiarioOProveedor;
        set => BeneficiarioOProveedor = value;
    }
    public string? ParaQueSePago { get; set; }
    public string? ActividadONecesidad { get; set; }

    // Tercero relacionado (Proveedor, Cliente, Asociado, Transportador, etc.)
    public string? BeneficiarioOProveedor { get; set; }
    public string? DocumentoIdentidadTercero { get; set; }
    public string TipoTercero { get; set; } = "Tercero / Proveedor";

    // Valores en COP y condiciones de pago
    public decimal Monto { get; set; }
    public decimal ValorPagado { get; set; }
    public decimal SaldoPendiente => Math.Max(0, Monto - ValorPagado);
    public string MetodoPago { get; set; } = MetodosPagoFinanciero.TransferenciaBancaria;

    // Fechas
    public DateTime FechaMovimiento { get; set; } = DateTime.Today;
    public DateTime? FechaVencimiento { get; set; }

    // Estado del movimiento
    public string Estado { get; set; } = EstadosMovimientoFinanciero.PendienteRevision;
    public string? NotaObservacion { get; set; }
    public string? MotivoAnulacion { get; set; }

    // Facturación y soportes normativos
    public string TipoSoporte { get; set; } = TiposSoporteFinanciero.ComprobanteInternoAsoagro;
    public bool TieneFacturaProveedor { get; set; }
    public string? NumeroFactura { get; set; }
    public string? CufeCude { get; set; }
    public bool EsFacturaInternaAsoagro { get; set; } = true;
    public string? RegimenTributarioAsociacion { get; set; } = "No responsable de IVA / No obligada a facturar";
    public bool AsociacionObligadaAFacturar { get; set; } = false;

    // Documento de soporte adjunto (factura recibida, comprobante de pago o comprobante interno)
    public string? ArchivoNombre { get; set; }
    public string? ArchivoUrl { get; set; }
    public string? ArchivoTamano { get; set; }
    public byte[]? ArchivoContenido { get; set; }
    public string? ArchivoTipoMime { get; set; } = "application/pdf";

    // Comprobante de pago adicional (ej. transferencia, consignación bancaria)
    public string? ComprobantePagoNombre { get; set; }
    public string? ComprobantePagoUrl { get; set; }
    public byte[]? ComprobantePagoContenido { get; set; }

    // Auditoría estricta y trazabilidad
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public string CreadoPor { get; set; } = "Tesorera General";
    public string RolCreador { get; set; } = "Tesorera";

    public DateTime? FechaModificacion { get; set; }
    public string? ModificadoPor { get; set; }
    public string? RolModificador { get; set; }

    public List<RegistroAuditoriaFinanciera> Auditoria { get; set; } = new();

    public void RegistrarAuditoria(string usuario, string rol, string accion, string detalle)
    {
        FechaModificacion = DateTime.Now;
        ModificadoPor = usuario;
        RolModificador = rol;

        Auditoria.Insert(0, new RegistroAuditoriaFinanciera
        {
            Fecha = DateTime.Now,
            Usuario = usuario,
            Rol = rol,
            Accion = accion,
            Detalle = detalle
        });
    }
}

public class ResumenFinanciero
{
    public string AsociacionFiltro { get; set; } = "Todas";
    public string PeriodoFiltro { get; set; } = "Todos los periodos";

    public decimal TotalIngresos { get; set; }
    public decimal TotalCostosGastos { get; set; }
    public decimal TotalCostosYGastos { get => TotalCostosGastos; set => TotalCostosGastos = value; }
    public decimal CuentasPorCobrar { get; set; }
    public decimal TotalCuentasPorCobrar { get => CuentasPorCobrar; set => CuentasPorCobrar = value; }
    public decimal CuentasPorPagar { get; set; }
    public decimal TotalCuentasPorPagar { get => CuentasPorPagar; set => CuentasPorPagar = value; }

    public decimal SaldoNeto => TotalIngresos - TotalCostosGastos;
    public decimal SaldoNetoCalculable => SaldoNeto;

    public int TotalMovimientos { get; set; }
    public int PendientesRevision { get; set; }
    public int CantidadPendientesRevision { get => PendientesRevision; set => PendientesRevision = value; }
    public int CantidadRegistrados { get; set; }
    public int Validados { get; set; }
    public int CantidadValidados { get => Validados; set => Validados = value; }
    public int Pagados { get; set; }
    public int CantidadPagados { get => Pagados; set => Pagados = value; }
    public int Observados { get; set; }
    public int CantidadObservados { get => Observados; set => Observados = value; }
    public int Anulados { get; set; }
    public int CantidadAnulados { get => Anulados; set => Anulados = value; }

    public bool DatosSuficientes => TotalMovimientos > 0;
}

public class ResultadoOperacionFinanciera
{
    public bool Exitoso { get; set; }
    public bool Exito { get => Exitoso; set => Exitoso = value; }
    public string Mensaje { get; set; } = string.Empty;
    public MovimientoFinanciero? Movimiento { get; set; }

    public static ResultadoOperacionFinanciera Ok(MovimientoFinanciero mov, string mensaje = "Operación realizada con éxito.")
        => new() { Exitoso = true, Mensaje = mensaje, Movimiento = mov };

    public static ResultadoOperacionFinanciera Error(string mensaje)
        => new() { Exitoso = false, Mensaje = mensaje };
}

public class ArchivoDescargaFinanciera
{
    public bool Disponible { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string TipoMime { get; set; } = "application/pdf";
    public byte[] Contenido { get; set; } = Array.Empty<byte>();
    public string Mensaje { get; set; } = string.Empty;

    public static ArchivoDescargaFinanciera Ok(string nombre, byte[] contenido, string mime = "application/pdf") =>
        new() { Disponible = true, NombreArchivo = nombre, Contenido = contenido, TipoMime = mime };

    public static ArchivoDescargaFinanciera NoDisponible(string mensaje) =>
        new() { Disponible = false, Mensaje = mensaje };
}


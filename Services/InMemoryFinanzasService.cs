using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Asoagro.Models;

namespace Asoagro.Services;

public class InMemoryFinanzasService : IFinanzasService
{
    private static readonly ConcurrentDictionary<Guid, MovimientoFinanciero> Movimientos = new();
    private static int _consecutivo = 100;
    private readonly object _sync = new();

    private readonly IDocumentoAsociacionService _documentoService;
    private readonly NotificacionService _notificaciones;

    public event Action? DatosFinancierosActualizados;

    public InMemoryFinanzasService(IDocumentoAsociacionService documentoService, NotificacionService notificaciones)
    {
        _documentoService = documentoService;
        _notificaciones = notificaciones;

        InicializarDatosSiVacio();
    }

    private void InicializarDatosSiVacio()
    {
        if (!Movimientos.IsEmpty) return;

        lock (_sync)
        {
            if (!Movimientos.IsEmpty) return;

            var asocAsoagro = "ASOAGRO - Asociación Agropecuaria de Urabá";
            var nitAsoagro = "900543210-8";

            var asocPlatano = "ASOPLÁTANO - Asociación de Productores de Plátano de Urabá";
            var nitPlatano = "901234567-1";

            var asocCacao = "ASOCACAO - Asociación de Cacaoteros del Eje Bananero";
            var nitCacao = "900876543-2";

            // 1. Ingreso Validado - Venta de producción (Plátano exportación)
            var mov1 = new MovimientoFinanciero
            {
                CodigoReferencia = "ING-2026-001",
                NombreAsociacion = asocAsoagro,
                NitAsociacion = nitAsoagro,
                Tipo = TipoMovimientoFinanciero.Ingreso,
                Subtipo = SubtiposMovimientoFinanciero.VentaProduccion,
                Categoria = "Venta de producción",
                Concepto = "Comercialización de 45 toneladas de plátano verde tipo exportación",
                Descripcion = "Despacho consolidado de 45 toneladas de plátano primera calidad desde centro de acopio Carepa con destino puerto marítimo.",
                OrigenOMotivo = "Comercialización directa en bloque acordada en mesa de precios con la comercializadora internacional.",
                ActividadOProyecto = "Línea Productiva de Plátano Exportación - Bloque Norte",
                Monto = 48500000m,
                ValorPagado = 48500000m,
                FechaMovimiento = DateTime.Today.AddDays(-12),
                BeneficiarioOProveedor = "C.I. Banacol / Unibán S.A.",
                DocumentoIdentidadTercero = "890900123-4",
                TipoTercero = "Cliente / Comercializadora",
                MetodoPago = MetodosPagoFinanciero.TransferenciaBancaria,
                Estado = EstadosMovimientoFinanciero.Validado,
                TipoSoporte = TiposSoporteFinanciero.FacturaVentaAsociacion,
                NumeroFactura = "FV-0042",
                TieneFacturaProveedor = true,
                EsFacturaInternaAsoagro = true,
                RegimenTributarioAsociacion = "Régimen Ordinario (Obligada a facturar)",
                AsociacionObligadaAFacturar = true,
                ArchivoNombre = "Factura_Venta_Platano_45Ton_Export.pdf",
                ArchivoTamano = "2.4 MB",
                ComprobantePagoNombre = "Comprobante_Transferencia_Bancolombia_48M.pdf",
                CreadoPor = "Tesorera General",
                RolCreador = "Tesorera",
                FechaCreacion = DateTime.Today.AddDays(-12),
                ModificadoPor = "Tesorera General",
                RolModificador = "Tesorera",
                FechaModificacion = DateTime.Today.AddDays(-10)
            };
            mov1.RegistrarAuditoria("Tesorera General", "Tesorera", "Registro y validación de venta", "Venta radicada con soporte de factura interna y confirmación de transferencia Bancolombia.");
            Movimientos[mov1.Id] = mov1;

            // 2. Costo y Gasto Pagado - Insumos con Factura Electrónica de Proveedor
            var mov2 = new MovimientoFinanciero
            {
                CodigoReferencia = "COS-2026-002",
                NombreAsociacion = asocAsoagro,
                NitAsociacion = nitAsoagro,
                Tipo = TipoMovimientoFinanciero.CostoGasto,
                Subtipo = SubtiposMovimientoFinanciero.InsumosFertilizantes,
                Categoria = "Insumos y fertilizantes",
                Concepto = "Adquisición comunitaria de fertilizante orgánico edáfico",
                Descripcion = "Compra al por mayor de 240 bultos de abono orgánico mineralizado para 24 familias productoras asociadas.",
                OrigenOMotivo = "Plan de choque de fertilización acordado en el comité técnico de producción.",
                ActividadOProyecto = "Plan Comunitario de Nutrición de Cultivos 2026",
                QueSePago = "240 bultos de fertilizante edáfico certificado ICA Lote B-18",
                AQuienSePago = "Agroquímicos de Urabá SAS (NIT: 900765432-1)",
                ParaQueSePago = "Garantizar el llenado de racimo y mantener el calibre de exportación en parcelas socias.",
                ActividadONecesidad = "Necesidad de abastecimiento conjunto para reducir costos unitarios en un 18%.",
                BeneficiarioOProveedor = "Agroquímicos de Urabá SAS",
                DocumentoIdentidadTercero = "900765432-1",
                TipoTercero = "Proveedor de Insumos",
                Monto = 14200000m,
                ValorPagado = 14200000m,
                MetodoPago = MetodosPagoFinanciero.TransferenciaBancaria,
                FechaMovimiento = DateTime.Today.AddDays(-20),
                FechaVencimiento = DateTime.Today.AddDays(-5),
                Estado = EstadosMovimientoFinanciero.Pagado,
                TipoSoporte = TiposSoporteFinanciero.FacturaElectronicaProveedor,
                NumeroFactura = "FE-99823",
                CufeCude = "a8f3b201948576c029384756bcdef90123456789abcdef0123456789abcdef01",
                TieneFacturaProveedor = true,
                ArchivoNombre = "Factura_Electronica_DIAN_FE99823.pdf",
                ArchivoTamano = "1.8 MB",
                ComprobantePagoNombre = "Transferencia_Agroquimicos_14M.pdf",
                CreadoPor = "Tesorera General",
                RolCreador = "Tesorera",
                FechaCreacion = DateTime.Today.AddDays(-20),
                ModificadoPor = "Tesorera General",
                RolModificador = "Tesorera",
                FechaModificacion = DateTime.Today.AddDays(-6)
            };
            mov2.RegistrarAuditoria("Tesorera General", "Tesorera", "Desembolso y liquidación", "Factura de proveedor verificada en el portal DIAN y desembolso conciliado.");
            Movimientos[mov2.Id] = mov2;

            // 3. Gasto con Documento Soporte a No Obligados - Mano de obra y jornales de cosecha
            var mov3 = new MovimientoFinanciero
            {
                CodigoReferencia = "COS-2026-003",
                NombreAsociacion = asocPlatano,
                NitAsociacion = nitPlatano,
                Tipo = TipoMovimientoFinanciero.CostoGasto,
                Subtipo = SubtiposMovimientoFinanciero.ManoDeObra,
                Categoria = "Mano de obra y cuadrillas",
                Concepto = "Pago de cuadrilla comunitaria de empaque y desmane",
                Descripcion = "Jornales de cuadrilla de 6 trabajadores agrícolas para selección, desmane, lavado y encajonado de 2 contenedores.",
                OrigenOMotivo = "Mano de obra especializada requerida durante la ventana de corte para no perder el barco.",
                ActividadOProyecto = "Cosecha y Embarque Semana 44 - ASOPLÁTANO",
                QueSePago = "36 jornales técnicos de selección, empaque al vacío y pesaje",
                AQuienSePago = "Cuadrilla La Esperanza - Resp. Arley Palacios (CC 71.890.123)",
                ParaQueSePago = "Asegurar la inocuidad y cumplimiento de especificaciones GlobalGAP en bodega.",
                ActividadONecesidad = "Fuerza operativa temporal para la logística de exportación de la asociación.",
                BeneficiarioOProveedor = "Arley Palacios Córdoba (Cuadrilla La Esperanza)",
                DocumentoIdentidadTercero = "71890123",
                TipoTercero = "Prestador de Servicios Agrícolas",
                Monto = 3240000m,
                ValorPagado = 3240000m,
                MetodoPago = MetodosPagoFinanciero.Efectivo,
                FechaMovimiento = DateTime.Today.AddDays(-8),
                Estado = EstadosMovimientoFinanciero.Pagado,
                TipoSoporte = TiposSoporteFinanciero.DocumentoSoporteNoObligados,
                NumeroFactura = "DS-0015",
                TieneFacturaProveedor = false,
                ArchivoNombre = "Documento_Soporte_NoObligados_DS0015.pdf",
                ArchivoTamano = "850 KB",
                ComprobantePagoNombre = "Recibo_Caja_Firmado_Cuadrilla.pdf",
                CreadoPor = "Tesorera General",
                RolCreador = "Tesorera",
                FechaCreacion = DateTime.Today.AddDays(-8)
            };
            mov3.RegistrarAuditoria("Tesorera General", "Tesorera", "Registro con Documento Soporte", "Se expidió Documento Soporte en Adquisiciones a No Obligados conforme a reglamentación DIAN.");
            Movimientos[mov3.Id] = mov3;

            // 4. Ingreso - Cuota de Sostenimiento de Asociados
            var mov4 = new MovimientoFinanciero
            {
                CodigoReferencia = "ING-2026-004",
                NombreAsociacion = asocCacao,
                NitAsociacion = nitCacao,
                Tipo = TipoMovimientoFinanciero.Ingreso,
                Subtipo = SubtiposMovimientoFinanciero.CuotaOrdinaria,
                Categoria = "Cuotas y aportes sociales",
                Concepto = "Recaudo mensual de cuotas estatutarias de sostenimiento",
                Descripcion = "Pago consolidado de cuotas estatutarias ordinarias correspondientes a 32 asociados activos mes de octubre.",
                OrigenOMotivo = "Cumplimiento de la obligación estatutaria aprobada en la Asamblea Ordinaria de delegados.",
                ActividadOProyecto = "Fondo de Sostenimiento y Gobernanza ASOCACAO",
                BeneficiarioOProveedor = "Padrón de Asociados ASOCACAO (32 integrantes)",
                DocumentoIdentidadTercero = "900876543-2",
                TipoTercero = "Asociados de Base",
                Monto = 1600000m,
                ValorPagado = 1600000m,
                MetodoPago = MetodosPagoFinanciero.Consignacion,
                FechaMovimiento = DateTime.Today.AddDays(-4),
                Estado = EstadosMovimientoFinanciero.Validado,
                TipoSoporte = TiposSoporteFinanciero.ComprobanteInternoAsoagro,
                NumeroFactura = "RC-2026-032",
                TieneFacturaProveedor = false,
                EsFacturaInternaAsoagro = true,
                ArchivoNombre = "Recibo_Caja_Consolidado_Cuotas_Oct2026.pdf",
                ArchivoTamano = "1.2 MB",
                CreadoPor = "Tesorera General",
                RolCreador = "Tesorera",
                FechaCreacion = DateTime.Today.AddDays(-4)
            };
            mov4.RegistrarAuditoria("Tesorera General", "Tesorera", "Validación de recaudo asociativo", "Consignación verificada en extracto de la cuenta de ahorros de la asociación.");
            Movimientos[mov4.Id] = mov4;

            // 5. Cuenta por Cobrar - Venta con Saldo Pendiente
            var mov5 = new MovimientoFinanciero
            {
                CodigoReferencia = "ING-2026-005",
                NombreAsociacion = asocPlatano,
                NitAsociacion = nitPlatano,
                Tipo = TipoMovimientoFinanciero.Ingreso,
                Subtipo = SubtiposMovimientoFinanciero.VentaProduccion,
                Categoria = "Venta de producción",
                Concepto = "Venta a crédito de 18 toneladas de plátano nacional",
                Descripcion = "Despacho a mercado mayorista de Medellín. Plazo de pago de 30 días calendario pactado según remisión.",
                OrigenOMotivo = "Comercialización de producto de segunda categoría no apto para embarque de exportación.",
                ActividadOProyecto = "Canal de Mercado Nacional - ASOPLÁTANO",
                BeneficiarioOProveedor = "Distribuidora Agrícola del Valle SAS",
                DocumentoIdentidadTercero = "800192837-5",
                TipoTercero = "Cliente Comercializador",
                Monto = 12600000m,
                ValorPagado = 4000000m, // Abono parcial
                FechaMovimiento = DateTime.Today.AddDays(-15),
                FechaVencimiento = DateTime.Today.AddDays(15),
                Estado = EstadosMovimientoFinanciero.PorCobrar,
                TipoSoporte = TiposSoporteFinanciero.FacturaVentaAsociacion,
                NumeroFactura = "FV-0089",
                ArchivoNombre = "Remision_Factura_MercadoNacional_FV0089.pdf",
                ArchivoTamano = "1.4 MB",
                ComprobantePagoNombre = "Anticipo_Bancario_4M.pdf",
                CreadoPor = "Tesorera General",
                RolCreador = "Tesorera",
                FechaCreacion = DateTime.Today.AddDays(-15)
            };
            mov5.RegistrarAuditoria("Tesorera General", "Tesorera", "Registro de cuenta por cobrar", "Se radicó la venta y se registró el anticipo de $4.000.000 COP; saldo pendiente: $8.600.000 COP.");
            Movimientos[mov5.Id] = mov5;

            // 6. Cuenta por Pagar - Costo de Transporte / Flete pendiente
            var mov6 = new MovimientoFinanciero
            {
                CodigoReferencia = "DES-2026-006",
                NombreAsociacion = asocAsoagro,
                NitAsociacion = nitAsoagro,
                Tipo = TipoMovimientoFinanciero.PagoDespacho,
                Subtipo = SubtiposMovimientoFinanciero.TransporteFlete,
                Categoria = "Transporte y fletes",
                Concepto = "Servicio de transporte de carga refrigerada hacia puerto",
                Descripcion = "Flete de tractomula termoking para movilización de 2 contenedores de fruta hacia Puerto Bahía Colombia de Urabá.",
                OrigenOMotivo = "Transporte marítimo programado en el plan logístico de exportación.",
                ActividadOProyecto = "Logística y Fletes de Despacho",
                BeneficiarioOProveedor = "Transportes del Golfo de Urabá SAS",
                DocumentoIdentidadTercero = "900345678-9",
                TipoTercero = "Transportador de Carga",
                Monto = 5800000m,
                ValorPagado = 0m,
                FechaMovimiento = DateTime.Today.AddDays(-5),
                FechaVencimiento = DateTime.Today.AddDays(10),
                Estado = EstadosMovimientoFinanciero.PorPagar,
                TipoSoporte = TiposSoporteFinanciero.FacturaElectronicaProveedor,
                NumeroFactura = "FET-4412",
                TieneFacturaProveedor = true,
                ArchivoNombre = "Factura_Flete_Termoking_FET4412.pdf",
                ArchivoTamano = "1.1 MB",
                CreadoPor = "Tesorera General",
                RolCreador = "Tesorera",
                FechaCreacion = DateTime.Today.AddDays(-5)
            };
            mov6.RegistrarAuditoria("Tesorera General", "Tesorera", "Registro de obligación por pagar", "Factura de flete radicada; programada para pago en la siguiente remesa de tesorería.");
            Movimientos[mov6.Id] = mov6;
        }
    }

    public IReadOnlyList<MovimientoFinanciero> ObtenerTodos(string? asociacion = null, string? periodo = null, TipoMovimientoFinanciero? tipo = null, string? estado = null)
    {
        var query = Movimientos.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(asociacion) && !asociacion.Equals("Todas", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(m => m.NombreAsociacion.Equals(asociacion, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(periodo) && !periodo.Equals("Todos", StringComparison.OrdinalIgnoreCase) && !periodo.Equals("Todos los periodos", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(m => m.FechaMovimiento.ToString("MM/yyyy").Equals(periodo, StringComparison.OrdinalIgnoreCase) ||
                                     m.FechaMovimiento.ToString("yyyy-MM").Equals(periodo, StringComparison.OrdinalIgnoreCase));
        }

        if (tipo.HasValue)
        {
            query = query.Where(m => m.Tipo == tipo.Value);
        }

        if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(m => m.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderByDescending(m => m.FechaMovimiento).ThenByDescending(m => m.FechaCreacion).ToList();
    }

    public MovimientoFinanciero? ObtenerPorId(Guid id)
    {
        Movimientos.TryGetValue(id, out var mov);
        return mov;
    }

    public ResumenFinanciero ObtenerResumen(string? asociacion = null, string? periodo = null)
    {
        var movimientos = ObtenerTodos(asociacion, periodo);

        var resumen = new ResumenFinanciero
        {
            AsociacionFiltro = string.IsNullOrWhiteSpace(asociacion) ? "Todas" : asociacion,
            PeriodoFiltro = string.IsNullOrWhiteSpace(periodo) ? "Todos los periodos" : periodo,
            TotalMovimientos = movimientos.Count,
            PendientesRevision = movimientos.Count(m => m.Estado == EstadosMovimientoFinanciero.PendienteRevision),
            Validados = movimientos.Count(m => m.Estado == EstadosMovimientoFinanciero.Validado),
            Pagados = movimientos.Count(m => m.Estado == EstadosMovimientoFinanciero.Pagado),
            Observados = movimientos.Count(m => m.Estado == EstadosMovimientoFinanciero.Observado),
            Anulados = movimientos.Count(m => m.Estado == EstadosMovimientoFinanciero.Anulado)
        };

        if (movimientos.Count == 0)
        {
            return resumen;
        }

        // Ingresos: suma de ingresos válidos (no anulados)
        resumen.TotalIngresos = movimientos
            .Where(m => m.Tipo == TipoMovimientoFinanciero.Ingreso &&
                        m.Estado != EstadosMovimientoFinanciero.Anulado)
            .Sum(m => m.Monto);

        // Costos y Gastos: suma de Costos/Gastos y Pagos/Despachos válidos
        resumen.TotalCostosGastos = movimientos
            .Where(m => (m.Tipo == TipoMovimientoFinanciero.CostoGasto || m.Tipo == TipoMovimientoFinanciero.PagoDespacho) &&
                        m.Estado != EstadosMovimientoFinanciero.Anulado)
            .Sum(m => m.Monto);

        // Cuentas por cobrar: Saldos pendientes de cobro en ventas e ingresos
        resumen.CuentasPorCobrar = movimientos
            .Where(m => m.Tipo == TipoMovimientoFinanciero.Ingreso &&
                        m.Estado != EstadosMovimientoFinanciero.Pagado &&
                        m.Estado != EstadosMovimientoFinanciero.Anulado)
            .Sum(m => m.SaldoPendiente > 0 ? m.SaldoPendiente : m.Monto);

        // Cuentas por pagar: Saldos pendientes de pago a proveedores o prestadores
        resumen.CuentasPorPagar = movimientos
            .Where(m => (m.Tipo == TipoMovimientoFinanciero.CostoGasto || m.Tipo == TipoMovimientoFinanciero.PagoDespacho) &&
                        m.Estado != EstadosMovimientoFinanciero.Pagado &&
                        m.Estado != EstadosMovimientoFinanciero.Anulado)
            .Sum(m => m.SaldoPendiente > 0 ? m.SaldoPendiente : m.Monto);

        return resumen;
    }

    public IReadOnlyList<string> ObtenerAsociaciones()
    {
        var lista = _documentoService.ObtenerAsociaciones().ToList();
        if (!lista.Any(a => a.Contains("ASOAGRO", StringComparison.OrdinalIgnoreCase)))
        {
            lista.Insert(0, "ASOAGRO - Asociación Agropecuaria de Urabá");
        }
        return lista;
    }

    public IReadOnlyList<string> ObtenerPeriodosDisponibles()
    {
        var periodos = Movimientos.Values
            .Select(m => m.FechaMovimiento.ToString("MM/yyyy"))
            .Distinct()
            .OrderByDescending(p => p)
            .ToList();

        if (periodos.Count == 0)
        {
            periodos.Add(DateTime.Today.ToString("MM/yyyy"));
        }

        return periodos;
    }

    public AsociacionInstitucional? ObtenerConfiguracionTributaria(string asociacion)
    {
        try
        {
            var expediente = _documentoService.ObtenerExpediente(asociacion);
            if (expediente != null && !string.IsNullOrWhiteSpace(expediente.NombreAsociacion))
            {
                return new AsociacionInstitucional
                {
                    NombreLegal = expediente.NombreAsociacion,
                    Nit = expediente.NitAsociacion,
                    RegimenTributario = "Régimen Ordinario",
                    Municipio = "Apartadó",
                    Departamento = "Antioquia"
                };
            }
        }
        catch { }

        return null;
    }

    // REGISTRO DE MOVIMIENTO
    public ResultadoOperacionFinanciera RegistrarMovimiento(MovimientoFinanciero movimiento, string usuario, string rol)
    {
        // Validación de permisos estricta
        if (EsPresidencia(rol) || EsSecretaria(rol))
        {
            return ResultadoOperacionFinanciera.Error($"El rol {rol} tiene permisos estrictos de consulta y no puede crear movimientos financieros.");
        }

        if (movimiento.Monto <= 0)
        {
            return ResultadoOperacionFinanciera.Error("El valor del movimiento debe ser superior a $0 COP.");
        }

        if (string.IsNullOrWhiteSpace(movimiento.NombreAsociacion))
        {
            return ResultadoOperacionFinanciera.Error("Debe seleccionar la asociación titular de la operación.");
        }

        if (string.IsNullOrWhiteSpace(movimiento.Descripcion))
        {
            return ResultadoOperacionFinanciera.Error("Debe ingresar una descripción clara del movimiento.");
        }

        // Si es gasto, verificar que se indique qué se pagó y a quién
        if (movimiento.Tipo == TipoMovimientoFinanciero.CostoGasto)
        {
            if (string.IsNullOrWhiteSpace(movimiento.BeneficiarioOProveedor))
            {
                return ResultadoOperacionFinanciera.Error("En los costos y gastos es obligatorio registrar a quién se le pagó (proveedor o beneficiario).");
            }
        }

        lock (_sync)
        {
            _consecutivo++;
            var prefijo = movimiento.Tipo switch
            {
                TipoMovimientoFinanciero.Ingreso => "ING",
                TipoMovimientoFinanciero.CostoGasto => "COS",
                TipoMovimientoFinanciero.PagoDespacho => "DES",
                _ => "OP"
            };
            movimiento.CodigoReferencia = $"{prefijo}-{DateTime.Today.Year}-{_consecutivo:D3}";
            movimiento.FechaCreacion = DateTime.Now;
            movimiento.CreadoPor = usuario;
            movimiento.RolCreador = rol;

            // Si lo crea Tesorería directamente, queda Validado
            if (EsTesoreria(rol))
            {
                if (movimiento.Estado == EstadosMovimientoFinanciero.PendienteRevision)
                {
                    movimiento.Estado = EstadosMovimientoFinanciero.Validado;
                }
                movimiento.RegistrarAuditoria(usuario, rol, "Registro directo por Tesorería", $"Movimiento creado y verificado por la Tesorera ({usuario}).");
            }
            else
            {
                // Administrador ingresa nuevo movimiento: queda estrictamente pendiente de revisión
                movimiento.Estado = EstadosMovimientoFinanciero.PendienteRevision;
                movimiento.RegistrarAuditoria(usuario, rol, "Registro operativo administrativo", $"Ingreso de operación por {usuario}. Remitido a Tesorería para revisión y validación.");
            }

            Movimientos[movimiento.Id] = movimiento;
        }

        // Notificación
        _notificaciones.Publicar(new Notificacion
        {
            Titulo = $"Nueva operación registrada: {movimiento.CodigoReferencia}",
            Descripcion = $"{movimiento.Tipo}: {movimiento.Descripcion} por ${movimiento.Monto:N0} COP ({movimiento.NombreAsociacion}). Registrado por {usuario}.",
            Etiqueta = "Finanzas",
            Enlace = "/panel-tesorero",
            Icono = "doc",
            Categoria = CategoriaNotificacion.Finanzas,
            Nivel = NivelNotificacion.Informativa,
            RolesDestino = new[] { "Tesorero", "Tesorera" },
            RolesEnlace = new[] { "Tesorero", "Tesorera", "Administrador", "Admin" },
            ClaveUnica = $"fin-reg-{movimiento.Id}"
        });

        NotificarCambioDatos();
        return ResultadoOperacionFinanciera.Ok(movimiento, $"Operación {movimiento.CodigoReferencia} radicada exitosamente.");
    }

    // EDICIÓN / CORRECCIÓN DE MOVIMIENTO (SOLO TESORERÍA)
    public ResultadoOperacionFinanciera ActualizarMovimiento(Guid id, Action<MovimientoFinanciero> actualizador, string usuario, string rol)
    {
        if (!EsTesoreria(rol))
        {
            return ResultadoOperacionFinanciera.Error($"Permiso denegado. Solo la Tesorera puede editar o corregir movimientos financieros guardados. Los administradores no tienen permisos de modificación.");
        }

        if (!Movimientos.TryGetValue(id, out var mov))
        {
            return ResultadoOperacionFinanciera.Error("El movimiento financiero no existe o fue retirado.");
        }

        if (mov.Estado == EstadosMovimientoFinanciero.Anulado)
        {
            return ResultadoOperacionFinanciera.Error("No se pueden editar movimientos que ya han sido anulados.");
        }

        lock (_sync)
        {
            actualizador(mov);
            mov.RegistrarAuditoria(usuario, rol, "Corrección y actualización", $"Ajuste formal de datos efectuado por la Tesorera ({usuario}).");
        }

        _notificaciones.Publicar(new Notificacion
        {
            Titulo = $"Movimiento corregido: {mov.CodigoReferencia}",
            Descripcion = $"Tesorería actualizó los datos de {mov.CodigoReferencia} ({mov.NombreAsociacion}).",
            Etiqueta = "Finanzas",
            Enlace = "/panel-tesorero",
            Icono = "ajustes",
            Categoria = CategoriaNotificacion.Finanzas,
            Nivel = NivelNotificacion.Informativa,
            RolesDestino = new[] { "Administrador", "Admin", "Tesorero", "Tesorera" },
            RolesEnlace = new[] { "Tesorero", "Tesorera", "Administrador", "Admin" },
            ClaveUnica = $"fin-act-{mov.Id}-{DateTime.Now.Ticks}"
        });

        NotificarCambioDatos();
        return ResultadoOperacionFinanciera.Ok(mov, $"El registro {mov.CodigoReferencia} fue corregido y actualizado correctamente.");
    }

    // VALIDACIÓN DE MOVIMIENTO (SOLO TESORERÍA)
    public ResultadoOperacionFinanciera ValidarMovimiento(Guid id, string usuario, string rol, string? observaciones = null)
    {
        if (!EsTesoreria(rol))
        {
            return ResultadoOperacionFinanciera.Error("Permiso denegado. El Administrador no puede aprobar registros financieros. La validación es función exclusiva de Tesorería.");
        }

        if (!Movimientos.TryGetValue(id, out var mov))
        {
            return ResultadoOperacionFinanciera.Error("El movimiento no fue encontrado.");
        }

        lock (_sync)
        {
            mov.Estado = EstadosMovimientoFinanciero.Validado;
            mov.NotaObservacion = null;
            mov.RegistrarAuditoria(usuario, rol, "Validación contable", string.IsNullOrWhiteSpace(observaciones) ? "Operación validada y respaldada conforme a soportes de Tesorería." : observaciones);
        }

        _notificaciones.Publicar(new Notificacion
        {
            Titulo = $"Operación validada: {mov.CodigoReferencia}",
            Descripcion = $"Tesorería validó la operación de ${mov.Monto:N0} COP en {mov.NombreAsociacion}.",
            Etiqueta = "Finanzas",
            Enlace = "/panel-tesorero",
            Icono = "check",
            Categoria = CategoriaNotificacion.Finanzas,
            Nivel = NivelNotificacion.Informativa,
            RolesDestino = new[] { "Administrador", "Admin", "Tesorero", "Tesorera" },
            RolesEnlace = new[] { "Administrador", "Admin", "Tesorero", "Tesorera", "Presidenta", "Presidente" },
            ClaveUnica = $"fin-val-{mov.Id}-{DateTime.Now.Ticks}"
        });

        NotificarCambioDatos();
        return ResultadoOperacionFinanciera.Ok(mov, $"Operación {mov.CodigoReferencia} validada exitosamente.");
    }

    // OBSERVACIÓN DE MOVIMIENTO (SOLO TESORERÍA)
    public ResultadoOperacionFinanciera ObservarMovimiento(Guid id, string observacion, string usuario, string rol)
    {
        if (!EsTesoreria(rol))
        {
            return ResultadoOperacionFinanciera.Error("Solo Tesorería puede emitir observaciones y devoluciones contables.");
        }

        if (string.IsNullOrWhiteSpace(observacion))
        {
            return ResultadoOperacionFinanciera.Error("Debe ingresar la nota u observación de corrección requerida.");
        }

        if (!Movimientos.TryGetValue(id, out var mov))
        {
            return ResultadoOperacionFinanciera.Error("El movimiento no fue encontrado.");
        }

        lock (_sync)
        {
            mov.Estado = EstadosMovimientoFinanciero.Observado;
            mov.NotaObservacion = observacion;
            mov.RegistrarAuditoria(usuario, rol, "Observación emitida", observacion);
        }

        _notificaciones.Publicar(new Notificacion
        {
            Titulo = $"Movimiento observado: {mov.CodigoReferencia}",
            Descripcion = $"Tesorería observó el registro de {mov.NombreAsociacion}: {observacion}",
            Etiqueta = "Finanzas",
            Enlace = "/panel-tesorero",
            Icono = "alerta",
            Categoria = CategoriaNotificacion.Finanzas,
            Nivel = NivelNotificacion.Advertencia,
            RolesDestino = new[] { "Administrador", "Admin", "Tesorero", "Tesorera" },
            RolesEnlace = new[] { "Administrador", "Admin", "Tesorero", "Tesorera" },
            ClaveUnica = $"fin-obs-{mov.Id}-{DateTime.Now.Ticks}"
        });

        NotificarCambioDatos();
        return ResultadoOperacionFinanciera.Ok(mov, $"Observación enviada al responsable del registro.");
    }

    // MARCAR PAGADO / ABONADO (SOLO TESORERÍA)
    public ResultadoOperacionFinanciera MarcarPagado(Guid id, string usuario, string rol, string? comprobante = null, decimal? valorAbonado = null)
    {
        if (!EsTesoreria(rol))
        {
            return ResultadoOperacionFinanciera.Error("Permiso denegado. Solo Tesorería puede registrar pagos, recaudos o abonos.");
        }

        if (!Movimientos.TryGetValue(id, out var mov))
        {
            return ResultadoOperacionFinanciera.Error("El movimiento no fue encontrado.");
        }

        lock (_sync)
        {
            if (valorAbonado.HasValue && valorAbonado.Value > 0 && valorAbonado.Value < mov.Monto)
            {
                mov.ValorPagado = valorAbonado.Value;
                mov.Estado = EstadosMovimientoFinanciero.AbonoParcial;
                if (!string.IsNullOrWhiteSpace(comprobante)) mov.ComprobantePagoNombre = comprobante;
                mov.RegistrarAuditoria(usuario, rol, "Abono parcial registrado", $"Abono de ${valorAbonado.Value:N0} COP registrado por {usuario}. Saldo restante: ${mov.SaldoPendiente:N0} COP.");
            }
            else
            {
                mov.ValorPagado = mov.Monto;
                mov.Estado = EstadosMovimientoFinanciero.Pagado;
                if (!string.IsNullOrWhiteSpace(comprobante))
                {
                    mov.ComprobantePagoNombre = comprobante;
                }
                mov.RegistrarAuditoria(usuario, rol, "Pago/Recaudo liquidado total", $"Liquidado totalmente por {usuario} por ${mov.Monto:N0} COP.");
            }
        }

        _notificaciones.Publicar(new Notificacion
        {
            Titulo = $"Pago registrado: {mov.CodigoReferencia}",
            Descripcion = $"Se registró el pago de ${mov.ValorPagado:N0} COP de {mov.NombreAsociacion}.",
            Etiqueta = "Finanzas",
            Enlace = "/panel-tesorero",
            Icono = "check",
            Categoria = CategoriaNotificacion.Finanzas,
            Nivel = NivelNotificacion.Informativa,
            RolesDestino = new[] { "Tesorero", "Tesorera", "Presidenta", "Presidente", "Secretaria", "Secretaría" },
            RolesEnlace = new[] { "Tesorero", "Tesorera", "Administrador", "Admin", "Presidenta", "Presidente" },
            ClaveUnica = $"fin-pag-{mov.Id}-{DateTime.Now.Ticks}"
        });

        NotificarCambioDatos();
        return ResultadoOperacionFinanciera.Ok(mov, $"El movimiento {mov.CodigoReferencia} fue registrado exitosamente con estado {mov.Estado}.");
    }

    // ANULACIÓN CON MOTIVO OBLIGATORIO (SOLO TESORERÍA)
    public ResultadoOperacionFinanciera AnularMovimiento(Guid id, string motivoAnulacion, string usuario, string rol)
    {
        if (!EsTesoreria(rol))
        {
            return ResultadoOperacionFinanciera.Error("Permiso denegado. La anulación formal de movimientos contables es potestad exclusiva de Tesorería.");
        }

        if (string.IsNullOrWhiteSpace(motivoAnulacion))
        {
            return ResultadoOperacionFinanciera.Error("Debe justificar de forma obligatoria el motivo de anulación del movimiento.");
        }

        if (!Movimientos.TryGetValue(id, out var mov))
        {
            return ResultadoOperacionFinanciera.Error("El movimiento no fue encontrado.");
        }

        lock (_sync)
        {
            mov.Estado = EstadosMovimientoFinanciero.Anulado;
            mov.MotivoAnulacion = motivoAnulacion;
            mov.RegistrarAuditoria(usuario, rol, "Anulación formal justificada", $"Motivo justificado: {motivoAnulacion}");
        }

        _notificaciones.Publicar(new Notificacion
        {
            Titulo = $"Operación anulada: {mov.CodigoReferencia}",
            Descripcion = $"Tesorería anuló {mov.CodigoReferencia} ({mov.NombreAsociacion}). Motivo: {motivoAnulacion}",
            Etiqueta = "Finanzas",
            Enlace = "/panel-tesorero",
            Icono = "alerta",
            Categoria = CategoriaNotificacion.Finanzas,
            Nivel = NivelNotificacion.Advertencia,
            RolesDestino = new[] { "Administrador", "Admin", "Tesorero", "Tesorera" },
            RolesEnlace = new[] { "Tesorero", "Tesorera", "Administrador", "Admin" },
            ClaveUnica = $"fin-anu-{mov.Id}-{DateTime.Now.Ticks}"
        });

        NotificarCambioDatos();
        return ResultadoOperacionFinanciera.Ok(mov, $"Operación {mov.CodigoReferencia} anulada exitosamente con su debida justificación auditable.");
    }

    public byte[] GenerarComprobantePdf(Guid id, string funcionario = "Tesorera General FAAUA")
    {
        if (!Movimientos.TryGetValue(id, out var mov))
        {
            throw new InvalidOperationException("Movimiento financiero no encontrado.");
        }

        var asoc = ObtenerConfiguracionTributaria(mov.NombreAsociacion);
        return ComprobanteFinancieroPdfGenerator.GenerarComprobantePdf(mov, asoc, funcionario);
    }

    public byte[] GenerarReporteConsolidadoPdf(string? asociacion = null, string? periodo = null, string funcionario = "Tesorera General FAAUA")
    {
        var resumen = ObtenerResumen(asociacion, periodo);
        var lista = ObtenerTodos(asociacion, periodo);
        return ComprobanteFinancieroPdfGenerator.GenerarReporteConsolidadoPdf(
            resumen, 
            lista, 
            string.IsNullOrWhiteSpace(asociacion) ? "Todas las asociaciones" : asociacion, 
            string.IsNullOrWhiteSpace(periodo) ? "Todos los periodos" : periodo, 
            funcionario);
    }

    public ArchivoDescargaFinanciera ObtenerSoporteDescarga(Guid id, bool esComprobantePago = false, string funcionario = "Tesorera General FAAUA")
    {
        if (!Movimientos.TryGetValue(id, out var mov))
        {
            return ArchivoDescargaFinanciera.NoDisponible("El movimiento financiero no existe o fue retirado del sistema.");
        }

        if (esComprobantePago)
        {
            if (string.IsNullOrWhiteSpace(mov.ComprobantePagoNombre) && (mov.ComprobantePagoContenido == null || mov.ComprobantePagoContenido.Length == 0))
            {
                return ArchivoDescargaFinanciera.NoDisponible($"El movimiento {mov.CodigoReferencia} ({mov.Descripcion}) no tiene registrado un comprobante de pago bancario anexo.");
            }

            var nombrePago = !string.IsNullOrWhiteSpace(mov.ComprobantePagoNombre) 
                ? mov.ComprobantePagoNombre 
                : $"Comprobante_Pago_{mov.CodigoReferencia}.pdf";

            if (!nombrePago.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && !nombrePago.Contains('.'))
            {
                nombrePago += ".pdf";
            }

            if (mov.ComprobantePagoContenido != null && mov.ComprobantePagoContenido.Length > 0)
            {
                return ArchivoDescargaFinanciera.Ok(nombrePago, mov.ComprobantePagoContenido, "application/pdf");
            }

            // Generar documento formal de pago
            var bytesPago = ComprobanteFinancieroPdfGenerator.GenerarComprobantePdf(mov, ObtenerConfiguracionTributaria(mov.NombreAsociacion), funcionario);
            mov.ComprobantePagoContenido = bytesPago;
            return ArchivoDescargaFinanciera.Ok(nombrePago, bytesPago, "application/pdf");
        }

        // Descarga de Soporte Principal (Factura de venta, Factura de proveedor, Documento soporte)
        if (string.IsNullOrWhiteSpace(mov.ArchivoNombre) && (mov.ArchivoContenido == null || mov.ArchivoContenido.Length == 0))
        {
            return ArchivoDescargaFinanciera.NoDisponible($"El movimiento {mov.CodigoReferencia} ({mov.Descripcion}) no tiene ningún archivo o soporte documental adjunto.");
        }

        var nombreSoporte = !string.IsNullOrWhiteSpace(mov.ArchivoNombre) 
            ? mov.ArchivoNombre 
            : $"Soporte_{mov.CodigoReferencia}.pdf";

        if (!nombreSoporte.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && !nombreSoporte.Contains('.'))
        {
            nombreSoporte += ".pdf";
        }

        var mime = mov.ArchivoTipoMime ?? "application/pdf";

        if (mov.ArchivoContenido != null && mov.ArchivoContenido.Length > 0)
        {
            return ArchivoDescargaFinanciera.Ok(nombreSoporte, mov.ArchivoContenido, mime);
        }

        // Si se registró el nombre de soporte pero aún no se habían guardado los bytes, generamos el PDF válido correspondiente
        var bytes = ComprobanteFinancieroPdfGenerator.GenerarComprobantePdf(mov, ObtenerConfiguracionTributaria(mov.NombreAsociacion), funcionario);
        mov.ArchivoContenido = bytes;
        mov.ArchivoTipoMime = "application/pdf";
        return ArchivoDescargaFinanciera.Ok(nombreSoporte, bytes, "application/pdf");
    }

    private void NotificarCambioDatos()
    {
        try
        {
            DatosFinancierosActualizados?.Invoke();
        }
        catch { }
    }

    // Ayudantes de validación de roles
    private static bool EsTesoreria(string rol) =>
        rol.Equals("Tesorero", StringComparison.OrdinalIgnoreCase) ||
        rol.Equals("Tesorera", StringComparison.OrdinalIgnoreCase);

    private static bool EsAdmin(string rol) =>
        rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) ||
        rol.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    private static bool EsPresidencia(string rol) =>
        rol.Equals("Presidenta", StringComparison.OrdinalIgnoreCase) ||
        rol.Equals("Presidente", StringComparison.OrdinalIgnoreCase);

    private static bool EsSecretaria(string rol) =>
        rol.Equals("Secretaria", StringComparison.OrdinalIgnoreCase) ||
        rol.Equals("Secretaría", StringComparison.OrdinalIgnoreCase);
}

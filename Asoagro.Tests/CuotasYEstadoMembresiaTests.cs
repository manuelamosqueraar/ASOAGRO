using System;
using Asoagro.Models;
using Asoagro.Services;
using Xunit;

namespace Asoagro.Tests;

public class CuotasYEstadoMembresiaTests
{
    [Theory]
    [InlineData("AlDia", "Activo")]
    [InlineData("Activo", "Activo")]
    [InlineData("Suspendido", "Suspendido")]
    [InlineData("Inactivo", "Inactivo")]
    [InlineData("Retirado", "Retirado")]
    public void MapearEstadoMembresiaParaBd_CumpleRestriccionSupabase(string input, string expected)
    {
        var result = AsociadoMapper.MapearEstadoMembresiaParaBd(input);
        Assert.Equal(expected, result);

        // Supabase constraint: CHECK (estado_membresia IN ('Activo', 'Inactivo', 'Suspendido', 'Retirado'))
        string[] permitidos = ["Activo", "Inactivo", "Suspendido", "Retirado"];
        Assert.Contains(result, permitidos);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void MapearEstadoMembresiaParaBd_VacioONulo_LanzaArgumentException(string? input)
    {
        Assert.Throws<ArgumentException>(() => AsociadoMapper.MapearEstadoMembresiaParaBd(input));
    }

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("Desconocido")]
    [InlineData("EnRevision")]
    [InlineData("PruebaInvalida")]
    public void MapearEstadoMembresiaParaBd_EstadoDesconocido_LanzaInvalidOperationException(string input)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => AsociadoMapper.MapearEstadoMembresiaParaBd(input));
        Assert.Contains(input, ex.Message);
        Assert.Contains("Supabase", ex.Message);
    }

    [Theory]
    [InlineData("Activo", "Activo")]
    [InlineData("AlDia", "Activo")]
    [InlineData("Suspendido", "Suspendido")]
    [InlineData("Inactivo", "Inactivo")]
    [InlineData("Retirado", "Retirado")]
    [InlineData("", "Activo")]
    [InlineData(null, "Activo")]
    public void MapearEstadoMembresiaParaUi_MantieneInterfazLegible(string? input, string expected)
    {
        var result = AsociadoMapper.MapearEstadoMembresiaParaUi(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CuotaAporte_CalculoSaldoPendiente_Correcto()
    {
        var cuota = new CuotaAporteAsociado
        {
            ValorCuota = 50000m,
            CantidadCuotasPendientes = 3,
            ValorTotalPagado = 50000m
        };

        cuota.ActualizarEstadoYSoportes();

        // Total establecido: 3 * 50,000 = 150,000
        Assert.Equal(150000m, cuota.TotalEstablecido);
        // Saldo pendiente: 150,000 - 50,000 = 100,000 (sin valores corruptos como 99,999,664)
        Assert.Equal(100000m, cuota.ValorTotalPendiente);
        Assert.Equal("Pago parcial", cuota.EstadoPago);
    }

    [Fact]
    public void CuotaAporte_PagoTotal_SaldoCero()
    {
        var cuota = new CuotaAporteAsociado
        {
            ValorCuota = 50000m,
            CantidadCuotasPendientes = 1,
            ValorTotalPagado = 50000m
        };

        cuota.ActualizarEstadoYSoportes();

        Assert.Equal(0m, cuota.ValorTotalPendiente);
    }

    [Fact]
    public void CuotaAporte_SinCuotasPendientesYPagadoCero_SaldoPendienteEsValorCuota()
    {
        var cuota = new CuotaAporteAsociado
        {
            ValorCuota = 50000m,
            CantidadCuotasPendientes = 0,
            ValorTotalPagado = 0m
        };

        cuota.ActualizarEstadoYSoportes();

        Assert.Equal(50000m, cuota.ValorTotalPendiente);
    }

    [Theory]
    [InlineData("50000", 50000)]
    [InlineData("50.000", 50000)]
    [InlineData("50,000", 50000)]
    [InlineData("$50.000", 50000)]
    [InlineData("$ 50,000 COP", 50000)]
    [InlineData("150.000.000", 150000000)]
    [InlineData("150,000,000", 150000000)]
    [InlineData("$50,003,333", 50003333)]
    [InlineData("$99,999,664", 99999664)]
    [InlineData("50.003.333", 50003333)]
    [InlineData("99.999.664", 99999664)]
    [InlineData("$ 50,003,333 COP", 50003333)]
    [InlineData("$ 99,999,664 COP", 99999664)]
    [InlineData("50.000,50", 50000.50)]
    [InlineData("50,000.50", 50000.50)]
    public void IntentarParsearDecimalPositivoConDosDecimales_InterpretaPuntosYComasCorrectamente(string input, decimal expected)
    {
        var ok = Asoagro.Components.Pages.RegistrarAsociado.IntentarParsearDecimalPositivoConDosDecimales(input, out var valor);
        Assert.True(ok, $"Fallo al parsear '{input}'");
        Assert.Equal(expected, valor);
    }
}

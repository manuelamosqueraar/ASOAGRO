using Asoagro.Models;

namespace Asoagro.Services
{
    public class OpcionCatalogoItem
    {
        public string Codigo { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
        public string Subtitulo { get; init; } = string.Empty;
        public string DocumentoSugerido { get; init; } = string.Empty;
    }

    public class AsociadoModel
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string TipoDocumentoIdentidad { get; set; } = "CC";
        public string Cedula { get; set; } = string.Empty;
        public string AsociacionPertenece { get; set; } = string.Empty;
        public List<Asoagro.Models.AfiliacionAsociado> Afiliaciones { get; set; } = new();
        public bool AutorizacionDatosPersonales { get; set; } = true;
        public DateTime? FechaAutorizacionHabeasData { get; set; } = DateTime.Now;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string NombrePredio { get; set; } = string.Empty;
        public string VeredaSector { get; set; } = string.Empty;
        public string Municipio { get; set; } = string.Empty;
        public string TenenciaTerreno { get; set; } = string.Empty;
        public double HectareasPredio { get; set; }
        public double HectareasLineaProductiva { get; set; }
        public string TipoActividadProductiva { get; set; } = string.Empty;
        public string ProduccionAproximada { get; set; } = string.Empty;
        public string CultivoPrincipal { get; set; } = string.Empty;
        public List<string> CultivosSeleccionados { get; set; } = new();
        public bool TieneBPA { get; set; }
        public string CertificadoBpaNombre { get; set; } = string.Empty;
        public string CertificadoBpaTamano { get; set; } = string.Empty;
        public string CertificadoBpaUrl { get; set; } = string.Empty;
        public string EsVictima { get; set; } = "No";
        public List<string> GruposVulnerables { get; set; } = new();
        public Dictionary<string, string> SoportesGruposVulnerables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> SoportesGruposUrls { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string EstadoMembresia { get; set; } = "Activo";
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public List<CuotaAporteAsociado> CuotasAportes { get; set; } = new()
        {
            new CuotaAporteAsociado
            {
                TipoCuota = "Afiliación",
                Periodicidad = "Pago único",
                ValorCuota = 50000m,
                FechaVencimiento = DateTime.Today.AddDays(30),
                CantidadCuotasPendientes = 0,
                ValorTotalPendiente = 0m,
                ValorTotalPagado = 50000m,
                EstadoPago = "Pagado · Al día"
            }
        };

        public int TotalCuotasPendientes => CuotasAportes.Sum(c => Math.Max(0, c.CantidadCuotasPendientes));
        public decimal ValorTotalPendienteAcumulado => CuotasAportes.Sum(c => Math.Max(0m, c.ValorTotalPendiente));
        public decimal ValorTotalPagadoAcumulado => CuotasAportes.Sum(c => Math.Max(0m, c.ValorTotalPagado));
        public int TotalSoportesCuotas => CuotasAportes.Sum(c => c.Soportes.Count);

        public string EstadoGeneralCuotas
        {
            get
            {
                if (CuotasAportes.Count == 0) return "Sin registrar";
                if (CuotasAportes.Any(c => c.CalcularEstadoAutomatico().Contains("Vencido", StringComparison.OrdinalIgnoreCase)))
                    return "Vencido · En mora";
                if (ValorTotalPendienteAcumulado > 0 && ValorTotalPagadoAcumulado > 0)
                    return "Pago parcial";
                if (ValorTotalPendienteAcumulado > 0 || TotalCuotasPendientes > 0)
                    return "Pendiente de pago";
                if (ValorTotalPagadoAcumulado > 0)
                    return "Pagado · Al día";
                return "Por liquidar";
            }
        }

        // Identificación ampliada (registro con persona natural o jurídica)
        public string TipoPersona { get; set; } = "Natural";
        public string MunicipioCodigo { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
        public string CedulaArchivoNombre { get; set; } = string.Empty;
        public string CedulaArchivoTamano { get; set; } = string.Empty;
        public string CedulaArchivoUrl { get; set; } = string.Empty;
        public string RutArchivoNombre { get; set; } = string.Empty;
        public string RutArchivoTamano { get; set; } = string.Empty;
        public string RutArchivoUrl { get; set; } = string.Empty;

        public bool EsJuridica => string.Equals(TipoPersona, "Juridica", StringComparison.OrdinalIgnoreCase);

        public static readonly IReadOnlyList<string> OpcionesTenenciaTerreno = new List<string>
        {
            "Propio",
            "Arrendado",
            "Prestado"
        };

        public static readonly IReadOnlyList<string> OpcionesTipoActividad = new List<string>
        {
            "Agrícola",
            "Pecuaria",
            "Mixta"
        };

        public static readonly IReadOnlyList<string> OpcionesTipoCuota = new List<string>
        {
            "Afiliación",
            "Sostenimiento",
            "Ordinaria",
            "Extraordinaria"
        };

        public static readonly IReadOnlyList<string> OpcionesPeriodicidadCuota = new List<string>
        {
            "Mensual",
            "Bimensual",
            "Anual",
            "Pago único"
        };

        public static readonly IReadOnlyList<OpcionCatalogoItem> CatalogoCultivos = new List<OpcionCatalogoItem>
        {
            new() { Codigo = "Platano", Nombre = "Plátano", Subtitulo = "Hartón / Exportación" },
            new() { Codigo = "Banano", Nombre = "Banano", Subtitulo = "Tipo Exportación / Criollo" },
            new() { Codigo = "Cacao", Nombre = "Cacao", Subtitulo = "Fino de Aroma" },
            new() { Codigo = "Ganaderia", Nombre = "Ganadería", Subtitulo = "Doble Propósito / Leche" },
            new() { Codigo = "Aguacate", Nombre = "Aguacate", Subtitulo = "Hass / Papelillo" },
            new() { Codigo = "Maracuya", Nombre = "Maracuyá", Subtitulo = "Frutales Comerciales" },
            new() { Codigo = "Yuca", Nombre = "Yuca", Subtitulo = "Industrial / Consumo" },
            new() { Codigo = "Maiz", Nombre = "Maíz", Subtitulo = "Tecnificado / Tradicional" },
            new() { Codigo = "Citricos", Nombre = "Cítricos", Subtitulo = "Limón Tahití / Naranja" },
            new() { Codigo = "Cafe", Nombre = "Café", Subtitulo = "Especial / Tradicional" }
        };

        public static readonly IReadOnlyList<OpcionCatalogoItem> CatalogoGruposVulnerables = new List<OpcionCatalogoItem>
        {
            new()
            {
                Codigo = "VictimaConflicto",
                Nombre = "Víctima del conflicto armado",
                Subtitulo = "Inscrito en el Registro Único de Víctimas (RUV)",
                DocumentoSugerido = "Certificado RUV / Unidad para las Víctimas"
            },
            new()
            {
                Codigo = "DesplazamientoForzado",
                Nombre = "Desplazamiento forzado",
                Subtitulo = "Población en situación de desplazamiento o retorno rural",
                DocumentoSugerido = "Constancia RUV / Declaración de desplazamiento"
            },
            new()
            {
                Codigo = "MujerCabezaHogar",
                Nombre = "Mujer cabeza de hogar",
                Subtitulo = "Liderazgo femenino y jefatura económica familiar",
                DocumentoSugerido = "Declaración juramentada / Certificación municipal"
            },
            new()
            {
                Codigo = "Discapacidad",
                Nombre = "Persona en condición de discapacidad",
                Subtitulo = "Productor o núcleo familiar con certificado de discapacidad",
                DocumentoSugerido = "Certificado médico / Registro RLCPD"
            },
            new()
            {
                Codigo = "AdultoMayor",
                Nombre = "Adulto mayor rural",
                Subtitulo = "Productor mayor de 60 años activo en el predio",
                DocumentoSugerido = "Copia de documento de identidad / Certificación"
            },
            new()
            {
                Codigo = "JovenRural",
                Nombre = "Joven rural (18 a 28 años)",
                Subtitulo = "Relevo generacional agropecuario",
                DocumentoSugerido = "Documento de identidad / Certificación juvenil"
            },
            new()
            {
                Codigo = "ComunidadEtnica",
                Nombre = "Comunidad étnica (Afrodescendiente / Indígena)",
                Subtitulo = "Pertenencia a consejo comunitario, cabildo o resguardo",
                DocumentoSugerido = "Aval de Consejo Comunitario / Cabildo / MinInterior"
            },
            new()
            {
                Codigo = "FirmantePaz",
                Nombre = "Proceso de reincorporación / Firmante de paz",
                Subtitulo = "Vinculado a programas productivos de construcción de paz",
                DocumentoSugerido = "Certificación ARN / CODA"
            }
        };

        public static string FormatearNombreCultivo(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return "Sin definir";
            }

            var partes = DesglosarCultivos(codigo);
            if (partes.Count == 0)
            {
                return "Sin definir";
            }

            return string.Join(", ", partes.Select(TraducirCodigoCultivo));
        }

        public static string TraducirCodigoCultivo(string codigo)
        {
            var limpio = codigo.Trim();
            var encontrado = CatalogoCultivos.FirstOrDefault(
                c => string.Equals(c.Codigo, limpio, StringComparison.OrdinalIgnoreCase)
                  || string.Equals(c.Nombre, limpio, StringComparison.OrdinalIgnoreCase));

            return encontrado?.Nombre ?? limpio;
        }

        public static List<string> DesglosarCultivos(string? cultivoPrincipal)
        {
            if (string.IsNullOrWhiteSpace(cultivoPrincipal))
            {
                return new List<string>();
            }

            return cultivoPrincipal
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizarCodigoCultivo)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string NormalizarCodigoCultivo(string valor)
        {
            var limpio = valor.Trim();
            var encontrado = CatalogoCultivos.FirstOrDefault(
                c => string.Equals(c.Codigo, limpio, StringComparison.OrdinalIgnoreCase)
                  || string.Equals(c.Nombre, limpio, StringComparison.OrdinalIgnoreCase));

            return encontrado?.Codigo ?? limpio;
        }

        public static string TraducirGrupoVulnerable(string codigo)
        {
            var limpio = codigo.Trim();
            var encontrado = CatalogoGruposVulnerables.FirstOrDefault(
                g => string.Equals(g.Codigo, limpio, StringComparison.OrdinalIgnoreCase)
                  || string.Equals(g.Nombre, limpio, StringComparison.OrdinalIgnoreCase));

            return encontrado?.Nombre ?? limpio;
        }

        public void SincronizarCultivosDesdeLista()
        {
            CultivosSeleccionados = CultivosSeleccionados
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(NormalizarCodigoCultivo)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            CultivoPrincipal = string.Join(", ", CultivosSeleccionados);
        }

        public void SincronizarListaDesdeCultivoPrincipal()
        {
            if (CultivosSeleccionados.Count == 0 && !string.IsNullOrWhiteSpace(CultivoPrincipal))
            {
                CultivosSeleccionados = DesglosarCultivos(CultivoPrincipal);
            }
            else if (CultivosSeleccionados.Count > 0)
            {
                SincronizarCultivosDesdeLista();
            }
        }
    }
}

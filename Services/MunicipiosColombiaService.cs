using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Asoagro.Services
{
    public sealed record MunicipioColombia(string Codigo, string Nombre, string Departamento, double? Latitud, double? Longitud)
    {
        public bool EsUraba => MunicipiosColombiaService.CodigosUraba.Contains(Codigo);

        public string Etiqueta => $"{Nombre}, {Departamento}";

        /// <summary>Texto que se guarda en el expediente: solo el nombre para Antioquia, con departamento para el resto.</summary>
        public string NombreParaExpediente =>
            string.Equals(Departamento, "Antioquia", StringComparison.OrdinalIgnoreCase)
                ? Nombre
                : $"{Nombre} ({Departamento})";
    }

    /// <summary>
    /// Catálogo de los 1.122 municipios de Colombia (DIVIPOLA - DANE, datos.gov.co) con coordenadas.
    /// Se carga una sola vez desde wwwroot/data/municipios-colombia.json.
    /// </summary>
    public sealed class MunicipiosColombiaService
    {
        public static readonly HashSet<string> CodigosUraba = new(StringComparer.Ordinal)
        {
            "05045", // Apartadó
            "05051", // Arboletes
            "05147", // Carepa
            "05172", // Chigorodó
            "05475", // Murindó
            "05480", // Mutatá
            "05490", // Necoclí
            "05659", // San Juan de Urabá
            "05665", // San Pedro de Urabá
            "05837", // Turbo
            "05873"  // Vigía del Fuerte
        };

        private readonly Lazy<IReadOnlyList<MunicipioColombia>> _todos;
        private readonly Lazy<Dictionary<string, string>> _claves;

        public MunicipiosColombiaService(IWebHostEnvironment entorno, ILogger<MunicipiosColombiaService> log)
        {
            _todos = new Lazy<IReadOnlyList<MunicipioColombia>>(() => Cargar(entorno, log));
            _claves = new Lazy<Dictionary<string, string>>(
                () => Todos.ToDictionary(m => m.Codigo, m => Normalizar($"{m.Nombre} {m.Departamento}")));
        }

        public IReadOnlyList<MunicipioColombia> Todos => _todos.Value;

        public IReadOnlyList<MunicipioColombia> Uraba =>
            Todos.Where(m => m.EsUraba).OrderBy(m => m.Nombre, StringComparer.Create(new CultureInfo("es-CO"), true)).ToList();

        public MunicipioColombia? PorCodigo(string? codigo) =>
            string.IsNullOrWhiteSpace(codigo) ? null : Todos.FirstOrDefault(m => m.Codigo == codigo);

        public IReadOnlyList<MunicipioColombia> Buscar(string? texto, int maximo = 8)
        {
            var consulta = Normalizar(texto ?? string.Empty).Trim();
            if (consulta.Length == 0)
            {
                return Uraba.Take(maximo).ToList();
            }

            var claves = _claves.Value;
            return Todos
                .Where(m => claves[m.Codigo].Contains(consulta, StringComparison.Ordinal))
                .OrderByDescending(m => m.EsUraba)
                .ThenByDescending(m => Normalizar(m.Nombre).StartsWith(consulta, StringComparison.Ordinal))
                .ThenBy(m => m.Nombre, StringComparer.Create(new CultureInfo("es-CO"), true))
                .Take(maximo)
                .ToList();
        }

        public static string Normalizar(string texto)
        {
            var descompuesto = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(descompuesto.Length);
            foreach (var c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }

            return sb.ToString();
        }

        private sealed class MunicipioJson
        {
            public string c { get; set; } = string.Empty;
            public string n { get; set; } = string.Empty;
            public string d { get; set; } = string.Empty;
            public double? a { get; set; }
            public double? o { get; set; }
        }

        private static IReadOnlyList<MunicipioColombia> Cargar(IWebHostEnvironment entorno, ILogger log)
        {
            try
            {
                var raiz = entorno.WebRootPath ?? Path.Combine(entorno.ContentRootPath, "wwwroot");
                var ruta = Path.Combine(raiz, "data", "municipios-colombia.json");
                if (File.Exists(ruta))
                {
                    var json = File.ReadAllText(ruta, Encoding.UTF8);
                    var datos = JsonSerializer.Deserialize<List<MunicipioJson>>(json) ?? new();
                    var lista = datos
                        .Where(x => !string.IsNullOrWhiteSpace(x.c) && !string.IsNullOrWhiteSpace(x.n))
                        .Select(x => new MunicipioColombia(x.c, x.n, x.d, x.a, x.o))
                        .ToList();
                    if (lista.Count > 0)
                    {
                        return lista;
                    }
                }

                log.LogWarning("No se encontró el catálogo de municipios; se usa el listado base de Urabá.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "No se pudo cargar el catálogo de municipios de Colombia.");
            }

            return new List<MunicipioColombia>
            {
                new("05045", "Apartadó", "Antioquia", 7.882968, -76.625279),
                new("05051", "Arboletes", "Antioquia", 8.849317, -76.426708),
                new("05147", "Carepa", "Antioquia", 7.755148, -76.652652),
                new("05172", "Chigorodó", "Antioquia", 7.666147, -76.681531),
                new("05475", "Murindó", "Antioquia", 6.97771, -76.817485),
                new("05480", "Mutatá", "Antioquia", 7.242875, -76.435875),
                new("05490", "Necoclí", "Antioquia", 8.434526, -76.787271),
                new("05659", "San Juan de Urabá", "Antioquia", 8.7597, -76.5283),
                new("05665", "San Pedro de Urabá", "Antioquia", 8.276884, -76.380567),
                new("05837", "Turbo", "Antioquia", 8.089929, -76.728858),
                new("05873", "Vigía del Fuerte", "Antioquia", 6.588164, -76.896004)
            };
        }
    }
}


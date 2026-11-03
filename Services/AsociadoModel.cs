namespace Asoagro.Services
{
    public class AsociadoModel
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string NombrePredio { get; set; } = string.Empty;
        public string VeredaSector { get; set; } = string.Empty;
        public string Municipio { get; set; } = string.Empty;
        public double HectareasPredio { get; set; }
        public string CultivoPrincipal { get; set; } = string.Empty;
        public bool TieneBPA { get; set; }
        public string EsVictima { get; set; } = "No";
        public string EstadoMembresia { get; set; } = "AlDia";
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}

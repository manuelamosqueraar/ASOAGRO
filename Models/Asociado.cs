using System.ComponentModel.DataAnnotations;

namespace Asoagro.Models
{
    public class Asociado
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La cédula es obligatoria.")]
        [StringLength(20)]
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(100)]
        public string NombreCompleto { get; set; } = string.Empty;

        [StringLength(100)]
        public string FincaVereda { get; set; } = string.Empty;

        [StringLength(50)]
        public string CultivoPrincipal { get; set; } = string.Empty;

        public bool TieneBPA { get; set; } = false;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

        [StringLength(80)]
        public string CreadoPor { get; set; } = string.Empty;

        [StringLength(80)]
        public string ActualizadoPor { get; set; } = string.Empty;
    }
}

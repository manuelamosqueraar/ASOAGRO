using System;
using System.Collections.Generic;

namespace Asoagro.Services
{
    public class AsociadoModel
    {
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

    public class AsociadoService
    {
        private readonly List<AsociadoModel> _asociados = new()
        {
            new AsociadoModel { NombreCompleto = "Juan Antonio Pérez", Cedula = "1045123890", Telefono = "3104567890", Correo = "juan.perez@email.com", NombrePredio = "Finca La Esperanza", VeredaSector = "Vereda El Tres", Municipio = "Apartadó", HectareasPredio = 5.0, CultivoPrincipal = "Platano", TieneBPA = true, EsVictima = "No", EstadoMembresia = "AlDia", FechaRegistro = DateTime.Now.AddDays(-3) },
            new AsociadoModel { NombreCompleto = "María Elena Garcés", Cedula = "43567890", Telefono = "3201234567", Correo = "maria.garces@email.com", NombrePredio = "Lote El Danubio", VeredaSector = "Sector Zungo", Municipio = "Carepa", HectareasPredio = 3.5, CultivoPrincipal = "Banano", TieneBPA = false, EsVictima = "Si", EstadoMembresia = "AlDia", FechaRegistro = DateTime.Now.AddDays(-7) }
        };

        public List<AsociadoModel> ObtenerAsociados() => _asociados;

        public void AgregarAsociado(AsociadoModel asociado)
        {
            _asociados.Insert(0, asociado); // Lo coloca de primero en el historial
        }
    }
}

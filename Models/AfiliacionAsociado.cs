using System;
using System.Collections.Generic;

namespace Asoagro.Models;

/// <summary>
/// Representa la vinculación o membresía institucional de un productor con una asociación específica.
/// Permite que un productor tenga un perfil único como persona y una o más afiliaciones asociativas.
/// </summary>
public class AfiliacionAsociado
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int AsociadoId { get; set; }

    public string AsociadoCedula { get; set; } = string.Empty;

    public string AsociacionNombre { get; set; } = string.Empty;

    public DateTime FechaSolicitud { get; set; } = DateTime.Today;

    public DateTime? FechaAprobacion { get; set; }

    public DateTime? FechaRetiro { get; set; }

    /// <summary>
    /// Estados posibles: "Activa", "Pendiente", "Inactiva", "Retirada"
    /// </summary>
    public string Estado { get; set; } = "Activa";

    public string? AprobadoPor { get; set; }

    public string? MotivoRetiro { get; set; }

    public string? Observaciones { get; set; }

    public bool CumpleRequisitosEstatutos { get; set; } = true;

    public List<string> RequisitosCumplidos { get; set; } = new();

    public string? SoportesAnexos { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}


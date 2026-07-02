using System.ComponentModel.DataAnnotations;
using GestionServiciosAutomotrices.API.Models;

namespace GestionServiciosAutomotrices.API.DTOs
{
    /// <summary>
    /// Datos para actualizar un ticket existente.
    /// El endpoint PUT que usa este DTO todavía no está implementado (fase 2).
    /// </summary>
    public class TicketActualizarDto
    {
        public int? IdMecanico { get; set; }

        public EstadoTicket? Estado { get; set; }

        public DateTime? FechaEstimadaEntrega { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        // Pendiente de validación: reglas de transición de estado
        // (por ejemplo, un ticket Cancelado no debería poder pasar a EnProceso).
    }
}

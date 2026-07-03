using System.ComponentModel.DataAnnotations;
using GestionServiciosAutomotrices.API.Models;

namespace GestionServiciosAutomotrices.API.DTOs
{
    /// <summary>
    /// Datos para actualizar un ticket existente (PUT api/tickets/{id}).
    /// Todas las propiedades son opcionales: solo se actualiza lo que se envía.
    /// </summary>
    public class TicketActualizarDto
    {
        public int? IdMecanico { get; set; }

        // Acepta el nombre del estado como texto ("EnProceso", "Terminado", ...).
        // Las transiciones se validan en el controlador: Entregado y Cancelado son finales.
        public EstadoTicket? Estado { get; set; }

        public DateTime? FechaEstimadaEntrega { get; set; }

        [StringLength(500, MinimumLength = 10,
            ErrorMessage = "La descripción debe tener entre 10 y 500 caracteres.")]
        public string? DescripcionProblema { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionServiciosAutomotrices.API.Models
{
    /// <summary>
    /// Ticket (orden de servicio) que se abre cuando un vehículo entra al taller.
    /// </summary>
    public class Ticket
    {
        [Key]
        public int IdTicket { get; set; }

        // Folio legible para el cliente, ej. "TKT-2026-0001".
        // Se genera automáticamente como consecutivo del año al crear el ticket.
        [Required]
        [StringLength(20)]
        public string Folio { get; set; } = string.Empty;

        [Required]
        public int IdVehiculo { get; set; }

        // El mecánico puede asignarse después de crear el ticket, por eso es opcional.
        public int? IdMecanico { get; set; }

        [Required(ErrorMessage = "La descripción del problema es obligatoria.")]
        [StringLength(500)]
        public string DescripcionProblema { get; set; } = string.Empty;

        public EstadoTicket Estado { get; set; } = EstadoTicket.Abierto;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaEstimadaEntrega { get; set; }

        // Se llena automáticamente cuando el ticket pasa al estado Entregado.
        public DateTime? FechaEntrega { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        // Suma de los precios aplicados de los servicios asociados (TicketServicios);
        // se calcula al crear el ticket.
        [Column(TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }

        // Propiedades de navegación
        [ForeignKey(nameof(IdVehiculo))]
        public Vehiculo? Vehiculo { get; set; }

        [ForeignKey(nameof(IdMecanico))]
        public Mecanico? Mecanico { get; set; }

        public ICollection<TicketServicio> TicketServicios { get; set; } = new List<TicketServicio>();
    }
}

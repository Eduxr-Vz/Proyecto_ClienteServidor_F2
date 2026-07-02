using System.ComponentModel.DataAnnotations.Schema;

namespace GestionServiciosAutomotrices.API.Models
{
    /// <summary>
    /// Tabla intermedia entre Ticket y Servicio (relación muchos a muchos).
    /// Se guarda el precio aplicado para que el histórico no cambie si el
    /// precio del catálogo se actualiza después.
    /// </summary>
    public class TicketServicio
    {
        public int IdTicket { get; set; }

        public int IdServicio { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecioAplicado { get; set; }

        // Propiedades de navegación
        public Ticket? Ticket { get; set; }

        public Servicio? Servicio { get; set; }
    }
}

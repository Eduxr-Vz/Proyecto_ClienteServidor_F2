using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionServiciosAutomotrices.API.Models
{
    /// <summary>
    /// Servicio que ofrece el taller (afinación, frenos, cambio de aceite, etc.).
    /// </summary>
    public class Servicio
    {
        [Key]
        public int IdServicio { get; set; }

        [Required(ErrorMessage = "El nombre del servicio es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Range(0, 100000, ErrorMessage = "El precio debe ser mayor o igual a 0.")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Precio { get; set; }

        // Tiempo estimado del servicio en minutos.
        public int? TiempoEstimadoMin { get; set; }

        public bool Activo { get; set; } = true;

        public ICollection<TicketServicio> TicketServicios { get; set; } = new List<TicketServicio>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace GestionServiciosAutomotrices.API.DTOs
{
    /// <summary>
    /// Datos que envía el cliente de la API para abrir un nuevo ticket.
    /// </summary>
    public class TicketCrearDto
    {
        [Required(ErrorMessage = "El vehículo es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El id del vehículo no es válido.")]
        public int IdVehiculo { get; set; }

        [Required(ErrorMessage = "La descripción del problema es obligatoria.")]
        [StringLength(500, MinimumLength = 10,
            ErrorMessage = "La descripción debe tener entre 10 y 500 caracteres.")]
        public string DescripcionProblema { get; set; } = string.Empty;

        // Opcional: el mecánico se puede asignar después.
        public int? IdMecanico { get; set; }

        public DateTime? FechaEstimadaEntrega { get; set; }

        // Ids de los servicios solicitados. Se validan contra el catálogo,
        // se guardan en TicketServicios y con ellos se calcula el total.
        public List<int>? IdsServicios { get; set; }
    }
}

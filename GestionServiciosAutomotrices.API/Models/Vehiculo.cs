using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionServiciosAutomotrices.API.Models
{
    /// <summary>
    /// Vehículo registrado por un cliente.
    /// </summary>
    public class Vehiculo
    {
        [Key]
        public int IdVehiculo { get; set; }

        [Required]
        public int IdCliente { get; set; }

        [Required(ErrorMessage = "La marca es obligatoria.")]
        [StringLength(50)]
        public string Marca { get; set; } = string.Empty;

        [Required(ErrorMessage = "El modelo es obligatorio.")]
        [StringLength(50)]
        public string Modelo { get; set; } = string.Empty;

        [Range(1950, 2027, ErrorMessage = "El año debe estar entre 1950 y 2027.")]
        public int Anio { get; set; }

        [Required(ErrorMessage = "Las placas son obligatorias.")]
        [StringLength(10)]
        public string Placas { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Color { get; set; }

        // Número de serie (VIN). Pendiente de validación de formato (17 caracteres).
        [StringLength(17)]
        public string? NumeroSerie { get; set; }

        // Propiedades de navegación
        [ForeignKey(nameof(IdCliente))]
        public Cliente? Cliente { get; set; }

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}

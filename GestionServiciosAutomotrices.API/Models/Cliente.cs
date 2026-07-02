using System.ComponentModel.DataAnnotations;

namespace GestionServiciosAutomotrices.API.Models
{
    /// <summary>
    /// Representa a un cliente del taller.
    /// </summary>
    public class Cliente
    {
        [Key]
        public int IdCliente { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
        [StringLength(15)]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(150)]
        public string? Correo { get; set; }

        [StringLength(250)]
        public string? Direccion { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Propiedad de navegación: un cliente puede tener varios vehículos.
        public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    }
}

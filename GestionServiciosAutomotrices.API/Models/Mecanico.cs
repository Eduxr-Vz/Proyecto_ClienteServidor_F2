using System.ComponentModel.DataAnnotations;

namespace GestionServiciosAutomotrices.API.Models
{
    /// <summary>
    /// Mecánico que atiende los tickets de servicio.
    /// </summary>
    public class Mecanico
    {
        [Key]
        public int IdMecanico { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Especialidad { get; set; }

        [Phone]
        [StringLength(15)]
        public string? Telefono { get; set; }

        public bool Activo { get; set; } = true;

        // TODO: Agregar horario/turno del mecánico. Se definirá con el taller en la siguiente fase.

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace GestionServiciosAutomotrices.API.DTOs
{
    /// <summary>
    /// Datos que se envían para dar de alta o actualizar un mecánico.
    /// </summary>
    public class MecanicoGuardarDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Los apellidos deben tener entre 2 y 100 caracteres.")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Especialidad")]
        public string? Especialidad { get; set; }

        [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
        [StringLength(15)]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        // Un mecánico inactivo (dado de baja) ya no aparece para asignar tickets.
        [Display(Name = "Activo (disponible para asignar tickets)")]
        public bool Activo { get; set; } = true;
    }

    /// <summary>
    /// Representación de un mecánico que devuelve la API.
    /// </summary>
    public class MecanicoDto
    {
        public int IdMecanico { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Apellidos { get; set; } = string.Empty;

        public string? Especialidad { get; set; }

        public string? Telefono { get; set; }

        public bool Activo { get; set; }

        public int TotalTickets { get; set; }

        // Tickets que todavía no se entregan ni se cancelan.
        public int TicketsAbiertos { get; set; }
    }
}

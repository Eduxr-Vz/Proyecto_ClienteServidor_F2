using System.ComponentModel.DataAnnotations;

namespace GestionServiciosAutomotrices.API.Models
{
    /// <summary>
    /// Usuario que puede iniciar sesión en el sistema.
    ///
    /// La contraseña NUNCA se guarda tal cual: se almacena su hash
    /// (ver ServicioUsuarios). Aunque alguien leyera la tabla, no podría
    /// recuperar las contraseñas.
    /// </summary>
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50, MinimumLength = 3)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>Resultado de aplicar el algoritmo de hash a la contraseña.</summary>
        [Required]
        [StringLength(500)]
        public string ContrasenaHash { get; set; } = string.Empty;

        /// <summary>Administrador, Recepcionista o Mecanico (ver RolUsuario).</summary>
        [Required]
        [StringLength(30)]
        public string Rol { get; set; } = RolUsuario.Recepcionista;

        public bool Activo { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public DateTime? UltimoAcceso { get; set; }
    }
}

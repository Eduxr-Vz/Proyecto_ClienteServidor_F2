using System.ComponentModel.DataAnnotations;

namespace GestionServiciosAutomotrices.API.DTOs
{
    /// <summary>Credenciales que se envían desde el formulario de acceso.</summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "Escribe tu nombre de usuario.")]
        [Display(Name = "Usuario")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe tu contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Contrasena { get; set; } = string.Empty;

        [Display(Name = "Mantener la sesión iniciada")]
        public bool Recordarme { get; set; }
    }

    /// <summary>Datos para dar de alta o editar una cuenta.</summary>
    public class UsuarioGuardarDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 50 caracteres.")]
        [RegularExpression("^[a-zA-Z0-9._-]+$",
            ErrorMessage = "El usuario solo admite letras, números, punto, guion y guion bajo.")]
        [Display(Name = "Nombre de usuario")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona un rol.")]
        [Display(Name = "Rol")]
        public string Rol { get; set; } = Models.RolUsuario.Recepcionista;

        [Display(Name = "Cuenta activa")]
        public bool Activo { get; set; } = true;

        // Al crear es obligatoria; al editar, si se deja vacía no se cambia.
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string? Contrasena { get; set; }
    }

    /// <summary>Representación de un usuario que devuelve la API (sin el hash).</summary>
    public class UsuarioDto
    {
        public int IdUsuario { get; set; }

        public string NombreUsuario { get; set; } = string.Empty;

        public string NombreCompleto { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;

        public bool Activo { get; set; }

        public DateTime FechaRegistro { get; set; }

        public DateTime? UltimoAcceso { get; set; }
    }
}

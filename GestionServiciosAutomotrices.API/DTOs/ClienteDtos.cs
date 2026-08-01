using System.ComponentModel.DataAnnotations;

namespace GestionServiciosAutomotrices.API.DTOs
{
    /// <summary>
    /// Datos que se envían para dar de alta o actualizar un cliente.
    /// Se usa tanto en el formulario MVC como en el POST/PUT de la API.
    /// </summary>
    public class ClienteGuardarDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Los apellidos deben tener entre 2 y 100 caracteres.")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; } = string.Empty;

        [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
        [StringLength(15)]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(150)]
        [Display(Name = "Correo electrónico")]
        public string? Correo { get; set; }

        [StringLength(250)]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }
    }

    /// <summary>
    /// Representación de un cliente que devuelve la API.
    /// </summary>
    public class ClienteDto
    {
        public int IdCliente { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Apellidos { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        public string? Correo { get; set; }

        public string? Direccion { get; set; }

        public DateTime FechaRegistro { get; set; }

        // Cuántos vehículos tiene registrados (dato útil para las listas).
        public int TotalVehiculos { get; set; }
    }
}

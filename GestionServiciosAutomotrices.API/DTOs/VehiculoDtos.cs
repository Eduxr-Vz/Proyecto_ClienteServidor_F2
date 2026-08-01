using System.ComponentModel.DataAnnotations;

namespace GestionServiciosAutomotrices.API.DTOs
{
    /// <summary>
    /// Datos que se envían para dar de alta o actualizar un vehículo.
    /// </summary>
    public class VehiculoGuardarDto
    {
        [Required(ErrorMessage = "El cliente es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un cliente válido.")]
        [Display(Name = "Cliente (dueño)")]
        public int IdCliente { get; set; }

        [Required(ErrorMessage = "La marca es obligatoria.")]
        [StringLength(50)]
        [Display(Name = "Marca")]
        public string Marca { get; set; } = string.Empty;

        [Required(ErrorMessage = "El modelo es obligatorio.")]
        [StringLength(50)]
        [Display(Name = "Modelo")]
        public string Modelo { get; set; } = string.Empty;

        [Range(1950, 2027, ErrorMessage = "El año debe estar entre 1950 y 2027.")]
        [Display(Name = "Año")]
        public int Anio { get; set; } = DateTime.Now.Year;

        [Required(ErrorMessage = "Las placas son obligatorias.")]
        [StringLength(10, MinimumLength = 5, ErrorMessage = "Las placas deben tener entre 5 y 10 caracteres.")]
        [Display(Name = "Placas")]
        public string Placas { get; set; } = string.Empty;

        [StringLength(30)]
        [Display(Name = "Color")]
        public string? Color { get; set; }

        // El VIN (número de serie) tiene exactamente 17 caracteres alfanuméricos,
        // sin las letras I, O ni Q para no confundirlas con 1 y 0.
        [StringLength(17, MinimumLength = 17, ErrorMessage = "El número de serie (VIN) debe tener exactamente 17 caracteres.")]
        [RegularExpression("^[A-HJ-NPR-Z0-9]{17}$",
            ErrorMessage = "El VIN solo admite letras y números, sin las letras I, O ni Q.")]
        [Display(Name = "Número de serie (VIN)")]
        public string? NumeroSerie { get; set; }
    }

    /// <summary>
    /// Representación de un vehículo que devuelve la API.
    /// </summary>
    public class VehiculoDto
    {
        public int IdVehiculo { get; set; }

        public string Marca { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public int Anio { get; set; }

        public string Placas { get; set; } = string.Empty;

        public string? Color { get; set; }

        public string? NumeroSerie { get; set; }

        public int IdCliente { get; set; }

        // Nombre completo del dueño, aplanado para no anidar el objeto Cliente.
        public string Cliente { get; set; } = string.Empty;

        public int TotalTickets { get; set; }
    }
}

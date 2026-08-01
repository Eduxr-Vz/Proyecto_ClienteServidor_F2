using System.ComponentModel.DataAnnotations;

namespace GestionServiciosAutomotrices.API.DTOs
{
    /// <summary>
    /// Datos que se envían para dar de alta o actualizar un servicio del catálogo.
    /// </summary>
    public class ServicioGuardarDto
    {
        [Required(ErrorMessage = "El nombre del servicio es obligatorio.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
        [Display(Name = "Nombre del servicio")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Range(0, 100000, ErrorMessage = "El precio debe estar entre 0 y 100,000.")]
        [Display(Name = "Precio")]
        public decimal Precio { get; set; }

        [Range(1, 10080, ErrorMessage = "El tiempo estimado debe estar entre 1 minuto y una semana.")]
        [Display(Name = "Tiempo estimado (minutos)")]
        public int? TiempoEstimadoMin { get; set; }

        [Display(Name = "Activo (disponible para nuevos tickets)")]
        public bool Activo { get; set; } = true;
    }

    /// <summary>
    /// Representación de un servicio que devuelve la API.
    /// </summary>
    public class ServicioDto
    {
        public int IdServicio { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public decimal Precio { get; set; }

        public int? TiempoEstimadoMin { get; set; }

        public bool Activo { get; set; }

        // Cuántas veces se ha aplicado este servicio en tickets.
        public int VecesAplicado { get; set; }
    }
}

namespace GestionServiciosAutomotrices.API.DTOs
{
    /// <summary>
    /// Representación de un ticket que devuelve la API.
    /// Evita exponer las entidades de EF directamente.
    /// </summary>
    public class TicketDto
    {
        public int IdTicket { get; set; }

        public string Folio { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public string DescripcionProblema { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaEstimadaEntrega { get; set; }

        public decimal Total { get; set; }

        // Datos "aplanados" del vehículo y su dueño para no anidar objetos completos.
        public string Vehiculo { get; set; } = string.Empty;

        public string Placas { get; set; } = string.Empty;

        public string Cliente { get; set; } = string.Empty;

        public string? Mecanico { get; set; }

        // TODO (Fase 2): Incluir la lista de servicios del ticket con sus precios.
    }
}

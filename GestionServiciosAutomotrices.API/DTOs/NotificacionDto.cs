namespace GestionServiciosAutomotrices.API.DTOs
{
    /// <summary>
    /// Mensaje que viaja del servidor al navegador cuando algo cambia en los
    /// tickets. Es el "paquete" que SignalR entrega a los clientes conectados.
    /// </summary>
    public class NotificacionDto
    {
        /// <summary>creado, actualizado, estado o eliminado.</summary>
        public string Tipo { get; set; } = string.Empty;

        public int IdTicket { get; set; }

        public string Folio { get; set; } = string.Empty;

        /// <summary>Texto corto que se muestra como título del aviso.</summary>
        public string Titulo { get; set; } = string.Empty;

        /// <summary>Detalle del aviso (vehículo, cliente, estado nuevo...).</summary>
        public string Mensaje { get; set; } = string.Empty;

        public string? Estado { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        /// <summary>
        /// Tickets que siguen sin entregarse. Viaja en cada notificación para
        /// que el contador del menú se actualice solo.
        /// </summary>
        public int TicketsPendientes { get; set; }
    }
}

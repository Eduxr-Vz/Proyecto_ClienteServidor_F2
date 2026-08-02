using GestionServiciosAutomotrices.API.Models;

namespace GestionServiciosAutomotrices.API.Services
{
    /// <summary>
    /// Contrato para avisar de los cambios del sistema en tiempo real.
    ///
    /// Los controladores dependen de esta interfaz y no de SignalR
    /// directamente. Gracias a eso, en la fase 4 se puede agregar una
    /// implementación que publique los avisos en RabbitMQ sin modificar
    /// ni una línea de los controladores: basta con cambiar el registro
    /// del servicio en Program.cs.
    /// </summary>
    public interface INotificadorEventos
    {
        // ----- Tickets -----

        Task TicketCreadoAsync(Ticket ticket);

        Task TicketActualizadoAsync(Ticket ticket);

        Task EstadoCambiadoAsync(Ticket ticket, EstadoTicket estadoAnterior);

        Task TicketEliminadoAsync(Ticket ticket);

        // ----- Catálogos (clientes, vehículos, mecánicos y servicios) -----

        /// <summary>
        /// Avisa de un alta, cambio o baja en un catálogo.
        /// </summary>
        /// <param name="accion">creado, actualizado o eliminado.</param>
        /// <param name="entidad">Cliente, Vehículo, Mecánico o Servicio.</param>
        /// <param name="nombre">Cómo identificar al registro (nombre, placas...).</param>
        /// <param name="detalle">Información adicional para el cuerpo del aviso.</param>
        /// <param name="url">A dónde lleva el botón "Ver"; null si se eliminó.</param>
        Task CatalogoAsync(string accion, string entidad, string nombre,
                           string? detalle = null, string? url = null);
    }
}

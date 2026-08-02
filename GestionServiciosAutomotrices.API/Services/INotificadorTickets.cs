using GestionServiciosAutomotrices.API.Models;

namespace GestionServiciosAutomotrices.API.Services
{
    /// <summary>
    /// Contrato para avisar de los cambios en los tickets.
    ///
    /// Los controladores dependen de esta interfaz y no de SignalR
    /// directamente. Gracias a eso, en la fase 4 se puede agregar una
    /// implementación que publique los avisos en RabbitMQ sin modificar
    /// ni una línea de los controladores: basta con cambiar el registro
    /// del servicio en Program.cs.
    /// </summary>
    public interface INotificadorTickets
    {
        Task TicketCreadoAsync(Ticket ticket);

        Task TicketActualizadoAsync(Ticket ticket);

        Task EstadoCambiadoAsync(Ticket ticket, EstadoTicket estadoAnterior);

        Task TicketEliminadoAsync(Ticket ticket);
    }
}

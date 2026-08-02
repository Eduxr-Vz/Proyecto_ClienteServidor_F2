using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Hubs;
using GestionServiciosAutomotrices.API.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Services
{
    /// <summary>
    /// Envía las notificaciones a los navegadores conectados usando SignalR.
    ///
    /// IHubContext es la forma de hablarle al hub desde fuera de él (desde un
    /// controlador, por ejemplo); no hace falta que el usuario esté haciendo
    /// nada para poder enviarle un mensaje.
    /// </summary>
    public class NotificadorSignalR : INotificadorTickets
    {
        private readonly IHubContext<NotificacionesHub> _hub;
        private readonly AppDbContext _context;
        private readonly ILogger<NotificadorSignalR> _logger;

        public NotificadorSignalR(
            IHubContext<NotificacionesHub> hub,
            AppDbContext context,
            ILogger<NotificadorSignalR> logger)
        {
            _hub = hub;
            _context = context;
            _logger = logger;
        }

        public Task TicketCreadoAsync(Ticket ticket) =>
            EnviarAsync(new NotificacionDto
            {
                Tipo = "creado",
                IdTicket = ticket.IdTicket,
                Folio = ticket.Folio,
                Titulo = $"Nuevo ticket {ticket.Folio}",
                Mensaje = $"{DescribirVehiculo(ticket)} · {Recortar(ticket.DescripcionProblema, 70)}",
                Estado = ticket.Estado.ToString()
            });

        public Task TicketActualizadoAsync(Ticket ticket) =>
            EnviarAsync(new NotificacionDto
            {
                Tipo = "actualizado",
                IdTicket = ticket.IdTicket,
                Folio = ticket.Folio,
                Titulo = $"Ticket {ticket.Folio} actualizado",
                Mensaje = $"{DescribirVehiculo(ticket)} · Total {ticket.Total:C}",
                Estado = ticket.Estado.ToString()
            });

        public Task EstadoCambiadoAsync(Ticket ticket, EstadoTicket estadoAnterior) =>
            EnviarAsync(new NotificacionDto
            {
                Tipo = "estado",
                IdTicket = ticket.IdTicket,
                Folio = ticket.Folio,
                Titulo = $"Ticket {ticket.Folio}: {estadoAnterior} → {ticket.Estado}",
                Mensaje = DescribirVehiculo(ticket),
                Estado = ticket.Estado.ToString()
            });

        public Task TicketEliminadoAsync(Ticket ticket) =>
            EnviarAsync(new NotificacionDto
            {
                Tipo = "eliminado",
                IdTicket = ticket.IdTicket,
                Folio = ticket.Folio,
                Titulo = $"Se eliminó el ticket {ticket.Folio}",
                Mensaje = DescribirVehiculo(ticket),
                Estado = ticket.Estado.ToString()
            });

        /// <summary>
        /// Manda el aviso a todos los navegadores conectados.
        /// Si algo falla se registra en el log pero NO se lanza la excepción:
        /// una notificación que no se pudo enviar no debe tumbar la operación
        /// principal (el ticket ya se guardó correctamente).
        /// </summary>
        private async Task EnviarAsync(NotificacionDto notificacion)
        {
            try
            {
                notificacion.TicketsPendientes = await ContarPendientesAsync();

                await _hub.Clients
                    .Group(NotificacionesHub.GrupoTaller)
                    .SendAsync(NotificacionesHub.EventoNotificacion, notificacion);

                _logger.LogInformation("Notificación enviada: {Tipo} {Folio}",
                    notificacion.Tipo, notificacion.Folio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar la notificación del ticket {Folio}",
                    notificacion.Folio);
            }
        }

        private Task<int> ContarPendientesAsync() =>
            _context.Tickets.CountAsync(t =>
                t.Estado != EstadoTicket.Entregado &&
                t.Estado != EstadoTicket.Cancelado);

        private static string DescribirVehiculo(Ticket t) =>
            t.Vehiculo != null
                ? $"{t.Vehiculo.Marca} {t.Vehiculo.Modelo} ({t.Vehiculo.Placas})"
                : "Vehículo del taller";

        private static string Recortar(string texto, int largo) =>
            texto.Length <= largo ? texto : texto[..largo].TrimEnd() + "...";
    }
}

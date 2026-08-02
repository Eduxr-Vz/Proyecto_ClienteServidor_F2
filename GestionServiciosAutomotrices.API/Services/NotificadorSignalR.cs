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
    public class NotificadorSignalR : INotificadorEventos
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

        // ----------------------- Tickets -----------------------

        public Task TicketCreadoAsync(Ticket ticket) =>
            EnviarAsync(new NotificacionDto
            {
                Tipo = "creado",
                Entidad = "Ticket",
                Titulo = $"Nuevo ticket {ticket.Folio}",
                Mensaje = $"{DescribirVehiculo(ticket)} · {Recortar(ticket.DescripcionProblema, 70)}",
                Url = $"/Tickets/Details/{ticket.IdTicket}"
            });

        public Task TicketActualizadoAsync(Ticket ticket) =>
            EnviarAsync(new NotificacionDto
            {
                Tipo = "actualizado",
                Entidad = "Ticket",
                Titulo = $"Ticket {ticket.Folio} actualizado",
                Mensaje = $"{DescribirVehiculo(ticket)} · Total {ticket.Total:C}",
                Url = $"/Tickets/Details/{ticket.IdTicket}"
            });

        public Task EstadoCambiadoAsync(Ticket ticket, EstadoTicket estadoAnterior) =>
            EnviarAsync(new NotificacionDto
            {
                Tipo = "estado",
                Entidad = "Ticket",
                Titulo = $"Ticket {ticket.Folio}: {estadoAnterior} → {ticket.Estado}",
                Mensaje = DescribirVehiculo(ticket),
                Url = $"/Tickets/Details/{ticket.IdTicket}"
            });

        public Task TicketEliminadoAsync(Ticket ticket) =>
            EnviarAsync(new NotificacionDto
            {
                Tipo = "eliminado",
                Entidad = "Ticket",
                Titulo = $"Se eliminó el ticket {ticket.Folio}",
                Mensaje = DescribirVehiculo(ticket)
            });

        // ----------------------- Catálogos -----------------------

        public Task CatalogoAsync(string accion, string entidad, string nombre,
                                  string? detalle = null, string? url = null)
        {
            var titulo = accion switch
            {
                "creado" => $"{entidad} nuevo: {nombre}",
                "eliminado" => $"Se eliminó el {entidad.ToLowerInvariant()} {nombre}",
                _ => $"{entidad} actualizado: {nombre}"
            };

            return EnviarAsync(new NotificacionDto
            {
                Tipo = accion,
                Entidad = entidad,
                Titulo = titulo,
                Mensaje = detalle ?? string.Empty,
                Url = url
            });
        }

        // ----------------------- Envío -----------------------

        /// <summary>
        /// Manda el aviso a todos los navegadores conectados.
        /// Si algo falla se registra en el log pero NO se lanza la excepción:
        /// una notificación que no se pudo enviar no debe tumbar la operación
        /// principal (el registro ya se guardó correctamente).
        /// </summary>
        private async Task EnviarAsync(NotificacionDto notificacion)
        {
            try
            {
                notificacion.TicketsPendientes = await ContarPendientesAsync();

                await _hub.Clients
                    .Group(NotificacionesHub.GrupoTaller)
                    .SendAsync(NotificacionesHub.EventoNotificacion, notificacion);

                _logger.LogInformation("Notificación enviada: {Entidad} {Tipo} — {Titulo}",
                    notificacion.Entidad, notificacion.Tipo, notificacion.Titulo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar la notificación: {Titulo}", notificacion.Titulo);
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

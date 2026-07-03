using GestionServiciosAutomotrices.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Data
{
    /// <summary>
    /// Reglas de negocio de tickets compartidas por el controlador de la API
    /// (TicketsApiController) y el controlador MVC de las vistas web (TicketsController).
    /// </summary>
    public static class TicketReglas
    {
        /// <summary>
        /// Genera el folio consecutivo del año en curso, ej. "TKT-2026-0003".
        /// Se toma el número más alto ya registrado (no un conteo, porque al
        /// eliminar tickets el conteo se desfasa y produciría folios duplicados).
        /// </summary>
        public static async Task<string> GenerarFolioAsync(AppDbContext context)
        {
            var prefijo = $"TKT-{DateTime.Now.Year}-";

            var folios = await context.Tickets
                .Where(t => t.Folio.StartsWith(prefijo))
                .Select(t => t.Folio)
                .ToListAsync();

            var maximo = 0;
            foreach (var folio in folios)
            {
                if (int.TryParse(folio.AsSpan(prefijo.Length), out var numero) && numero > maximo)
                {
                    maximo = numero;
                }
            }

            return $"{prefijo}{maximo + 1:D4}";
        }

        /// <summary>
        /// Reglas de transición de estados. Devuelve el mensaje de error
        /// o null si el cambio es válido.
        /// </summary>
        public static string? ValidarCambioDeEstado(Ticket ticket, EstadoTicket nuevoEstado)
        {
            if (!Enum.IsDefined(nuevoEstado))
            {
                return $"El estado {(int)nuevoEstado} no es válido. Valores permitidos: " +
                       string.Join(", ", Enum.GetNames<EstadoTicket>());
            }

            // Entregado y Cancelado son estados finales.
            if (ticket.Estado is EstadoTicket.Entregado or EstadoTicket.Cancelado
                && nuevoEstado != ticket.Estado)
            {
                return $"El ticket {ticket.Folio} está {ticket.Estado} y ya no puede cambiar de estado.";
            }

            return null;
        }

        public static void AplicarCambioDeEstado(Ticket ticket, EstadoTicket nuevoEstado)
        {
            ticket.Estado = nuevoEstado;

            // Al entregar el vehículo se registra la fecha real de entrega.
            if (nuevoEstado == EstadoTicket.Entregado && ticket.FechaEntrega == null)
            {
                ticket.FechaEntrega = DateTime.Now;
            }
        }
    }
}

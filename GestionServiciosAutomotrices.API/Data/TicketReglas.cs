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

        /// <summary>
        /// Deja el ticket con exactamente los servicios indicados y recalcula el total.
        /// Los servicios que ya estaban conservan su PrecioAplicado original; los
        /// nuevos toman el precio vigente del catálogo.
        /// Devuelve el mensaje de error si algún id no existe, o null si todo salió bien.
        /// </summary>
        public static async Task<string?> SincronizarServiciosAsync(
            AppDbContext context, Ticket ticket, List<int> idsServicios)
        {
            var idsSolicitados = idsServicios.Distinct().ToList();

            var servicios = await context.Servicios
                .Where(s => idsSolicitados.Contains(s.IdServicio))
                .ToListAsync();

            var idsNoEncontrados = idsSolicitados
                .Except(servicios.Select(s => s.IdServicio))
                .ToList();

            if (idsNoEncontrados.Count > 0)
            {
                return $"Los servicios con id [{string.Join(", ", idsNoEncontrados)}] no existen.";
            }

            // Un servicio desactivado solo se rechaza si es nuevo en el ticket:
            // los que ya estaban se conservan aunque se hayan dado de baja después.
            var yaEnElTicket = ticket.TicketServicios.Select(ts => ts.IdServicio).ToHashSet();
            var inactivosNuevos = servicios
                .Where(s => !s.Activo && !yaEnElTicket.Contains(s.IdServicio))
                .Select(s => s.Nombre)
                .ToList();

            if (inactivosNuevos.Count > 0)
            {
                return $"Estos servicios están inactivos y no se pueden agregar: {string.Join(", ", inactivosNuevos)}.";
            }

            // Quitar los que ya no vienen en la lista.
            var aQuitar = ticket.TicketServicios
                .Where(ts => !idsSolicitados.Contains(ts.IdServicio))
                .ToList();

            foreach (var ts in aQuitar)
            {
                ticket.TicketServicios.Remove(ts);
                context.TicketServicios.Remove(ts);
            }

            // Agregar los nuevos con el precio vigente.
            foreach (var servicio in servicios.Where(s => !yaEnElTicket.Contains(s.IdServicio)))
            {
                ticket.TicketServicios.Add(new TicketServicio
                {
                    IdTicket = ticket.IdTicket,
                    IdServicio = servicio.IdServicio,
                    PrecioAplicado = servicio.Precio
                });
            }

            ticket.Total = ticket.TicketServicios.Sum(ts => ts.PrecioAplicado);
            return null;
        }

        /// <summary>
        /// Comprueba si el vehículo ya tiene un ticket sin cerrar (ni entregado ni
        /// cancelado). Evita abrir dos órdenes de servicio para el mismo carro.
        /// </summary>
        public static Task<Ticket?> BuscarTicketAbiertoDelVehiculoAsync(
            AppDbContext context, int idVehiculo, int? idTicketExcluir = null)
        {
            return context.Tickets
                .FirstOrDefaultAsync(t =>
                    t.IdVehiculo == idVehiculo &&
                    t.Estado != EstadoTicket.Entregado &&
                    t.Estado != EstadoTicket.Cancelado &&
                    (idTicketExcluir == null || t.IdTicket != idTicketExcluir));
        }
    }
}

using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Controllers
{
    /// <summary>
    /// CRUD completo de tickets (órdenes de servicio) - Fase 2.
    /// Create (POST), Read (GET), Update (PUT/PATCH) y Delete (DELETE).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TicketsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/tickets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketDto>>> GetTickets()
        {
            // TODO (Fase 3): Agregar filtros por estado y por mecánico (?estado=Abierto&idMecanico=2)
            // y paginación cuando la tabla crezca.
            var tickets = await _context.Tickets
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v!.Cliente)
                .Include(t => t.Mecanico)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            return Ok(tickets.Select(MapearADto));
        }

        // GET: api/tickets/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<TicketDto>> GetTicket(int id)
        {
            var ticket = await BuscarTicketConRelaciones(id);

            if (ticket == null)
            {
                return NotFound(new { mensaje = $"No existe un ticket con id {id}." });
            }

            return Ok(MapearADto(ticket));
        }

        // POST: api/tickets
        [HttpPost]
        public async Task<ActionResult<TicketDto>> CrearTicket(TicketCrearDto dto)
        {
            // Las validaciones de los DataAnnotations del DTO las aplica [ApiController]
            // automáticamente (devuelve 400 si el modelo no es válido).

            var vehiculo = await _context.Vehiculos
                .Include(v => v.Cliente)
                .FirstOrDefaultAsync(v => v.IdVehiculo == dto.IdVehiculo);

            if (vehiculo == null)
            {
                return BadRequest(new { mensaje = $"El vehículo con id {dto.IdVehiculo} no está registrado." });
            }

            if (dto.IdMecanico.HasValue)
            {
                var existeMecanico = await _context.Mecanicos
                    .AnyAsync(m => m.IdMecanico == dto.IdMecanico.Value && m.Activo);

                if (!existeMecanico)
                {
                    return BadRequest(new { mensaje = $"El mecánico con id {dto.IdMecanico} no existe o no está activo." });
                }
            }

            var ticket = new Ticket
            {
                IdVehiculo = dto.IdVehiculo,
                IdMecanico = dto.IdMecanico,
                DescripcionProblema = dto.DescripcionProblema,
                FechaEstimadaEntrega = dto.FechaEstimadaEntrega,
                Estado = EstadoTicket.Abierto,
                FechaCreacion = DateTime.Now,
                Folio = await GenerarFolioAsync()
            };

            // Se asocian los servicios solicitados y se calcula el total con el
            // precio vigente de cada servicio (queda "congelado" en PrecioAplicado).
            if (dto.IdsServicios is { Count: > 0 })
            {
                var idsSolicitados = dto.IdsServicios.Distinct().ToList();
                var servicios = await _context.Servicios
                    .Where(s => idsSolicitados.Contains(s.IdServicio) && s.Activo)
                    .ToListAsync();

                var idsNoEncontrados = idsSolicitados
                    .Except(servicios.Select(s => s.IdServicio))
                    .ToList();

                if (idsNoEncontrados.Count > 0)
                {
                    return BadRequest(new
                    {
                        mensaje = $"Los servicios con id [{string.Join(", ", idsNoEncontrados)}] no existen o no están activos."
                    });
                }

                foreach (var servicio in servicios)
                {
                    ticket.TicketServicios.Add(new TicketServicio
                    {
                        IdServicio = servicio.IdServicio,
                        PrecioAplicado = servicio.Precio
                    });
                }

                ticket.Total = servicios.Sum(s => s.Precio);
            }

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Se recarga el mecánico para poder devolver su nombre en la respuesta.
            if (ticket.IdMecanico.HasValue)
            {
                await _context.Entry(ticket).Reference(t => t.Mecanico).LoadAsync();
            }
            ticket.Vehiculo = vehiculo;

            return CreatedAtAction(nameof(GetTicket), new { id = ticket.IdTicket }, MapearADto(ticket));
        }

        // PUT: api/tickets/5
        [HttpPut("{id:int}")]
        public async Task<ActionResult<TicketDto>> ActualizarTicket(int id, TicketActualizarDto dto)
        {
            var ticket = await BuscarTicketConRelaciones(id);

            if (ticket == null)
            {
                return NotFound(new { mensaje = $"No existe un ticket con id {id}." });
            }

            if (dto.IdMecanico.HasValue)
            {
                var existeMecanico = await _context.Mecanicos
                    .AnyAsync(m => m.IdMecanico == dto.IdMecanico.Value && m.Activo);

                if (!existeMecanico)
                {
                    return BadRequest(new { mensaje = $"El mecánico con id {dto.IdMecanico} no existe o no está activo." });
                }

                ticket.IdMecanico = dto.IdMecanico;
            }

            if (dto.Estado.HasValue)
            {
                var error = ValidarCambioDeEstado(ticket, dto.Estado.Value);
                if (error != null)
                {
                    return BadRequest(new { mensaje = error });
                }

                AplicarCambioDeEstado(ticket, dto.Estado.Value);
            }

            if (dto.DescripcionProblema != null)
            {
                ticket.DescripcionProblema = dto.DescripcionProblema;
            }

            if (dto.FechaEstimadaEntrega.HasValue)
            {
                ticket.FechaEstimadaEntrega = dto.FechaEstimadaEntrega;
            }

            if (dto.Observaciones != null)
            {
                ticket.Observaciones = dto.Observaciones;
            }

            await _context.SaveChangesAsync();

            // Se recarga el mecánico por si se reasignó.
            await _context.Entry(ticket).Reference(t => t.Mecanico).LoadAsync();

            return Ok(MapearADto(ticket));
        }

        // PATCH: api/tickets/5/estado
        // Permite cambiar solo el estado sin mandar todo el ticket.
        // El cuerpo es el nuevo estado como texto JSON, ej: "Terminado"
        [HttpPatch("{id:int}/estado")]
        public async Task<ActionResult<TicketDto>> CambiarEstado(int id, [FromBody] EstadoTicket nuevoEstado)
        {
            var ticket = await BuscarTicketConRelaciones(id);

            if (ticket == null)
            {
                return NotFound(new { mensaje = $"No existe un ticket con id {id}." });
            }

            var error = ValidarCambioDeEstado(ticket, nuevoEstado);
            if (error != null)
            {
                return BadRequest(new { mensaje = error });
            }

            AplicarCambioDeEstado(ticket, nuevoEstado);
            await _context.SaveChangesAsync();

            return Ok(MapearADto(ticket));
        }

        // DELETE: api/tickets/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> EliminarTicket(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.TicketServicios)
                .FirstOrDefaultAsync(t => t.IdTicket == id);

            if (ticket == null)
            {
                return NotFound(new { mensaje = $"No existe un ticket con id {id}." });
            }

            // Regla de negocio: un ticket entregado forma parte del historial
            // del taller y no puede eliminarse.
            if (ticket.Estado == EstadoTicket.Entregado)
            {
                return BadRequest(new { mensaje = "Un ticket entregado no puede eliminarse; forma parte del historial." });
            }

            // Se eliminan primero los servicios asociados porque la llave foránea
            // en la base de datos no tiene borrado en cascada.
            _context.TicketServicios.RemoveRange(ticket.TicketServicios);
            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ----------------- Métodos de apoyo -----------------

        private Task<Ticket?> BuscarTicketConRelaciones(int id)
        {
            return _context.Tickets
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v!.Cliente)
                .Include(t => t.Mecanico)
                .FirstOrDefaultAsync(t => t.IdTicket == id);
        }

        /// <summary>
        /// Genera el folio consecutivo del año en curso, ej. "TKT-2026-0003".
        /// </summary>
        private async Task<string> GenerarFolioAsync()
        {
            var anio = DateTime.Now.Year;
            var ticketsDelAnio = await _context.Tickets
                .CountAsync(t => t.FechaCreacion.Year == anio);

            return $"TKT-{anio}-{ticketsDelAnio + 1:D4}";
        }

        /// <summary>
        /// Reglas de transición de estados. Devuelve el mensaje de error
        /// o null si el cambio es válido.
        /// </summary>
        private static string? ValidarCambioDeEstado(Ticket ticket, EstadoTicket nuevoEstado)
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

        private static void AplicarCambioDeEstado(Ticket ticket, EstadoTicket nuevoEstado)
        {
            ticket.Estado = nuevoEstado;

            // Al entregar el vehículo se registra la fecha real de entrega.
            if (nuevoEstado == EstadoTicket.Entregado && ticket.FechaEntrega == null)
            {
                ticket.FechaEntrega = DateTime.Now;
            }
        }

        /// <summary>
        /// Convierte la entidad Ticket al DTO de respuesta.
        /// TODO (Fase 3): Evaluar usar AutoMapper en lugar de mapeo manual.
        /// </summary>
        private static TicketDto MapearADto(Ticket t)
        {
            return new TicketDto
            {
                IdTicket = t.IdTicket,
                Folio = t.Folio,
                Estado = t.Estado.ToString(),
                DescripcionProblema = t.DescripcionProblema,
                FechaCreacion = t.FechaCreacion,
                FechaEstimadaEntrega = t.FechaEstimadaEntrega,
                Total = t.Total,
                Vehiculo = t.Vehiculo != null
                    ? $"{t.Vehiculo.Marca} {t.Vehiculo.Modelo} {t.Vehiculo.Anio}"
                    : string.Empty,
                Placas = t.Vehiculo?.Placas ?? string.Empty,
                Cliente = t.Vehiculo?.Cliente != null
                    ? $"{t.Vehiculo.Cliente.Nombre} {t.Vehiculo.Cliente.Apellidos}"
                    : string.Empty,
                Mecanico = t.Mecanico != null
                    ? $"{t.Mecanico.Nombre} {t.Mecanico.Apellidos}"
                    : null
            };
        }
    }
}

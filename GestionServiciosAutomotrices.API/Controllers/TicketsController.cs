using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Controllers
{
    /// <summary>
    /// CRUD de tickets (órdenes de servicio).
    /// AVANCE FASE 1: GET y POST funcionando de forma básica.
    /// PUT, DELETE y cambio de estado se implementarán en la fase 2.
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
            // TODO: Agregar filtros por estado y por mecánico (?estado=Abierto&idMecanico=2).
            // TODO: Agregar paginación cuando la tabla crezca.
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
            var ticket = await _context.Tickets
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v!.Cliente)
                .Include(t => t.Mecanico)
                .FirstOrDefaultAsync(t => t.IdTicket == id);

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

            // Pendiente de validación: verificar que el vehículo no tenga ya un ticket abierto.

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

                // TODO: Implementar generación de folio consecutivo (TKT-2026-0001).
                // Por ahora se usa un folio provisional basado en la hora para evitar duplicados.
                Folio = $"TKT-{DateTime.Now:yyyyMMddHHmmss}",

                // TODO: Calcular el total sumando los servicios de dto.IdsServicios.
                // Los servicios solicitados aún NO se guardan en TicketServicios (fase 2).
                Total = 0
            };

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
        public IActionResult ActualizarTicket(int id, TicketActualizarDto dto)
        {
            // TODO: Implementar lógica.
            // Se desarrollará en la siguiente fase:
            //  - Validar que el ticket exista.
            //  - Validar transiciones de estado permitidas (ej. Cancelado no puede volver a EnProceso).
            //  - Registrar la fecha de entrega cuando el estado cambie a Entregado.
            return StatusCode(StatusCodes.Status501NotImplemented,
                new { mensaje = "Endpoint pendiente de implementar en la fase 2." });
        }

        // PATCH: api/tickets/5/estado
        [HttpPatch("{id:int}/estado")]
        public IActionResult CambiarEstado(int id, [FromBody] EstadoTicket nuevoEstado)
        {
            // TODO: Implementar lógica.
            // Este endpoint permitirá cambiar solo el estado sin mandar todo el ticket.
            return StatusCode(StatusCodes.Status501NotImplemented,
                new { mensaje = "Endpoint pendiente de implementar en la fase 2." });
        }

        // DELETE: api/tickets/5
        [HttpDelete("{id:int}")]
        public IActionResult EliminarTicket(int id)
        {
            // TODO: Implementar lógica.
            // Pendiente de decidir con el equipo: borrado físico vs borrado lógico
            // (probablemente solo se permita cancelar tickets, no eliminarlos).
            return StatusCode(StatusCodes.Status501NotImplemented,
                new { mensaje = "Endpoint pendiente de implementar en la fase 2." });
        }

        /// <summary>
        /// Convierte la entidad Ticket al DTO de respuesta.
        /// TODO (Fase 2): Evaluar usar AutoMapper en lugar de mapeo manual.
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

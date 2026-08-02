using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Models;
using GestionServiciosAutomotrices.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Controllers.Api
{
    /// <summary>
    /// API REST: CRUD completo de tickets (órdenes de servicio).
    /// Las vistas web equivalentes están en el controlador MVC TicketsController.
    /// </summary>
    [ApiController]
    [Route("api/tickets")]
    public class TicketsApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificadorTickets _notificador;

        public TicketsApiController(AppDbContext context, INotificadorTickets notificador)
        {
            _context = context;
            _notificador = notificador;
        }

        // GET: api/tickets
        // Filtros opcionales: ?estado=Abierto&idMecanico=2&pagina=1&porPagina=20
        [HttpGet]
        public async Task<ActionResult<object>> GetTickets(
            [FromQuery] EstadoTicket? estado,
            [FromQuery] int? idMecanico,
            [FromQuery] int pagina = 1,
            [FromQuery] int porPagina = 20)
        {
            if (pagina < 1) pagina = 1;
            porPagina = Math.Clamp(porPagina, 1, 100);

            var consulta = _context.Tickets
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v!.Cliente)
                .Include(t => t.Mecanico)
                .AsQueryable();

            if (estado.HasValue)
            {
                consulta = consulta.Where(t => t.Estado == estado.Value);
            }

            if (idMecanico.HasValue)
            {
                consulta = consulta.Where(t => t.IdMecanico == idMecanico.Value);
            }

            var totalRegistros = await consulta.CountAsync();

            var tickets = await consulta
                .OrderByDescending(t => t.FechaCreacion)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            // La respuesta incluye los datos de paginación además de los tickets.
            return Ok(new
            {
                pagina,
                porPagina,
                totalRegistros,
                totalPaginas = (int)Math.Ceiling(totalRegistros / (double)porPagina),
                datos = tickets.Select(MapearADto)
            });
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

            // Un vehículo no puede tener dos órdenes de servicio abiertas a la vez.
            var ticketAbierto = await TicketReglas.BuscarTicketAbiertoDelVehiculoAsync(_context, dto.IdVehiculo);
            if (ticketAbierto != null)
            {
                return BadRequest(new
                {
                    mensaje = $"El vehículo ya tiene el ticket {ticketAbierto.Folio} sin cerrar (estado {ticketAbierto.Estado})."
                });
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
                Folio = await TicketReglas.GenerarFolioAsync(_context)
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

            await _notificador.TicketCreadoAsync(ticket);

            return CreatedAtAction(nameof(GetTicket), new { id = ticket.IdTicket }, MapearADto(ticket));
        }

        // PUT: api/tickets/5
        [HttpPut("{id:int}")]
        public async Task<ActionResult<TicketDto>> ActualizarTicket(int id, TicketActualizarDto dto)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v!.Cliente)
                .Include(t => t.Mecanico)
                .Include(t => t.TicketServicios)
                .FirstOrDefaultAsync(t => t.IdTicket == id);

            if (ticket == null)
            {
                return NotFound(new { mensaje = $"No existe un ticket con id {id}." });
            }

            // Estado previo, para saber si hubo cambio de etapa al notificar.
            var estadoAnterior = ticket.Estado;

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
                var error = TicketReglas.ValidarCambioDeEstado(ticket, dto.Estado.Value);
                if (error != null)
                {
                    return BadRequest(new { mensaje = error });
                }

                TicketReglas.AplicarCambioDeEstado(ticket, dto.Estado.Value);
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

            // Si el cliente mandó la lista de servicios, se reemplaza el detalle
            // completo del ticket y se recalcula el total.
            if (dto.IdsServicios != null)
            {
                var errorServicios = await TicketReglas.SincronizarServiciosAsync(_context, ticket, dto.IdsServicios);
                if (errorServicios != null)
                {
                    return BadRequest(new { mensaje = errorServicios });
                }
            }

            await _context.SaveChangesAsync();

            // Se recarga el mecánico por si se reasignó.
            await _context.Entry(ticket).Reference(t => t.Mecanico).LoadAsync();

            if (estadoAnterior != ticket.Estado)
            {
                await _notificador.EstadoCambiadoAsync(ticket, estadoAnterior);
            }
            else
            {
                await _notificador.TicketActualizadoAsync(ticket);
            }

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

            var error = TicketReglas.ValidarCambioDeEstado(ticket, nuevoEstado);
            if (error != null)
            {
                return BadRequest(new { mensaje = error });
            }

            var estadoAnterior = ticket.Estado;
            TicketReglas.AplicarCambioDeEstado(ticket, nuevoEstado);
            await _context.SaveChangesAsync();

            await _notificador.EstadoCambiadoAsync(ticket, estadoAnterior);

            return Ok(MapearADto(ticket));
        }

        // DELETE: api/tickets/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> EliminarTicket(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Vehiculo)
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

            await _notificador.TicketEliminadoAsync(ticket);

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

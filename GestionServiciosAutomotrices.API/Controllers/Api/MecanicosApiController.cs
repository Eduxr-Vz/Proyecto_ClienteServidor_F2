using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Models;
using GestionServiciosAutomotrices.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Controllers.Api
{
    /// <summary>
    /// API REST: CRUD de mecánicos.
    /// </summary>
    [ApiController]
    [Route("api/mecanicos")]
    public class MecanicosApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificadorEventos _notificador;

        public MecanicosApiController(AppDbContext context, INotificadorEventos notificador)
        {
            _context = context;
            _notificador = notificador;
        }

        // GET: api/mecanicos
        // Opcionalmente solo los activos: api/mecanicos?soloActivos=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MecanicoDto>>> GetMecanicos([FromQuery] bool soloActivos = false)
        {
            var consulta = _context.Mecanicos
                .Include(m => m.Tickets)
                .AsQueryable();

            if (soloActivos)
            {
                consulta = consulta.Where(m => m.Activo);
            }

            var mecanicos = await consulta.OrderBy(m => m.Nombre).ToListAsync();

            return Ok(mecanicos.Select(MapearADto));
        }

        // GET: api/mecanicos/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<MecanicoDto>> GetMecanico(int id)
        {
            var mecanico = await _context.Mecanicos
                .Include(m => m.Tickets)
                .FirstOrDefaultAsync(m => m.IdMecanico == id);

            if (mecanico == null)
            {
                return NotFound(new { mensaje = $"No existe un mecánico con id {id}." });
            }

            return Ok(MapearADto(mecanico));
        }

        // POST: api/mecanicos
        [HttpPost]
        public async Task<ActionResult<MecanicoDto>> CrearMecanico(MecanicoGuardarDto dto)
        {
            var mecanico = new Mecanico
            {
                Nombre = dto.Nombre,
                Apellidos = dto.Apellidos,
                Especialidad = dto.Especialidad,
                Telefono = dto.Telefono,
                Activo = dto.Activo
            };

            _context.Mecanicos.Add(mecanico);
            await _context.SaveChangesAsync();

            await _notificador.CatalogoAsync("creado", "Mecánico",
                $"{mecanico.Nombre} {mecanico.Apellidos}", mecanico.Especialidad, $"/Mecanicos/Details/{mecanico.IdMecanico}");

            return CreatedAtAction(nameof(GetMecanico), new { id = mecanico.IdMecanico }, MapearADto(mecanico));
        }

        // PUT: api/mecanicos/5
        [HttpPut("{id:int}")]
        public async Task<ActionResult<MecanicoDto>> ActualizarMecanico(int id, MecanicoGuardarDto dto)
        {
            var mecanico = await _context.Mecanicos
                .Include(m => m.Tickets)
                .FirstOrDefaultAsync(m => m.IdMecanico == id);

            if (mecanico == null)
            {
                return NotFound(new { mensaje = $"No existe un mecánico con id {id}." });
            }

            // Regla de negocio: no se puede desactivar a un mecánico que tiene
            // trabajo pendiente; primero hay que cerrar o reasignar sus tickets.
            if (!dto.Activo && mecanico.Activo)
            {
                var ticketsAbiertos = mecanico.Tickets.Count(t =>
                    t.Estado != EstadoTicket.Entregado && t.Estado != EstadoTicket.Cancelado);

                if (ticketsAbiertos > 0)
                {
                    return BadRequest(new
                    {
                        mensaje = $"El mecánico tiene {ticketsAbiertos} ticket(s) sin entregar. Reasígnalos antes de darlo de baja."
                    });
                }
            }

            mecanico.Nombre = dto.Nombre;
            mecanico.Apellidos = dto.Apellidos;
            mecanico.Especialidad = dto.Especialidad;
            mecanico.Telefono = dto.Telefono;
            mecanico.Activo = dto.Activo;

            await _context.SaveChangesAsync();

            await _notificador.CatalogoAsync("actualizado", "Mecánico",
                $"{mecanico.Nombre} {mecanico.Apellidos}", mecanico.Especialidad, $"/Mecanicos/Details/{mecanico.IdMecanico}");

            return Ok(MapearADto(mecanico));
        }

        // DELETE: api/mecanicos/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> EliminarMecanico(int id)
        {
            var mecanico = await _context.Mecanicos
                .Include(m => m.Tickets)
                .FirstOrDefaultAsync(m => m.IdMecanico == id);

            if (mecanico == null)
            {
                return NotFound(new { mensaje = $"No existe un mecánico con id {id}." });
            }

            // Si ya trabajó en tickets se hace baja lógica (Activo = false) para
            // no perder el historial de quién atendió cada servicio.
            if (mecanico.Tickets.Count > 0)
            {
                var ticketsAbiertos = mecanico.Tickets.Count(t =>
                    t.Estado != EstadoTicket.Entregado && t.Estado != EstadoTicket.Cancelado);

                if (ticketsAbiertos > 0)
                {
                    return BadRequest(new
                    {
                        mensaje = $"El mecánico tiene {ticketsAbiertos} ticket(s) sin entregar. Reasígnalos antes de darlo de baja."
                    });
                }

                mecanico.Activo = false;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = $"El mecánico tiene {mecanico.Tickets.Count} ticket(s) en el historial: se dio de baja (inactivo) en lugar de eliminarlo.",
                    mecanico = MapearADto(mecanico)
                });
            }

            _context.Mecanicos.Remove(mecanico);
            await _context.SaveChangesAsync();

            await _notificador.CatalogoAsync("eliminado", "Mecánico", $"{mecanico.Nombre} {mecanico.Apellidos}");

            return NoContent();
        }

        private static MecanicoDto MapearADto(Mecanico m) => new()
        {
            IdMecanico = m.IdMecanico,
            Nombre = m.Nombre,
            Apellidos = m.Apellidos,
            Especialidad = m.Especialidad,
            Telefono = m.Telefono,
            Activo = m.Activo,
            TotalTickets = m.Tickets.Count,
            TicketsAbiertos = m.Tickets.Count(t =>
                t.Estado != EstadoTicket.Entregado && t.Estado != EstadoTicket.Cancelado)
        };
    }
}

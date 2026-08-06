using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Models;
using GestionServiciosAutomotrices.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Controllers.Api
{
    /// <summary>
    /// API REST: CRUD del catálogo de servicios del taller.
    /// </summary>
    [ApiController]
    [Route("api/servicios")]
    public class ServiciosApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificadorEventos _notificador;

        public ServiciosApiController(AppDbContext context, INotificadorEventos notificador)
        {
            _context = context;
            _notificador = notificador;
        }

        // GET: api/servicios
        // Opcionalmente solo los activos: api/servicios?soloActivos=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ServicioDto>>> GetServicios([FromQuery] bool soloActivos = false)
        {
            var consulta = _context.Servicios
                .Include(s => s.TicketServicios)
                .AsQueryable();

            if (soloActivos)
            {
                consulta = consulta.Where(s => s.Activo);
            }

            var servicios = await consulta.OrderBy(s => s.Nombre).ToListAsync();

            return Ok(servicios.Select(MapearADto));
        }

        // GET: api/servicios/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServicioDto>> GetServicio(int id)
        {
            var servicio = await _context.Servicios
                .Include(s => s.TicketServicios)
                .FirstOrDefaultAsync(s => s.IdServicio == id);

            if (servicio == null)
            {
                return NotFound(new { mensaje = $"No existe un servicio con id {id}." });
            }

            return Ok(MapearADto(servicio));
        }

        // POST: api/servicios
        [HttpPost]
        [Authorize(Roles = RolUsuario.PuedenEditar)]
        public async Task<ActionResult<ServicioDto>> CrearServicio(ServicioGuardarDto dto)
        {
            var nombreRepetido = await _context.Servicios.AnyAsync(s => s.Nombre == dto.Nombre);
            if (nombreRepetido)
            {
                return BadRequest(new { mensaje = $"Ya existe un servicio llamado \"{dto.Nombre}\"." });
            }

            var servicio = new Servicio
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Precio = dto.Precio,
                TiempoEstimadoMin = dto.TiempoEstimadoMin,
                Activo = dto.Activo
            };

            _context.Servicios.Add(servicio);
            await _context.SaveChangesAsync();

            await _notificador.CatalogoAsync("creado", "Servicio",
                servicio.Nombre, $"Precio {servicio.Precio:C}", $"/Servicios/Details/{servicio.IdServicio}");

            return CreatedAtAction(nameof(GetServicio), new { id = servicio.IdServicio }, MapearADto(servicio));
        }

        // PUT: api/servicios/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = RolUsuario.PuedenEditar)]
        public async Task<ActionResult<ServicioDto>> ActualizarServicio(int id, ServicioGuardarDto dto)
        {
            var servicio = await _context.Servicios
                .Include(s => s.TicketServicios)
                .FirstOrDefaultAsync(s => s.IdServicio == id);

            if (servicio == null)
            {
                return NotFound(new { mensaje = $"No existe un servicio con id {id}." });
            }

            var nombreRepetido = await _context.Servicios
                .AnyAsync(s => s.Nombre == dto.Nombre && s.IdServicio != id);

            if (nombreRepetido)
            {
                return BadRequest(new { mensaje = $"Ya existe otro servicio llamado \"{dto.Nombre}\"." });
            }

            // Cambiar el precio aquí NO afecta los tickets ya creados: cada
            // TicketServicio guarda el PrecioAplicado del momento de la venta.
            servicio.Nombre = dto.Nombre;
            servicio.Descripcion = dto.Descripcion;
            servicio.Precio = dto.Precio;
            servicio.TiempoEstimadoMin = dto.TiempoEstimadoMin;
            servicio.Activo = dto.Activo;

            await _context.SaveChangesAsync();

            await _notificador.CatalogoAsync("actualizado", "Servicio",
                servicio.Nombre, $"Precio {servicio.Precio:C}", $"/Servicios/Details/{servicio.IdServicio}");

            return Ok(MapearADto(servicio));
        }

        // DELETE: api/servicios/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = RolUsuario.Administrador)]
        public async Task<IActionResult> EliminarServicio(int id)
        {
            var servicio = await _context.Servicios
                .Include(s => s.TicketServicios)
                .FirstOrDefaultAsync(s => s.IdServicio == id);

            if (servicio == null)
            {
                return NotFound(new { mensaje = $"No existe un servicio con id {id}." });
            }

            // Si el servicio ya se aplicó en tickets se desactiva en lugar de
            // borrarlo, para no romper el historial de esos tickets.
            if (servicio.TicketServicios.Count > 0)
            {
                servicio.Activo = false;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = $"El servicio se aplicó en {servicio.TicketServicios.Count} ticket(s): se desactivó en lugar de eliminarlo.",
                    servicio = MapearADto(servicio)
                });
            }

            _context.Servicios.Remove(servicio);
            await _context.SaveChangesAsync();

            await _notificador.CatalogoAsync("eliminado", "Servicio", servicio.Nombre);

            return NoContent();
        }

        private static ServicioDto MapearADto(Servicio s) => new()
        {
            IdServicio = s.IdServicio,
            Nombre = s.Nombre,
            Descripcion = s.Descripcion,
            Precio = s.Precio,
            TiempoEstimadoMin = s.TiempoEstimadoMin,
            Activo = s.Activo,
            VecesAplicado = s.TicketServicios.Count
        };
    }
}

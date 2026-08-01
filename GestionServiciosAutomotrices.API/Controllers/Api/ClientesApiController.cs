using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Controllers.Api
{
    /// <summary>
    /// API REST: CRUD del catálogo de clientes.
    /// </summary>
    [ApiController]
    [Route("api/clientes")]
    public class ClientesApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientesApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/clientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClienteDto>>> GetClientes()
        {
            var clientes = await _context.Clientes
                .Include(c => c.Vehiculos)
                .OrderBy(c => c.Apellidos)
                .ToListAsync();

            return Ok(clientes.Select(MapearADto));
        }

        // GET: api/clientes/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ClienteDto>> GetCliente(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Vehiculos)
                .FirstOrDefaultAsync(c => c.IdCliente == id);

            if (cliente == null)
            {
                return NotFound(new { mensaje = $"No existe un cliente con id {id}." });
            }

            return Ok(MapearADto(cliente));
        }

        // POST: api/clientes
        [HttpPost]
        public async Task<ActionResult<ClienteDto>> CrearCliente(ClienteGuardarDto dto)
        {
            var cliente = new Cliente
            {
                Nombre = dto.Nombre,
                Apellidos = dto.Apellidos,
                Telefono = dto.Telefono,
                Correo = dto.Correo,
                Direccion = dto.Direccion,
                FechaRegistro = DateTime.Now
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCliente), new { id = cliente.IdCliente }, MapearADto(cliente));
        }

        // PUT: api/clientes/5
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ClienteDto>> ActualizarCliente(int id, ClienteGuardarDto dto)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Vehiculos)
                .FirstOrDefaultAsync(c => c.IdCliente == id);

            if (cliente == null)
            {
                return NotFound(new { mensaje = $"No existe un cliente con id {id}." });
            }

            cliente.Nombre = dto.Nombre;
            cliente.Apellidos = dto.Apellidos;
            cliente.Telefono = dto.Telefono;
            cliente.Correo = dto.Correo;
            cliente.Direccion = dto.Direccion;

            await _context.SaveChangesAsync();

            return Ok(MapearADto(cliente));
        }

        // DELETE: api/clientes/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> EliminarCliente(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Vehiculos)
                .FirstOrDefaultAsync(c => c.IdCliente == id);

            if (cliente == null)
            {
                return NotFound(new { mensaje = $"No existe un cliente con id {id}." });
            }

            // Regla de negocio: no se puede borrar un cliente que tiene vehículos
            // registrados (esos vehículos podrían tener tickets en el historial).
            if (cliente.Vehiculos.Count > 0)
            {
                return BadRequest(new
                {
                    mensaje = $"El cliente tiene {cliente.Vehiculos.Count} vehículo(s) registrado(s) y no puede eliminarse."
                });
            }

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static ClienteDto MapearADto(Cliente c) => new()
        {
            IdCliente = c.IdCliente,
            Nombre = c.Nombre,
            Apellidos = c.Apellidos,
            Telefono = c.Telefono,
            Correo = c.Correo,
            Direccion = c.Direccion,
            FechaRegistro = c.FechaRegistro,
            TotalVehiculos = c.Vehiculos.Count
        };
    }
}

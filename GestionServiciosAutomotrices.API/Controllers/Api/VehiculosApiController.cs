using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Models;
using GestionServiciosAutomotrices.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Controllers.Api
{
    /// <summary>
    /// API REST: CRUD de vehículos.
    /// </summary>
    [ApiController]
    [Route("api/vehiculos")]
    public class VehiculosApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificadorEventos _notificador;

        public VehiculosApiController(AppDbContext context, INotificadorEventos notificador)
        {
            _context = context;
            _notificador = notificador;
        }

        // GET: api/vehiculos
        // Opcionalmente filtra por cliente: api/vehiculos?idCliente=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehiculoDto>>> GetVehiculos([FromQuery] int? idCliente)
        {
            var consulta = _context.Vehiculos
                .Include(v => v.Cliente)
                .Include(v => v.Tickets)
                .AsQueryable();

            if (idCliente.HasValue)
            {
                consulta = consulta.Where(v => v.IdCliente == idCliente.Value);
            }

            var vehiculos = await consulta.OrderBy(v => v.Marca).ThenBy(v => v.Modelo).ToListAsync();

            return Ok(vehiculos.Select(MapearADto));
        }

        // GET: api/vehiculos/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<VehiculoDto>> GetVehiculo(int id)
        {
            var vehiculo = await BuscarConRelaciones(id);

            if (vehiculo == null)
            {
                return NotFound(new { mensaje = $"No existe un vehículo con id {id}." });
            }

            return Ok(MapearADto(vehiculo));
        }

        // POST: api/vehiculos
        [HttpPost]
        public async Task<ActionResult<VehiculoDto>> CrearVehiculo(VehiculoGuardarDto dto)
        {
            var error = await ValidarAsync(dto, idVehiculoActual: null);
            if (error != null)
            {
                return BadRequest(new { mensaje = error });
            }

            var vehiculo = new Vehiculo
            {
                IdCliente = dto.IdCliente,
                Marca = dto.Marca,
                Modelo = dto.Modelo,
                Anio = dto.Anio,
                Placas = dto.Placas.ToUpperInvariant(),
                Color = dto.Color,
                NumeroSerie = dto.NumeroSerie?.ToUpperInvariant()
            };

            _context.Vehiculos.Add(vehiculo);
            await _context.SaveChangesAsync();

            await _context.Entry(vehiculo).Reference(v => v.Cliente).LoadAsync();

            await _notificador.CatalogoAsync("creado", "Vehículo",
                $"{vehiculo.Marca} {vehiculo.Modelo} ({vehiculo.Placas})", $"Año {vehiculo.Anio}", $"/Vehiculos/Details/{vehiculo.IdVehiculo}");

            return CreatedAtAction(nameof(GetVehiculo), new { id = vehiculo.IdVehiculo }, MapearADto(vehiculo));
        }

        // PUT: api/vehiculos/5
        [HttpPut("{id:int}")]
        public async Task<ActionResult<VehiculoDto>> ActualizarVehiculo(int id, VehiculoGuardarDto dto)
        {
            var vehiculo = await BuscarConRelaciones(id);

            if (vehiculo == null)
            {
                return NotFound(new { mensaje = $"No existe un vehículo con id {id}." });
            }

            var error = await ValidarAsync(dto, idVehiculoActual: id);
            if (error != null)
            {
                return BadRequest(new { mensaje = error });
            }

            vehiculo.IdCliente = dto.IdCliente;
            vehiculo.Marca = dto.Marca;
            vehiculo.Modelo = dto.Modelo;
            vehiculo.Anio = dto.Anio;
            vehiculo.Placas = dto.Placas.ToUpperInvariant();
            vehiculo.Color = dto.Color;
            vehiculo.NumeroSerie = dto.NumeroSerie?.ToUpperInvariant();

            await _context.SaveChangesAsync();
            await _context.Entry(vehiculo).Reference(v => v.Cliente).LoadAsync();

            await _notificador.CatalogoAsync("actualizado", "Vehículo",
                $"{vehiculo.Marca} {vehiculo.Modelo} ({vehiculo.Placas})", $"Año {vehiculo.Anio}", $"/Vehiculos/Details/{vehiculo.IdVehiculo}");

            return Ok(MapearADto(vehiculo));
        }

        // DELETE: api/vehiculos/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> EliminarVehiculo(int id)
        {
            var vehiculo = await _context.Vehiculos
                .Include(v => v.Tickets)
                .FirstOrDefaultAsync(v => v.IdVehiculo == id);

            if (vehiculo == null)
            {
                return NotFound(new { mensaje = $"No existe un vehículo con id {id}." });
            }

            // Regla de negocio: el historial de tickets del taller no se pierde.
            if (vehiculo.Tickets.Count > 0)
            {
                return BadRequest(new
                {
                    mensaje = $"El vehículo tiene {vehiculo.Tickets.Count} ticket(s) en el historial y no puede eliminarse."
                });
            }

            _context.Vehiculos.Remove(vehiculo);
            await _context.SaveChangesAsync();

            await _notificador.CatalogoAsync("eliminado", "Vehículo", $"{vehiculo.Marca} {vehiculo.Modelo} ({vehiculo.Placas})");

            return NoContent();
        }

        // ----------------- Métodos de apoyo -----------------

        private Task<Vehiculo?> BuscarConRelaciones(int id)
        {
            return _context.Vehiculos
                .Include(v => v.Cliente)
                .Include(v => v.Tickets)
                .FirstOrDefaultAsync(v => v.IdVehiculo == id);
        }

        /// <summary>
        /// Valida que el cliente exista y que las placas no estén repetidas.
        /// Devuelve el mensaje de error o null si todo está bien.
        /// </summary>
        private async Task<string?> ValidarAsync(VehiculoGuardarDto dto, int? idVehiculoActual)
        {
            var existeCliente = await _context.Clientes.AnyAsync(c => c.IdCliente == dto.IdCliente);
            if (!existeCliente)
            {
                return $"El cliente con id {dto.IdCliente} no está registrado.";
            }

            var placas = dto.Placas.ToUpperInvariant();
            var placasRepetidas = await _context.Vehiculos
                .AnyAsync(v => v.Placas == placas && v.IdVehiculo != idVehiculoActual);

            if (placasRepetidas)
            {
                return $"Ya existe otro vehículo registrado con las placas {placas}.";
            }

            return null;
        }

        private static VehiculoDto MapearADto(Vehiculo v) => new()
        {
            IdVehiculo = v.IdVehiculo,
            Marca = v.Marca,
            Modelo = v.Modelo,
            Anio = v.Anio,
            Placas = v.Placas,
            Color = v.Color,
            NumeroSerie = v.NumeroSerie,
            IdCliente = v.IdCliente,
            Cliente = v.Cliente != null ? $"{v.Cliente.Nombre} {v.Cliente.Apellidos}" : string.Empty,
            TotalTickets = v.Tickets.Count
        };
    }
}

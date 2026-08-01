using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Controllers
{
    /// <summary>
    /// CRUD de vehículos con vistas MVC (/Vehiculos).
    /// </summary>
    public class VehiculosController : Controller
    {
        private readonly AppDbContext _context;

        public VehiculosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Vehiculos
        public async Task<IActionResult> Index(string? buscar)
        {
            var consulta = _context.Vehiculos
                .Include(v => v.Cliente)
                .Include(v => v.Tickets)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                consulta = consulta.Where(v =>
                    v.Marca.Contains(buscar) ||
                    v.Modelo.Contains(buscar) ||
                    v.Placas.Contains(buscar));
            }

            ViewBag.Buscar = buscar;
            return View(await consulta.OrderBy(v => v.Marca).ThenBy(v => v.Modelo).ToListAsync());
        }

        // GET: /Vehiculos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vehiculo = await _context.Vehiculos
                .Include(v => v.Cliente)
                .Include(v => v.Tickets)
                .FirstOrDefaultAsync(v => v.IdVehiculo == id);

            if (vehiculo == null)
            {
                return NotFound();
            }

            return View(vehiculo);
        }

        // GET: /Vehiculos/Create
        // Puede venir con un cliente preseleccionado desde la ficha del cliente.
        public async Task<IActionResult> Create(int? idCliente)
        {
            var dto = new VehiculoGuardarDto();

            if (idCliente.HasValue)
            {
                dto.IdCliente = idCliente.Value;
            }

            await CargarClientesAsync(dto.IdCliente);
            return View(dto);
        }

        // POST: /Vehiculos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VehiculoGuardarDto dto)
        {
            await ValidarEnModelStateAsync(dto, idVehiculoActual: null);

            if (!ModelState.IsValid)
            {
                await CargarClientesAsync(dto.IdCliente);
                return View(dto);
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

            TempData["Mensaje"] = $"Vehículo {vehiculo.Marca} {vehiculo.Modelo} ({vehiculo.Placas}) registrado correctamente.";
            return RedirectToAction(nameof(Details), new { id = vehiculo.IdVehiculo });
        }

        // GET: /Vehiculos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(id);

            if (vehiculo == null)
            {
                return NotFound();
            }

            var dto = new VehiculoGuardarDto
            {
                IdCliente = vehiculo.IdCliente,
                Marca = vehiculo.Marca,
                Modelo = vehiculo.Modelo,
                Anio = vehiculo.Anio,
                Placas = vehiculo.Placas,
                Color = vehiculo.Color,
                NumeroSerie = vehiculo.NumeroSerie
            };

            ViewBag.IdVehiculo = id;
            await CargarClientesAsync(dto.IdCliente);
            return View(dto);
        }

        // POST: /Vehiculos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VehiculoGuardarDto dto)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(id);

            if (vehiculo == null)
            {
                return NotFound();
            }

            await ValidarEnModelStateAsync(dto, idVehiculoActual: id);

            if (!ModelState.IsValid)
            {
                ViewBag.IdVehiculo = id;
                await CargarClientesAsync(dto.IdCliente);
                return View(dto);
            }

            vehiculo.IdCliente = dto.IdCliente;
            vehiculo.Marca = dto.Marca;
            vehiculo.Modelo = dto.Modelo;
            vehiculo.Anio = dto.Anio;
            vehiculo.Placas = dto.Placas.ToUpperInvariant();
            vehiculo.Color = dto.Color;
            vehiculo.NumeroSerie = dto.NumeroSerie?.ToUpperInvariant();

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Vehículo actualizado correctamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Vehiculos/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var vehiculo = await _context.Vehiculos
                .Include(v => v.Cliente)
                .Include(v => v.Tickets)
                .FirstOrDefaultAsync(v => v.IdVehiculo == id);

            if (vehiculo == null)
            {
                return NotFound();
            }

            return View(vehiculo);
        }

        // POST: /Vehiculos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vehiculo = await _context.Vehiculos
                .Include(v => v.Tickets)
                .FirstOrDefaultAsync(v => v.IdVehiculo == id);

            if (vehiculo == null)
            {
                return NotFound();
            }

            if (vehiculo.Tickets.Count > 0)
            {
                TempData["Error"] = $"El vehículo tiene {vehiculo.Tickets.Count} ticket(s) en el historial y no puede eliminarse.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.Vehiculos.Remove(vehiculo);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Vehículo {vehiculo.Placas} eliminado.";
            return RedirectToAction(nameof(Index));
        }

        // ----------------- Métodos de apoyo -----------------

        private async Task CargarClientesAsync(int? seleccionado)
        {
            var clientes = await _context.Clientes
                .OrderBy(c => c.Apellidos)
                .Select(c => new
                {
                    c.IdCliente,
                    Texto = c.Nombre + " " + c.Apellidos + (c.Telefono != null ? " — " + c.Telefono : "")
                })
                .ToListAsync();

            ViewBag.Clientes = new SelectList(clientes, "IdCliente", "Texto", seleccionado);
        }

        /// <summary>
        /// Valida cliente existente y placas no repetidas, agregando los errores a ModelState.
        /// </summary>
        private async Task ValidarEnModelStateAsync(VehiculoGuardarDto dto, int? idVehiculoActual)
        {
            var existeCliente = await _context.Clientes.AnyAsync(c => c.IdCliente == dto.IdCliente);
            if (!existeCliente)
            {
                ModelState.AddModelError(nameof(dto.IdCliente), "El cliente seleccionado no está registrado.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Placas))
            {
                var placas = dto.Placas.ToUpperInvariant();
                var repetidas = await _context.Vehiculos
                    .AnyAsync(v => v.Placas == placas && v.IdVehiculo != idVehiculoActual);

                if (repetidas)
                {
                    ModelState.AddModelError(nameof(dto.Placas), $"Ya existe otro vehículo con las placas {placas}.");
                }
            }
        }
    }
}

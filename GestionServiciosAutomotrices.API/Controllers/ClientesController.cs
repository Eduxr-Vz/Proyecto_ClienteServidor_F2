using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Models;
using GestionServiciosAutomotrices.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Controllers
{
    /// <summary>
    /// CRUD de clientes con vistas MVC (/Clientes).
    /// La API REST equivalente está en Controllers/Api/ClientesApiController.
    /// </summary>
    public class ClientesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotificadorEventos _notificador;

        public ClientesController(AppDbContext context, INotificadorEventos notificador)
        {
            _context = context;
            _notificador = notificador;
        }

        // GET: /Clientes
        public async Task<IActionResult> Index(string? buscar)
        {
            var consulta = _context.Clientes
                .Include(c => c.Vehiculos)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                consulta = consulta.Where(c =>
                    c.Nombre.Contains(buscar) ||
                    c.Apellidos.Contains(buscar) ||
                    (c.Telefono != null && c.Telefono.Contains(buscar)));
            }

            ViewBag.Buscar = buscar;
            return View(await consulta.OrderBy(c => c.Apellidos).ToListAsync());
        }

        // GET: /Clientes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Vehiculos)
                .FirstOrDefaultAsync(c => c.IdCliente == id);

            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }

        // GET: /Clientes/Create
        [Authorize(Roles = RolUsuario.PuedenEditar)]
        public IActionResult Create()
        {
            return View(new ClienteGuardarDto());
        }

        // POST: /Clientes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolUsuario.PuedenEditar)]
        public async Task<IActionResult> Create(ClienteGuardarDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

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

            await _notificador.CatalogoAsync("creado", "Cliente",
                $"{cliente.Nombre} {cliente.Apellidos}",
                cliente.Telefono, $"/Clientes/Details/{cliente.IdCliente}");

            TempData["Mensaje"] = $"Cliente {cliente.Nombre} {cliente.Apellidos} registrado correctamente.";
            return RedirectToAction(nameof(Details), new { id = cliente.IdCliente });
        }

        // GET: /Clientes/Edit/5
        [Authorize(Roles = RolUsuario.PuedenEditar)]
        public async Task<IActionResult> Edit(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
            {
                return NotFound();
            }

            ViewBag.IdCliente = cliente.IdCliente;
            return View(new ClienteGuardarDto
            {
                Nombre = cliente.Nombre,
                Apellidos = cliente.Apellidos,
                Telefono = cliente.Telefono,
                Correo = cliente.Correo,
                Direccion = cliente.Direccion
            });
        }

        // POST: /Clientes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolUsuario.PuedenEditar)]
        public async Task<IActionResult> Edit(int id, ClienteGuardarDto dto)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.IdCliente = id;
                return View(dto);
            }

            cliente.Nombre = dto.Nombre;
            cliente.Apellidos = dto.Apellidos;
            cliente.Telefono = dto.Telefono;
            cliente.Correo = dto.Correo;
            cliente.Direccion = dto.Direccion;

            await _context.SaveChangesAsync();

            await _notificador.CatalogoAsync("actualizado", "Cliente",
                $"{cliente.Nombre} {cliente.Apellidos}",
                cliente.Telefono, $"/Clientes/Details/{cliente.IdCliente}");

            TempData["Mensaje"] = "Cliente actualizado correctamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Clientes/Delete/5
        [Authorize(Roles = RolUsuario.Administrador)]
        public async Task<IActionResult> Delete(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Vehiculos)
                .FirstOrDefaultAsync(c => c.IdCliente == id);

            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }

        // POST: /Clientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolUsuario.Administrador)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Vehiculos)
                .FirstOrDefaultAsync(c => c.IdCliente == id);

            if (cliente == null)
            {
                return NotFound();
            }

            // Regla de negocio: no se borra un cliente con vehículos registrados.
            if (cliente.Vehiculos.Count > 0)
            {
                TempData["Error"] = $"El cliente tiene {cliente.Vehiculos.Count} vehículo(s) registrado(s) y no puede eliminarse.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            await _notificador.CatalogoAsync("eliminado", "Cliente",
                $"{cliente.Nombre} {cliente.Apellidos}");

            TempData["Mensaje"] = $"Cliente {cliente.Nombre} {cliente.Apellidos} eliminado.";
            return RedirectToAction(nameof(Index));
        }
    }
}

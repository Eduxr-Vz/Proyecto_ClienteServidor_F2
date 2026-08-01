using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Controllers
{
    /// <summary>
    /// CRUD de mecánicos con vistas MVC (/Mecanicos).
    /// </summary>
    public class MecanicosController : Controller
    {
        private readonly AppDbContext _context;

        public MecanicosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Mecanicos
        public async Task<IActionResult> Index(bool soloActivos = false)
        {
            var consulta = _context.Mecanicos
                .Include(m => m.Tickets)
                .AsQueryable();

            if (soloActivos)
            {
                consulta = consulta.Where(m => m.Activo);
            }

            ViewBag.SoloActivos = soloActivos;
            return View(await consulta.OrderBy(m => m.Nombre).ToListAsync());
        }

        // GET: /Mecanicos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var mecanico = await _context.Mecanicos
                .Include(m => m.Tickets)
                    .ThenInclude(t => t.Vehiculo)
                .FirstOrDefaultAsync(m => m.IdMecanico == id);

            if (mecanico == null)
            {
                return NotFound();
            }

            return View(mecanico);
        }

        // GET: /Mecanicos/Create
        public IActionResult Create()
        {
            return View(new MecanicoGuardarDto());
        }

        // POST: /Mecanicos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MecanicoGuardarDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

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

            TempData["Mensaje"] = $"Mecánico {mecanico.Nombre} {mecanico.Apellidos} registrado correctamente.";
            return RedirectToAction(nameof(Details), new { id = mecanico.IdMecanico });
        }

        // GET: /Mecanicos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var mecanico = await _context.Mecanicos.FindAsync(id);

            if (mecanico == null)
            {
                return NotFound();
            }

            ViewBag.IdMecanico = id;
            return View(new MecanicoGuardarDto
            {
                Nombre = mecanico.Nombre,
                Apellidos = mecanico.Apellidos,
                Especialidad = mecanico.Especialidad,
                Telefono = mecanico.Telefono,
                Activo = mecanico.Activo
            });
        }

        // POST: /Mecanicos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MecanicoGuardarDto dto)
        {
            var mecanico = await _context.Mecanicos
                .Include(m => m.Tickets)
                .FirstOrDefaultAsync(m => m.IdMecanico == id);

            if (mecanico == null)
            {
                return NotFound();
            }

            // No se puede dar de baja a un mecánico con trabajo pendiente.
            if (!dto.Activo && mecanico.Activo)
            {
                var ticketsAbiertos = mecanico.Tickets.Count(t =>
                    t.Estado != EstadoTicket.Entregado && t.Estado != EstadoTicket.Cancelado);

                if (ticketsAbiertos > 0)
                {
                    ModelState.AddModelError(nameof(dto.Activo),
                        $"Tiene {ticketsAbiertos} ticket(s) sin entregar. Reasígnalos antes de darlo de baja.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.IdMecanico = id;
                return View(dto);
            }

            mecanico.Nombre = dto.Nombre;
            mecanico.Apellidos = dto.Apellidos;
            mecanico.Especialidad = dto.Especialidad;
            mecanico.Telefono = dto.Telefono;
            mecanico.Activo = dto.Activo;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Mecánico actualizado correctamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Mecanicos/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var mecanico = await _context.Mecanicos
                .Include(m => m.Tickets)
                .FirstOrDefaultAsync(m => m.IdMecanico == id);

            if (mecanico == null)
            {
                return NotFound();
            }

            return View(mecanico);
        }

        // POST: /Mecanicos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mecanico = await _context.Mecanicos
                .Include(m => m.Tickets)
                .FirstOrDefaultAsync(m => m.IdMecanico == id);

            if (mecanico == null)
            {
                return NotFound();
            }

            var ticketsAbiertos = mecanico.Tickets.Count(t =>
                t.Estado != EstadoTicket.Entregado && t.Estado != EstadoTicket.Cancelado);

            if (ticketsAbiertos > 0)
            {
                TempData["Error"] = $"El mecánico tiene {ticketsAbiertos} ticket(s) sin entregar. Reasígnalos antes de darlo de baja.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Baja lógica si ya trabajó en tickets: se conserva el historial.
            if (mecanico.Tickets.Count > 0)
            {
                mecanico.Activo = false;
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = $"El mecánico tiene tickets en el historial: se dio de baja (inactivo) en lugar de eliminarlo.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.Mecanicos.Remove(mecanico);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Mecánico {mecanico.Nombre} {mecanico.Apellidos} eliminado.";
            return RedirectToAction(nameof(Index));
        }
    }
}

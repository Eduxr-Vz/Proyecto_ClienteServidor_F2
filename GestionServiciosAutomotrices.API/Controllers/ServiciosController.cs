using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Controllers
{
    /// <summary>
    /// CRUD del catálogo de servicios con vistas MVC (/Servicios).
    /// </summary>
    public class ServiciosController : Controller
    {
        private readonly AppDbContext _context;

        public ServiciosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Servicios
        public async Task<IActionResult> Index(bool soloActivos = false)
        {
            var consulta = _context.Servicios
                .Include(s => s.TicketServicios)
                .AsQueryable();

            if (soloActivos)
            {
                consulta = consulta.Where(s => s.Activo);
            }

            ViewBag.SoloActivos = soloActivos;
            return View(await consulta.OrderBy(s => s.Nombre).ToListAsync());
        }

        // GET: /Servicios/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var servicio = await _context.Servicios
                .Include(s => s.TicketServicios)
                    .ThenInclude(ts => ts.Ticket)
                .FirstOrDefaultAsync(s => s.IdServicio == id);

            if (servicio == null)
            {
                return NotFound();
            }

            return View(servicio);
        }

        // GET: /Servicios/Create
        public IActionResult Create()
        {
            return View(new ServicioGuardarDto());
        }

        // POST: /Servicios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServicioGuardarDto dto)
        {
            await ValidarNombreAsync(dto.Nombre, idActual: null);

            if (!ModelState.IsValid)
            {
                return View(dto);
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

            TempData["Mensaje"] = $"Servicio \"{servicio.Nombre}\" agregado al catálogo.";
            return RedirectToAction(nameof(Details), new { id = servicio.IdServicio });
        }

        // GET: /Servicios/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var servicio = await _context.Servicios.FindAsync(id);

            if (servicio == null)
            {
                return NotFound();
            }

            ViewBag.IdServicio = id;
            return View(new ServicioGuardarDto
            {
                Nombre = servicio.Nombre,
                Descripcion = servicio.Descripcion,
                Precio = servicio.Precio,
                TiempoEstimadoMin = servicio.TiempoEstimadoMin,
                Activo = servicio.Activo
            });
        }

        // POST: /Servicios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServicioGuardarDto dto)
        {
            var servicio = await _context.Servicios.FindAsync(id);

            if (servicio == null)
            {
                return NotFound();
            }

            await ValidarNombreAsync(dto.Nombre, idActual: id);

            if (!ModelState.IsValid)
            {
                ViewBag.IdServicio = id;
                return View(dto);
            }

            servicio.Nombre = dto.Nombre;
            servicio.Descripcion = dto.Descripcion;
            servicio.Precio = dto.Precio;
            servicio.TiempoEstimadoMin = dto.TiempoEstimadoMin;
            servicio.Activo = dto.Activo;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Servicio actualizado. Los tickets ya creados conservan su precio original.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Servicios/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var servicio = await _context.Servicios
                .Include(s => s.TicketServicios)
                .FirstOrDefaultAsync(s => s.IdServicio == id);

            if (servicio == null)
            {
                return NotFound();
            }

            return View(servicio);
        }

        // POST: /Servicios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var servicio = await _context.Servicios
                .Include(s => s.TicketServicios)
                .FirstOrDefaultAsync(s => s.IdServicio == id);

            if (servicio == null)
            {
                return NotFound();
            }

            // Baja lógica si ya se usó en tickets.
            if (servicio.TicketServicios.Count > 0)
            {
                servicio.Activo = false;
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = $"El servicio se aplicó en {servicio.TicketServicios.Count} ticket(s): se desactivó en lugar de eliminarlo.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.Servicios.Remove(servicio);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Servicio \"{servicio.Nombre}\" eliminado del catálogo.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidarNombreAsync(string nombre, int? idActual)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return;
            }

            var repetido = await _context.Servicios
                .AnyAsync(s => s.Nombre == nombre && s.IdServicio != idActual);

            if (repetido)
            {
                ModelState.AddModelError("Nombre", $"Ya existe otro servicio llamado \"{nombre}\".");
            }
        }
    }
}

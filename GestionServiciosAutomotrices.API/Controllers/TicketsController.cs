using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Controllers
{
    /// <summary>
    /// CRUD de tickets con vistas MVC (interfaz web en /Tickets).
    /// La API REST equivalente está en Controllers/Api/TicketsApiController (api/tickets).
    /// </summary>
    public class TicketsController : Controller
    {
        private readonly AppDbContext _context;

        public TicketsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Tickets
        public async Task<IActionResult> Index()
        {
            var tickets = await _context.Tickets
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v!.Cliente)
                .Include(t => t.Mecanico)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            return View(tickets);
        }

        // GET: /Tickets/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v!.Cliente)
                .Include(t => t.Mecanico)
                .Include(t => t.TicketServicios)
                    .ThenInclude(ts => ts.Servicio)
                .FirstOrDefaultAsync(t => t.IdTicket == id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // GET: /Tickets/Create
        public async Task<IActionResult> Create()
        {
            await CargarListasAsync();
            return View(new TicketCrearDto());
        }

        // POST: /Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketCrearDto dto)
        {
            var vehiculo = await _context.Vehiculos
                .FirstOrDefaultAsync(v => v.IdVehiculo == dto.IdVehiculo);

            if (vehiculo == null)
            {
                ModelState.AddModelError(nameof(dto.IdVehiculo), "El vehículo seleccionado no está registrado.");
            }

            if (dto.IdMecanico.HasValue)
            {
                var existeMecanico = await _context.Mecanicos
                    .AnyAsync(m => m.IdMecanico == dto.IdMecanico.Value && m.Activo);

                if (!existeMecanico)
                {
                    ModelState.AddModelError(nameof(dto.IdMecanico), "El mecánico seleccionado no existe o no está activo.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarListasAsync(dto);
                return View(dto);
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

            // Servicios solicitados: se congela el precio vigente y se calcula el total.
            if (dto.IdsServicios is { Count: > 0 })
            {
                var idsSolicitados = dto.IdsServicios.Distinct().ToList();
                var servicios = await _context.Servicios
                    .Where(s => idsSolicitados.Contains(s.IdServicio) && s.Activo)
                    .ToListAsync();

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

            TempData["Mensaje"] = $"Ticket {ticket.Folio} creado correctamente.";
            return RedirectToAction(nameof(Details), new { id = ticket.IdTicket });
        }

        // GET: /Tickets/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Vehiculo)
                .FirstOrDefaultAsync(t => t.IdTicket == id);

            if (ticket == null)
            {
                return NotFound();
            }

            var dto = new TicketActualizarDto
            {
                IdMecanico = ticket.IdMecanico,
                Estado = ticket.Estado,
                FechaEstimadaEntrega = ticket.FechaEstimadaEntrega,
                DescripcionProblema = ticket.DescripcionProblema,
                Observaciones = ticket.Observaciones
            };

            await CargarDatosEdicionAsync(ticket, dto);
            return View(dto);
        }

        // POST: /Tickets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TicketActualizarDto dto)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Vehiculo)
                .FirstOrDefaultAsync(t => t.IdTicket == id);

            if (ticket == null)
            {
                return NotFound();
            }

            if (dto.IdMecanico.HasValue)
            {
                var existeMecanico = await _context.Mecanicos
                    .AnyAsync(m => m.IdMecanico == dto.IdMecanico.Value && m.Activo);

                if (!existeMecanico)
                {
                    ModelState.AddModelError(nameof(dto.IdMecanico), "El mecánico seleccionado no existe o no está activo.");
                }
            }

            if (dto.Estado.HasValue)
            {
                var error = TicketReglas.ValidarCambioDeEstado(ticket, dto.Estado.Value);
                if (error != null)
                {
                    ModelState.AddModelError(nameof(dto.Estado), error);
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarDatosEdicionAsync(ticket, dto);
                return View(dto);
            }

            if (dto.IdMecanico.HasValue)
            {
                ticket.IdMecanico = dto.IdMecanico;
            }

            if (dto.Estado.HasValue)
            {
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

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Ticket {ticket.Folio} actualizado correctamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Tickets/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v!.Cliente)
                .Include(t => t.Mecanico)
                .FirstOrDefaultAsync(t => t.IdTicket == id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // POST: /Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.TicketServicios)
                .FirstOrDefaultAsync(t => t.IdTicket == id);

            if (ticket == null)
            {
                return NotFound();
            }

            // Regla de negocio: un ticket entregado forma parte del historial.
            if (ticket.Estado == EstadoTicket.Entregado)
            {
                TempData["Error"] = $"El ticket {ticket.Folio} está entregado y no puede eliminarse.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.TicketServicios.RemoveRange(ticket.TicketServicios);
            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Ticket {ticket.Folio} eliminado.";
            return RedirectToAction(nameof(Index));
        }

        // ----------------- Métodos de apoyo -----------------

        /// <summary>
        /// Llena los combos y la lista de servicios del formulario de creación.
        /// </summary>
        private async Task CargarListasAsync(TicketCrearDto? dto = null)
        {
            var vehiculos = await _context.Vehiculos
                .Include(v => v.Cliente)
                .OrderBy(v => v.Marca)
                .Select(v => new
                {
                    v.IdVehiculo,
                    Texto = v.Marca + " " + v.Modelo + " (" + v.Placas + ") — "
                            + v.Cliente!.Nombre + " " + v.Cliente.Apellidos
                })
                .ToListAsync();

            ViewBag.Vehiculos = new SelectList(vehiculos, "IdVehiculo", "Texto", dto?.IdVehiculo);
            ViewBag.Mecanicos = new SelectList(await ListaMecanicosAsync(), "IdMecanico", "Texto", dto?.IdMecanico);
            ViewBag.Servicios = await _context.Servicios
                .Where(s => s.Activo)
                .OrderBy(s => s.Nombre)
                .ToListAsync();
        }

        /// <summary>
        /// Llena los datos que necesita el formulario de edición.
        /// </summary>
        private async Task CargarDatosEdicionAsync(Ticket ticket, TicketActualizarDto dto)
        {
            ViewBag.IdTicket = ticket.IdTicket;
            ViewBag.Folio = ticket.Folio;
            ViewBag.VehiculoTexto = ticket.Vehiculo != null
                ? $"{ticket.Vehiculo.Marca} {ticket.Vehiculo.Modelo} ({ticket.Vehiculo.Placas})"
                : string.Empty;
            ViewBag.EstadoActual = ticket.Estado;
            ViewBag.Mecanicos = new SelectList(await ListaMecanicosAsync(), "IdMecanico", "Texto", dto.IdMecanico);
        }

        private async Task<List<object>> ListaMecanicosAsync()
        {
            var mecanicos = await _context.Mecanicos
                .Where(m => m.Activo)
                .OrderBy(m => m.Nombre)
                .Select(m => new
                {
                    m.IdMecanico,
                    Texto = m.Nombre + " " + m.Apellidos + " — " + (m.Especialidad ?? "General")
                })
                .ToListAsync();

            return mecanicos.Cast<object>().ToList();
        }
    }
}

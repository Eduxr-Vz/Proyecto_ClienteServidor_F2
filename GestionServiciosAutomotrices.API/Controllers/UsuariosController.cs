using System.Security.Claims;
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
    /// Gestión de las cuentas del sistema (/Usuarios).
    /// Solo el administrador puede entrar aquí.
    /// </summary>
    [Authorize(Roles = RolUsuario.Administrador)]
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ServicioUsuarios _usuarios;

        public UsuariosController(AppDbContext context, ServicioUsuarios usuarios)
        {
            _context = context;
            _usuarios = usuarios;
        }

        // GET: /Usuarios
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .OrderBy(u => u.NombreUsuario)
                .ToListAsync();

            return View(usuarios);
        }

        // GET: /Usuarios/Create
        public IActionResult Create()
        {
            return View(new UsuarioGuardarDto());
        }

        // POST: /Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioGuardarDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Contrasena))
            {
                ModelState.AddModelError(nameof(dto.Contrasena), "La contraseña es obligatoria al crear la cuenta.");
            }

            await ValidarNombreAsync(dto.NombreUsuario, idActual: null);

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var usuario = new Usuario
            {
                NombreUsuario = dto.NombreUsuario,
                NombreCompleto = dto.NombreCompleto,
                Rol = dto.Rol,
                Activo = dto.Activo,
            };
            usuario.ContrasenaHash = _usuarios.CalcularHash(usuario, dto.Contrasena!);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Cuenta de {usuario.NombreUsuario} creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Usuarios/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            ViewBag.IdUsuario = id;
            ViewBag.NombreUsuarioActual = usuario.NombreUsuario;
            return View(new UsuarioGuardarDto
            {
                NombreUsuario = usuario.NombreUsuario,
                NombreCompleto = usuario.NombreCompleto,
                Rol = usuario.Rol,
                Activo = usuario.Activo,
            });
        }

        // POST: /Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UsuarioGuardarDto dto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            await ValidarNombreAsync(dto.NombreUsuario, idActual: id);

            // Nadie puede quitarse a sí mismo el rol de administrador ni
            // desactivar su propia cuenta: se quedaría fuera del sistema.
            if (EsMiCuenta(usuario))
            {
                if (dto.Rol != RolUsuario.Administrador)
                {
                    ModelState.AddModelError(nameof(dto.Rol), "No puedes quitarte a ti mismo el rol de administrador.");
                }

                if (!dto.Activo)
                {
                    ModelState.AddModelError(nameof(dto.Activo), "No puedes desactivar tu propia cuenta.");
                }
            }
            else if (usuario.Rol == RolUsuario.Administrador && dto.Rol != RolUsuario.Administrador)
            {
                // Tampoco se puede dejar al sistema sin ningún administrador.
                var otrosAdmins = await _context.Usuarios
                    .CountAsync(u => u.Rol == RolUsuario.Administrador && u.Activo && u.IdUsuario != id);

                if (otrosAdmins == 0)
                {
                    ModelState.AddModelError(nameof(dto.Rol), "Debe quedar al menos un administrador activo.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.IdUsuario = id;
                ViewBag.NombreUsuarioActual = usuario.NombreUsuario;
                return View(dto);
            }

            usuario.NombreUsuario = dto.NombreUsuario;
            usuario.NombreCompleto = dto.NombreCompleto;
            usuario.Rol = dto.Rol;
            usuario.Activo = dto.Activo;

            // La contraseña solo se cambia si escribieron una nueva.
            if (!string.IsNullOrWhiteSpace(dto.Contrasena))
            {
                usuario.ContrasenaHash = _usuarios.CalcularHash(usuario, dto.Contrasena);
            }

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Cuenta de {usuario.NombreUsuario} actualizada.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Usuarios/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            ViewBag.EsMiCuenta = EsMiCuenta(usuario);
            return View(usuario);
        }

        // POST: /Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            if (EsMiCuenta(usuario))
            {
                TempData["Error"] = "No puedes eliminar tu propia cuenta.";
                return RedirectToAction(nameof(Index));
            }

            if (usuario.Rol == RolUsuario.Administrador)
            {
                var otrosAdmins = await _context.Usuarios
                    .CountAsync(u => u.Rol == RolUsuario.Administrador && u.Activo && u.IdUsuario != id);

                if (otrosAdmins == 0)
                {
                    TempData["Error"] = "Debe quedar al menos un administrador activo.";
                    return RedirectToAction(nameof(Index));
                }
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Cuenta de {usuario.NombreUsuario} eliminada.";
            return RedirectToAction(nameof(Index));
        }

        // ----------------- Métodos de apoyo -----------------

        private bool EsMiCuenta(Usuario usuario) =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) == usuario.IdUsuario.ToString();

        private async Task ValidarNombreAsync(string nombreUsuario, int? idActual)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                return;
            }

            var repetido = await _context.Usuarios
                .AnyAsync(u => u.NombreUsuario == nombreUsuario && u.IdUsuario != idActual);

            if (repetido)
            {
                ModelState.AddModelError("NombreUsuario", $"Ya existe una cuenta con el usuario \"{nombreUsuario}\".");
            }
        }
    }
}

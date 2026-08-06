using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionServiciosAutomotrices.API.Controllers
{
    /// <summary>
    /// Inicio y cierre de sesión (/Cuenta/Login y /Cuenta/Logout).
    /// </summary>
    [AllowAnonymous]
    public class CuentaController : Controller
    {
        private readonly ServicioUsuarios _usuarios;
        private readonly ILogger<CuentaController> _logger;

        public CuentaController(ServicioUsuarios usuarios, ILogger<CuentaController> logger)
        {
            _usuarios = usuarios;
            _logger = logger;
        }

        // GET: /Cuenta/Login
        public IActionResult Login(string? returnUrl = null)
        {
            // Si ya inició sesión, no tiene caso mostrarle el formulario.
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Tickets");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginDto());
        }

        // POST: /Cuenta/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var usuario = await _usuarios.ValidarCredencialesAsync(dto.NombreUsuario, dto.Contrasena);

            if (usuario == null)
            {
                // El mensaje es el mismo si falla el usuario o la contraseña,
                // para no revelar cuáles nombres de usuario existen.
                ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
                _logger.LogWarning("Intento de acceso fallido para {Usuario}", dto.NombreUsuario);
                return View(dto);
            }

            var identidad = ServicioUsuarios.CrearIdentidad(usuario);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                identidad,
                new AuthenticationProperties
                {
                    // Si marcó "mantener la sesión", la cookie sobrevive al
                    // cerrar el navegador; si no, se borra al cerrarlo.
                    IsPersistent = dto.Recordarme,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(dto.Recordarme ? 24 * 7 : 8),
                });

            _logger.LogInformation("Acceso correcto de {Usuario} ({Rol})", usuario.NombreUsuario, usuario.Rol);

            // Solo se acepta una dirección de retorno local, para que nadie
            // pueda usar el login como trampolín hacia un sitio externo.
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            TempData["Mensaje"] = $"Bienvenido, {usuario.NombreCompleto}.";
            return RedirectToAction("Index", "Tickets");
        }

        // POST: /Cuenta/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // GET: /Cuenta/AccesoDenegado
        public IActionResult AccesoDenegado(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
    }
}

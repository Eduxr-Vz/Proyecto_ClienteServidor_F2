using System.Security.Claims;
using GestionServiciosAutomotrices.API.DTOs;
using GestionServiciosAutomotrices.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionServiciosAutomotrices.API.Controllers.Api
{
    /// <summary>
    /// API REST: inicio y cierre de sesión.
    ///
    /// Se usa el mismo esquema de cookie que la interfaz web. Postman guarda
    /// la cookie automáticamente, así que basta con llamar una vez a
    /// POST api/cuenta/login y las peticiones siguientes ya van autenticadas.
    /// </summary>
    [ApiController]
    [Route("api/cuenta")]
    public class CuentaApiController : ControllerBase
    {
        private readonly ServicioUsuarios _usuarios;

        public CuentaApiController(ServicioUsuarios usuarios)
        {
            _usuarios = usuarios;
        }

        // POST: api/cuenta/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var usuario = await _usuarios.ValidarCredencialesAsync(dto.NombreUsuario, dto.Contrasena);

            if (usuario == null)
            {
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });
            }

            var identidad = ServicioUsuarios.CrearIdentidad(usuario);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                identidad,
                new AuthenticationProperties { IsPersistent = dto.Recordarme });

            return Ok(new
            {
                mensaje = "Sesión iniciada correctamente.",
                usuario = new UsuarioDto
                {
                    IdUsuario = usuario.IdUsuario,
                    NombreUsuario = usuario.NombreUsuario,
                    NombreCompleto = usuario.NombreCompleto,
                    Rol = usuario.Rol,
                    Activo = usuario.Activo,
                    FechaRegistro = usuario.FechaRegistro,
                    UltimoAcceso = usuario.UltimoAcceso,
                }
            });
        }

        // POST: api/cuenta/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { mensaje = "Sesión cerrada." });
        }

        // GET: api/cuenta/yo
        // Devuelve quién es el usuario de la sesión actual. Sirve para
        // comprobar desde Postman que la cookie está funcionando.
        [HttpGet("yo")]
        public IActionResult Yo() => Ok(new
        {
            nombreUsuario = User.Identity?.Name,
            nombreCompleto = User.FindFirstValue(ClaimTypes.GivenName),
            rol = User.FindFirstValue(ClaimTypes.Role),
        });
    }
}

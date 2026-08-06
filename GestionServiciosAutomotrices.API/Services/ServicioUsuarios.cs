using System.Security.Claims;
using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Services
{
    /// <summary>
    /// Maneja las contraseñas y la verificación de credenciales.
    ///
    /// El hash lo hace PasswordHasher, la misma clase que usa ASP.NET Core
    /// Identity: aplica PBKDF2 con una sal aleatoria distinta para cada
    /// usuario y miles de iteraciones. Por eso dos usuarios con la misma
    /// contraseña tienen hashes diferentes, y el proceso no se puede revertir.
    /// </summary>
    public class ServicioUsuarios
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<Usuario> _hasher = new();

        public ServicioUsuarios(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Convierte una contraseña en su hash para guardarla.</summary>
        public string CalcularHash(Usuario usuario, string contrasena) =>
            _hasher.HashPassword(usuario, contrasena);

        /// <summary>
        /// Comprueba usuario y contraseña. Devuelve el usuario si son correctos
        /// o null si no lo son (no se distingue cuál de los dos falló, para no
        /// darle pistas a quien intente adivinar).
        /// </summary>
        public async Task<Usuario?> ValidarCredencialesAsync(string nombreUsuario, string contrasena)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

            if (usuario == null || !usuario.Activo)
            {
                return null;
            }

            var resultado = _hasher.VerifyHashedPassword(usuario, usuario.ContrasenaHash, contrasena);

            if (resultado == PasswordVerificationResult.Failed)
            {
                return null;
            }

            // Si el algoritmo de hash cambió de versión, se vuelve a guardar
            // con el formato nuevo aprovechando que aquí sí tenemos la contraseña.
            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.ContrasenaHash = CalcularHash(usuario, contrasena);
            }

            usuario.UltimoAcceso = DateTime.Now;
            await _context.SaveChangesAsync();

            return usuario;
        }

        /// <summary>
        /// Arma la "credencial" que se guardará en la cookie de sesión: quién es
        /// el usuario y qué rol tiene. ASP.NET Core la lee en cada petición para
        /// saber si puede entrar a cada página.
        /// </summary>
        public static ClaimsPrincipal CrearIdentidad(Usuario usuario)
        {
            var datos = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new(ClaimTypes.Name, usuario.NombreUsuario),
                new(ClaimTypes.GivenName, usuario.NombreCompleto),
                new(ClaimTypes.Role, usuario.Rol),
            };

            var identidad = new ClaimsIdentity(datos, "Cookies");
            return new ClaimsPrincipal(identidad);
        }

        /// <summary>
        /// Crea el usuario administrador inicial si la tabla está vacía, para
        /// que siempre haya con qué entrar la primera vez.
        /// </summary>
        public async Task AsegurarAdministradorAsync()
        {
            if (await _context.Usuarios.AnyAsync())
            {
                return;
            }

            var admin = new Usuario
            {
                NombreUsuario = "admin",
                NombreCompleto = "Administrador del taller",
                Rol = RolUsuario.Administrador,
                Activo = true,
            };
            admin.ContrasenaHash = CalcularHash(admin, "Admin123!");

            _context.Usuarios.Add(admin);
            await _context.SaveChangesAsync();
        }
    }
}

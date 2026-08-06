namespace GestionServiciosAutomotrices.API.Models
{
    /// <summary>
    /// Perfiles de acceso del sistema. Se guardan como texto en la columna Rol
    /// porque así el nombre del rol es el mismo que usa [Authorize(Roles = ...)].
    /// </summary>
    public static class RolUsuario
    {
        /// <summary>Acceso total, incluida la gestión de usuarios y las bajas.</summary>
        public const string Administrador = "Administrador";

        /// <summary>Atiende el mostrador: da de alta y edita, pero no elimina.</summary>
        public const string Recepcionista = "Recepcionista";

        /// <summary>Consulta los tickets y actualiza el avance de su trabajo.</summary>
        public const string Mecanico = "Mecanico";

        public static readonly string[] Todos =
        {
            Administrador, Recepcionista, Mecanico
        };

        /// <summary>Roles que pueden dar de alta o modificar registros.</summary>
        public const string PuedenEditar = $"{Administrador},{Recepcionista}";
    }
}

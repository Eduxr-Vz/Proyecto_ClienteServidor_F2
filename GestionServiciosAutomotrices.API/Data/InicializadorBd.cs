using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace GestionServiciosAutomotrices.API.Data
{
    /// <summary>
    /// Crea la base de datos al arrancar si todavía no existe, ejecutando el
    /// script database/CreacionBD.sql.
    ///
    /// Sirve para que el proyecto funcione en cualquier equipo con solo
    /// ejecutarlo: si la base no está (por ejemplo, al clonar el repositorio o
    /// al usar una instancia de LocalDB recién creada), la aplicación la
    /// construye sola con sus tablas y datos de prueba.
    /// </summary>
    public static class InicializadorBd
    {
        public static async Task PrepararAsync(WebApplication app)
        {
            var log = app.Logger;
            var cadena = app.Configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(cadena))
            {
                log.LogWarning("No hay cadena de conexión configurada; se omite la creación de la base.");
                return;
            }

            var constructor = new SqlConnectionStringBuilder(cadena);
            var nombreBd = constructor.InitialCatalog;

            // Para preguntar si la base existe hay que conectarse a otra:
            // master siempre está disponible.
            constructor.InitialCatalog = "master";
            constructor.ConnectTimeout = Math.Max(constructor.ConnectTimeout, 30);

            try
            {
                await using var conexion = new SqlConnection(constructor.ConnectionString);
                await conexion.OpenAsync();

                await using (var comprobar = conexion.CreateCommand())
                {
                    comprobar.CommandText = "SELECT DB_ID(@nombre)";
                    comprobar.Parameters.AddWithValue("@nombre", nombreBd);

                    if (await comprobar.ExecuteScalarAsync() is not (null or DBNull))
                    {
                        log.LogInformation("La base {Base} ya existe.", nombreBd);
                        return;
                    }
                }

                var script = LocalizarScript(app.Environment.ContentRootPath);
                if (script == null)
                {
                    log.LogWarning(
                        "La base {Base} no existe y no se encontró database/CreacionBD.sql. " +
                        "Ejecuta el script manualmente antes de usar la aplicación.", nombreBd);
                    return;
                }

                log.LogInformation("La base {Base} no existe. Creándola con {Script}...", nombreBd, script);
                await EjecutarScriptAsync(conexion, await File.ReadAllTextAsync(script));
                log.LogInformation("Base {Base} creada correctamente con sus datos de prueba.", nombreBd);
            }
            catch (Exception ex)
            {
                // Si esto falla la aplicación igual arranca: así se puede ver el
                // error en pantalla en lugar de que el proceso muera al inicio.
                log.LogError(ex, "No se pudo preparar la base de datos {Base}.", nombreBd);
            }
        }

        /// <summary>
        /// Busca database/CreacionBD.sql subiendo por las carpetas padre, para
        /// que funcione tanto al ejecutar desde Visual Studio como con dotnet run.
        /// </summary>
        private static string? LocalizarScript(string rutaInicial)
        {
            var carpeta = new DirectoryInfo(rutaInicial);

            for (var i = 0; i < 5 && carpeta != null; i++)
            {
                var candidato = Path.Combine(carpeta.FullName, "database", "CreacionBD.sql");
                if (File.Exists(candidato))
                {
                    return candidato;
                }

                carpeta = carpeta.Parent;
            }

            return null;
        }

        /// <summary>
        /// Ejecuta el script por lotes. SQL Server no acepta varios lotes en una
        /// sola instrucción: hay que partir el texto por las líneas "GO", que es
        /// justo lo que hacen SSMS y sqlcmd.
        /// </summary>
        private static async Task EjecutarScriptAsync(SqlConnection conexion, string script)
        {
            var lotes = Regex.Split(script, @"^\s*GO\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            foreach (var lote in lotes)
            {
                if (string.IsNullOrWhiteSpace(lote))
                {
                    continue;
                }

                await using var comando = conexion.CreateCommand();
                comando.CommandText = lote;
                comando.CommandTimeout = 60;
                await comando.ExecuteNonQueryAsync();
            }
        }
    }
}

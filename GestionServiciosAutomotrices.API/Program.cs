using System.Globalization;
using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.Hubs;
using GestionServiciosAutomotrices.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

// Cultura fija en español de México para que los precios se muestren como
// $2,400.00 y las fechas como dd/MM/yyyy en cualquier equipo donde se ejecute.
var culturaMexico = new CultureInfo("es-MX");
CultureInfo.DefaultThreadCurrentCulture = culturaMexico;
CultureInfo.DefaultThreadCurrentUICulture = culturaMexico;

var builder = WebApplication.CreateBuilder(args);

// ----- Servicios -----

// Controladores de API + vistas MVC (interfaz web) en el mismo proyecto.
builder.Services.AddControllersWithViews(options =>
    {
        // Deja el contador de tickets pendientes listo para el menú.
        options.Filters.Add<ContadorPendientesFilter>();
    })
    .AddJsonOptions(options =>
    {
        // Los enums (como EstadoTicket) se devuelven como texto en lugar de números.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Conexión a SQL Server (la cadena está en appsettings.json).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));
// EnableRetryOnFailure reintenta las consultas cuando el servidor tarda en
// responder. Es importante con LocalDB: se apaga tras unos minutos sin uso y
// la primera consulta debe esperar a que vuelva a arrancar.

// SignalR: notificaciones en tiempo real hacia los navegadores conectados.
builder.Services.AddSignalR();

// Servicio que publica los avisos de los tickets.
// TODO (Fase 4): Para enviar los avisos a través de RabbitMQ basta con
// registrar aquí otra implementación de INotificadorTickets; los
// controladores no cambian porque dependen de la interfaz, no de SignalR.
builder.Services.AddScoped<INotificadorTickets, NotificadorSignalR>();

// Swagger para documentar y probar la API.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Gestión de Servicios Automotrices",
        Version = "v1 (Fase 3 - CRUD completo)",
        Description = "API REST para la administración de un taller mecánico. " +
                      "CRUD completo de tickets, clientes, vehículos, mecánicos y servicios."
    });
});

// TODO (Fase 3): Configurar CORS para el cliente web.
// TODO (Fase 3): Agregar autenticación con JWT y manejo de roles (admin / recepcionista / mecánico).

var app = builder.Build();

// Crea la base de datos con el script si todavía no existe, para que el
// proyecto funcione con solo ejecutarlo en cualquier equipo.
await InicializadorBd.PrepararAsync(app);

// ----- Pipeline HTTP -----

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Por ahora la API es pública; la autorización se activará cuando exista autenticación.
app.UseAuthorization();

// Rutas con atributos (API REST en /api/tickets).
app.MapControllers();

// Punto de conexión de SignalR al que se enganchan los navegadores.
app.MapHub<NotificacionesHub>("/hubs/notificaciones");

// Ruta convencional de las vistas MVC; la raíz del sitio muestra la lista de tickets.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Tickets}/{action=Index}/{id?}");

app.Run();

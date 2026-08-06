using System.Globalization;
using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.Hubs;
using GestionServiciosAutomotrices.API.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

// Cultura fija en español de México para que los precios se muestren como
// $2,400.00 y las fechas como dd/MM/yyyy en cualquier equipo donde se ejecute.
var culturaMexico = new CultureInfo("es-MX");
CultureInfo.DefaultThreadCurrentCulture = culturaMexico;
CultureInfo.DefaultThreadCurrentUICulture = culturaMexico;

// QuestPDF (generación de las órdenes de servicio en PDF) exige declarar la
// licencia. La Community es gratuita para proyectos como este.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ----- Servicios -----

// Controladores de API + vistas MVC (interfaz web) en el mismo proyecto.
builder.Services.AddControllersWithViews(options =>
    {
        // Deja el contador de tickets pendientes listo para el menú.
        options.Filters.Add<ContadorPendientesFilter>();

        // Todo el sistema exige haber iniciado sesión. Las pocas acciones
        // públicas (el propio login) se marcan con [AllowAnonymous].
        options.Filters.Add(new AuthorizeFilter(
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
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

// Autenticación por cookie: al iniciar sesión el servidor manda una cookie
// firmada con la identidad del usuario, y el navegador la reenvía en cada
// petición. Así el servidor sabe quién es sin volver a pedir la contraseña.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Cuenta/Login";
        opciones.LogoutPath = "/Cuenta/Logout";
        opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        opciones.SlidingExpiration = true;              // se renueva mientras se use
        opciones.Cookie.Name = "TallerSesion";
        opciones.Cookie.HttpOnly = true;                // JavaScript no puede leerla
        opciones.Cookie.SameSite = SameSiteMode.Lax;    // mitiga ataques CSRF

        // Las peticiones de la API deben recibir 401/403 en lugar de una
        // redirección al formulario de login (que no sabrían interpretar).
        opciones.Events.OnRedirectToLogin = contexto =>
        {
            if (contexto.Request.Path.StartsWithSegments("/api"))
            {
                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            contexto.Response.Redirect(contexto.RedirectUri);
            return Task.CompletedTask;
        };
        opciones.Events.OnRedirectToAccessDenied = contexto =>
        {
            if (contexto.Request.Path.StartsWithSegments("/api"))
            {
                contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            contexto.Response.Redirect(contexto.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<ServicioUsuarios>();

// SignalR: notificaciones en tiempo real hacia los navegadores conectados.
builder.Services.AddSignalR();

// Servicio que publica los avisos de los tickets.
// TODO (Fase 4): Para enviar los avisos a través de RabbitMQ basta con
// registrar aquí otra implementación de INotificadorEventos; los
// controladores no cambian porque dependen de la interfaz, no de SignalR.
builder.Services.AddScoped<INotificadorEventos, NotificadorSignalR>();

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

// Se asegura de que exista el administrador inicial (admin / Admin123!).
using (var alcance = app.Services.CreateScope())
{
    var servicioUsuarios = alcance.ServiceProvider.GetRequiredService<ServicioUsuarios>();
    try
    {
        await servicioUsuarios.AsegurarAdministradorAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "No se pudo crear el usuario administrador inicial.");
    }
}

// ----- Pipeline HTTP -----

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// El orden importa: primero se averigua quién es (autenticación) y
// después si puede entrar a lo que pidió (autorización).
app.UseAuthentication();
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

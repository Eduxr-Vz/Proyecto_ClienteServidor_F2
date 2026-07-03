using GestionServiciosAutomotrices.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ----- Servicios -----

// Controladores de API + vistas MVC (interfaz web) en el mismo proyecto.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Los enums (como EstadoTicket) se devuelven como texto en lugar de números.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Conexión a SQL Server (la cadena está en appsettings.json).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger para documentar y probar la API.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Gestión de Servicios Automotrices",
        Version = "v1 (Fase 2 - CRUD completo)",
        Description = "API REST para la administración de un taller mecánico. " +
                      "CRUD completo de tickets: crear, consultar, actualizar y eliminar."
    });
});

// TODO (Fase 3): Configurar CORS para el cliente web.
// TODO (Fase 3): Agregar autenticación con JWT y manejo de roles (admin / recepcionista / mecánico).

var app = builder.Build();

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

// Ruta convencional de las vistas MVC; la raíz del sitio muestra la lista de tickets.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Tickets}/{action=Index}/{id?}");

app.Run();

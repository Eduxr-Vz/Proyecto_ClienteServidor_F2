using GestionServiciosAutomotrices.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ----- Servicios -----

builder.Services.AddControllers()
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
        Version = "v1 (Fase 1 - Avance)",
        Description = "API REST para la administración de un taller mecánico. " +
                      "Proyecto universitario en desarrollo."
    });
});

// TODO (Fase 2): Configurar CORS para el cliente web.
// TODO (Fase 3): Agregar autenticación con JWT y manejo de roles (admin / recepcionista / mecánico).

var app = builder.Build();

// ----- Pipeline HTTP -----

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // La raíz del sitio redirige a Swagger para facilitar las pruebas.
    app.MapGet("/", () => Results.Redirect("/swagger"))
       .ExcludeFromDescription();
}

app.UseHttpsRedirection();

// Por ahora la API es pública; la autorización se activará cuando exista autenticación.
app.UseAuthorization();

app.MapControllers();

app.Run();

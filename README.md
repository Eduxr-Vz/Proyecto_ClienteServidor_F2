# Gestión de Servicios Automotrices - API REST + MVC

Proyecto de la materia **Clientes-Servidor**.
Sistema para administrar un taller mecánico: clientes, sus vehículos, los mecánicos, el catálogo de servicios y los tickets (órdenes de servicio).

> **Estado: Fase 3.** CRUD completo de las cinco entidades, disponible de dos formas **en un solo proyecto**: una **interfaz web MVC** (vistas Razor con Bootstrap) y una **API REST** (JSON). Ambas comparten los modelos, el DbContext y las reglas de negocio.

## Tecnologías

- ASP.NET Core (.NET 10): MVC con vistas Razor + Web API en el mismo proyecto
- Entity Framework Core 10 (SQL Server)
- SQL Server LocalDB (o SQL Server Express)
- Bootstrap 5 (interfaz web)
- Swagger (documentación y pruebas de la API)
- Postman (colección de pruebas incluida)

## Estructura del proyecto

```
Proyecto_ClientesServidor/
├── GestionServiciosAutomotrices.sln
├── GestionServiciosAutomotrices.API/
│   ├── Controllers/                  # Controladores MVC (vistas web)
│   │   ├── TicketsController.cs
│   │   ├── ClientesController.cs
│   │   ├── VehiculosController.cs
│   │   ├── MecanicosController.cs
│   │   ├── ServiciosController.cs
│   │   └── Api/                      # Controladores de la API REST
│   │       ├── TicketsApiController.cs
│   │       ├── ClientesApiController.cs
│   │       ├── VehiculosApiController.cs
│   │       ├── MecanicosApiController.cs
│   │       └── ServiciosApiController.cs
│   ├── Views/
│   │   ├── Tickets/                  # Index, Details, Create, Edit, Delete
│   │   ├── Clientes/
│   │   ├── Vehiculos/
│   │   ├── Mecanicos/
│   │   ├── Servicios/
│   │   └── Shared/                   # _Layout y parciales reutilizables
│   ├── Models/                       # Entidades del dominio
│   │   ├── Cliente.cs
│   │   ├── Vehiculo.cs
│   │   ├── Mecanico.cs
│   │   ├── Servicio.cs
│   │   ├── Ticket.cs
│   │   ├── TicketServicio.cs         # Tabla intermedia Ticket-Servicio
│   │   └── EstadoTicket.cs           # Enum de estados
│   ├── DTOs/                         # Objetos de entrada y salida
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── TicketReglas.cs           # Reglas de negocio compartidas
│   ├── Program.cs
│   └── appsettings.json              # Cadena de conexión
├── database/
│   └── CreacionBD.sql                # Script de creación + datos de prueba
├── postman/
│   └── GestionServiciosAutomotrices.postman_collection.json
└── README.md
```

## Cómo ejecutar el proyecto

1. **Crear la base de datos**. Con SQL Server Management Studio, abrir `database/CreacionBD.sql` y ejecutarlo completo. Desde la terminal también funciona:
   ```
   sqlcmd -S "(localdb)\MSSQLLocalDB" -i database\CreacionBD.sql
   ```
   Crea la BD `GestionServiciosAutomotricesDB` con datos de prueba.

2. **Revisar la cadena de conexión** en `GestionServiciosAutomotrices.API/appsettings.json`. Por defecto usa LocalDB:
   ```
   Server=(localdb)\MSSQLLocalDB;Database=GestionServiciosAutomotricesDB;Trusted_Connection=True;TrustServerCertificate=True;
   ```
   Para una instancia con nombre, cambiar `Server` (por ejemplo `localhost\SQLEXPRESS` o el nombre del equipo).

3. **Ejecutar la aplicación**:
   ```
   cd GestionServiciosAutomotrices.API
   dotnet run --launch-profile https
   ```

4. Abrir en el navegador:
   - **Interfaz web (MVC):** https://localhost:7122
   - **API (Swagger):** https://localhost:7122/swagger

## Interfaz web (MVC)

| Sección      | Rutas                                                      |
|--------------|------------------------------------------------------------|
| Tickets      | `/Tickets` con filtros por estado y mecánico + paginación   |
| Clientes     | `/Clientes` con buscador por nombre, apellidos o teléfono   |
| Vehículos    | `/Vehiculos` con buscador por marca, modelo o placas        |
| Mecánicos    | `/Mecanicos` con filtro de activos                          |
| Servicios    | `/Servicios` (catálogo) con filtro de activos               |

Cada sección tiene sus cinco vistas: lista (Index), detalle (Details), alta (Create), edición (Edit) y confirmación de baja (Delete). Los formularios validan con DataAnnotations —las mismas reglas que usa la API— y muestran los errores junto a cada campo.

## Endpoints de la API REST

**Tickets** — `api/tickets`

| Método | Ruta                      | Descripción                                              | Respuestas    |
|--------|---------------------------|----------------------------------------------------------|---------------|
| GET    | /api/tickets              | Lista con filtros `?estado=&idMecanico=` y paginación `?pagina=&porPagina=` | 200 |
| GET    | /api/tickets/{id}         | Consulta un ticket                                       | 200, 404      |
| POST   | /api/tickets              | Crea con folio consecutivo, servicios y total calculado  | 201, 400      |
| PUT    | /api/tickets/{id}         | Actualiza datos y **los servicios del ticket**           | 200, 400, 404 |
| PATCH  | /api/tickets/{id}/estado  | Cambia únicamente el estado                              | 200, 400, 404 |
| DELETE | /api/tickets/{id}         | Elimina el ticket y sus servicios                        | 204, 400, 404 |

**Catálogos** — todos siguen el mismo patrón REST (GET lista, GET por id, POST, PUT, DELETE):

| Recurso    | Ruta base        | Filtros disponibles     |
|------------|------------------|-------------------------|
| Clientes   | `api/clientes`   | —                       |
| Vehículos  | `api/vehiculos`  | `?idCliente=`           |
| Mecánicos  | `api/mecanicos`  | `?soloActivos=true`     |
| Servicios  | `api/servicios`  | `?soloActivos=true`     |

## Reglas de negocio implementadas

**Tickets**
- El **folio** se genera automáticamente como consecutivo del año (`TKT-2026-0003`), tomando el número más alto registrado para no duplicar aunque se eliminen tickets.
- El **total** se calcula con el precio vigente de los servicios y queda registrado en `TicketServicios.PrecioAplicado`; al editar los servicios se recalcula, y los que ya estaban conservan su precio original.
- Un **vehículo no puede tener dos tickets sin cerrar** al mismo tiempo.
- **Entregado** y **Cancelado** son estados finales: un ticket en esos estados ya no cambia de estado.
- Al pasar a **Entregado** se registra automáticamente la `FechaEntrega`.
- Un ticket **Entregado no puede eliminarse** (forma parte del historial del taller).

**Catálogos**
- No se elimina un **cliente** que tiene vehículos registrados.
- No se elimina un **vehículo** que tiene tickets en el historial.
- Las **placas** son únicas; el **VIN** debe tener 17 caracteres sin las letras I, O ni Q.
- Un **mecánico** con tickets sin entregar no puede darse de baja; si ya tiene historial se hace **baja lógica** (`Activo = false`) en lugar de borrarlo.
- Un **servicio** ya aplicado en tickets se **desactiva** en lugar de borrarse, para no romper el historial.

## Pruebas con Postman

Importar `postman/GestionServiciosAutomotrices.postman_collection.json` (botón *Import*). La colección está organizada en 5 carpetas —Tickets, Clientes, Vehículos, Mecánicos y Servicios— con **casos exitosos y casos de error** de cada endpoint: creaciones (201), consultas (200), eliminaciones (204), validaciones de datos y de reglas de negocio (400) e ids inexistentes (404).

La variable `baseUrl` apunta a `https://localhost:7122`. Si Postman marca un error de certificado, desactivar *SSL certificate verification* en Settings → General.

## Pendientes para siguientes fases

- [ ] Autenticación con JWT y roles (admin / recepcionista / mecánico).
- [ ] CORS para consumir la API desde un cliente externo.
- [ ] Migraciones de EF Core en lugar del script SQL manual.
- [ ] Reportes: ingresos por periodo y productividad por mecánico.

---
*Fase 3: CRUD completo de las cinco entidades con interfaz web MVC y API REST en un solo proyecto, probado en navegador y Postman.*

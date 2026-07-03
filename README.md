# Gestión de Servicios Automotrices - API REST

Proyecto de la materia **Clientes-Servidor**.
API REST para administrar un taller mecánico: clientes, sus vehículos, los mecánicos, el catálogo de servicios y los tickets (órdenes de servicio).

> **Estado: Fase 2.** CRUD completo de tickets (Create, Read, Update, Delete) disponible de dos formas **en un solo proyecto**: una **interfaz web MVC** (vistas Razor con Bootstrap) y una **API REST** (JSON). Ambas comparten los modelos, el DbContext y las reglas de negocio.

## Tecnologías

- ASP.NET Core (.NET 10): MVC con vistas Razor + Web API en el mismo proyecto
- Entity Framework Core 10 (SQL Server)
- SQL Server Express
- Bootstrap 5 (interfaz web)
- Swagger (documentación y pruebas de la API)
- Postman (colección de pruebas incluida)

## Estructura del proyecto

```
Proyecto_ClientesServidor/
├── GestionServiciosAutomotrices.sln
├── GestionServiciosAutomotrices.API/
│   ├── Controllers/
│   │   ├── TicketsController.cs      # CRUD con vistas MVC (interfaz web)
│   │   └── Api/
│   │       └── TicketsApiController.cs  # CRUD de la API REST (api/tickets)
│   ├── Views/
│   │   ├── Tickets/                  # Index, Details, Create, Edit, Delete
│   │   └── Shared/                   # _Layout y parciales
│   ├── Models/                       # Entidades del dominio
│   │   ├── Cliente.cs
│   │   ├── Vehiculo.cs
│   │   ├── Mecanico.cs
│   │   ├── Servicio.cs
│   │   ├── Ticket.cs
│   │   ├── TicketServicio.cs         # Tabla intermedia Ticket-Servicio
│   │   └── EstadoTicket.cs           # Enum de estados
│   ├── DTOs/
│   │   ├── TicketCrearDto.cs
│   │   ├── TicketActualizarDto.cs
│   │   └── TicketDto.cs
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── TicketReglas.cs           # Reglas de negocio compartidas (folio, estados)
│   ├── Program.cs
│   └── appsettings.json              # Cadena de conexión
├── database/
│   └── CreacionBD.sql                # Script de creación + datos de prueba
├── postman/
│   └── GestionServiciosAutomotrices.postman_collection.json
└── README.md
```

## Cómo ejecutar el proyecto

1. **Crear la base de datos**: abrir `database/CreacionBD.sql` en SQL Server Management Studio y ejecutarlo completo. Crea la BD `GestionServiciosAutomotricesDB` con datos de prueba.

2. **Revisar la cadena de conexión** en `GestionServiciosAutomotrices.API/appsettings.json`. Por defecto apunta a la instancia local de SQL Server Express:
   ```
   Server=localhost\SQLEXPRESS;Database=GestionServiciosAutomotricesDB;Trusted_Connection=True;TrustServerCertificate=True;
   ```
   Si se usa otra instancia (por ejemplo `localhost` o `.\MSSQLSERVER`), cambiar el valor de `Server`.

3. **Ejecutar la API**:
   ```
   cd GestionServiciosAutomotrices.API
   dotnet run --launch-profile https
   ```

4. Abrir en el navegador:
   - **Interfaz web (MVC):** https://localhost:7122 — lista de tickets con botones para crear, editar y eliminar.
   - **API (Swagger):** https://localhost:7122/swagger

## Interfaz web (MVC)

| Ruta                  | Vista                                             |
|-----------------------|---------------------------------------------------|
| `/` o `/Tickets`      | Lista de tickets con estado, total y acciones     |
| `/Tickets/Details/5`  | Detalle: datos, servicios aplicados y total       |
| `/Tickets/Create`     | Formulario de alta (vehículo, mecánico, servicios con cálculo de total) |
| `/Tickets/Edit/5`     | Edición: estado, mecánico, fechas y observaciones |
| `/Tickets/Delete/5`   | Confirmación de eliminación                       |

Los formularios validan con DataAnnotations (las mismas reglas que la API) y las reglas de negocio compartidas viven en `Data/TicketReglas.cs`.

## Endpoints de la API REST (Fase 2 - CRUD completo)

| Método | Ruta                      | Descripción                                          | Respuestas          |
|--------|---------------------------|------------------------------------------------------|---------------------|
| GET    | /api/tickets              | Lista todos los tickets                              | 200                 |
| GET    | /api/tickets/{id}         | Consulta un ticket por id                            | 200, 404            |
| POST   | /api/tickets              | Crea un ticket con folio consecutivo; asocia servicios y calcula el total | 201, 400 |
| PUT    | /api/tickets/{id}         | Actualiza mecánico, estado, fechas, descripción y observaciones | 200, 400, 404 |
| PATCH  | /api/tickets/{id}/estado  | Cambia únicamente el estado                          | 200, 400, 404       |
| DELETE | /api/tickets/{id}         | Elimina el ticket y sus servicios asociados          | 204, 400, 404       |

### Reglas de negocio implementadas

- El **folio** se genera automáticamente como consecutivo del año (`TKT-2026-0003`), tomando el número más alto registrado para no duplicar aunque se eliminen tickets.
- El **total** se calcula al crear el ticket, sumando el precio vigente de los servicios solicitados (queda registrado en `TicketServicios.PrecioAplicado`).
- **Entregado** y **Cancelado** son estados finales: un ticket en esos estados ya no puede cambiar de estado.
- Al pasar a **Entregado** se registra automáticamente la `FechaEntrega`.
- Un ticket **Entregado no puede eliminarse** (forma parte del historial del taller).
- Validaciones con DataAnnotations en los DTOs (descripción de 10 a 500 caracteres, ids válidos) y verificación de existencia de vehículo, mecánico y servicios.

## Pruebas con Postman

Importar el archivo `postman/GestionServiciosAutomotrices.postman_collection.json` en Postman (botón *Import*). La colección incluye 11 peticiones que cubren **casos exitosos y casos de error** de todos los endpoints:

- GET de todos los tickets, por id, y por id inexistente (404).
- POST con servicios (calcula total), con validación fallida (400) y con vehículo inexistente (400).
- PUT con actualización válida (200) y con id inexistente (404).
- PATCH para cambiar solo el estado.
- DELETE con caso válido (204) y con id inexistente (404).

La variable `baseUrl` de la colección apunta a `https://localhost:7122`.

## Pendientes para las siguientes fases

- [ ] Controladores de Clientes, Vehículos, Mecánicos y Servicios.
- [ ] Filtros y paginación en el GET de tickets.
- [ ] Migraciones de EF Core en lugar del script SQL manual.
- [ ] CORS para el cliente web.
- [ ] Autenticación con JWT y roles.

---
*Fase 2: CRUD completo de tickets con interfaz web MVC y API REST en un solo proyecto, probado en navegador y Postman. Los comentarios `TODO (Fase 3)` del código indican la funcionalidad planeada para las siguientes entregas.*

# Gestión de Servicios Automotrices - API REST

Proyecto universitario para la materia de **Clientes-Servidor**.
API REST para administrar un taller mecánico: clientes, sus vehículos, los mecánicos, el catálogo de servicios y los tickets (órdenes de servicio).

> **Estado: Fase 1 (avance).** El proyecto está en desarrollo; varias funcionalidades están pendientes y marcadas con `TODO` en el código.

## Tecnologías

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core 10 (SQL Server)
- SQL Server Express
- Swagger (documentación y pruebas)
- Postman (colección de pruebas incluida)

## Estructura del proyecto

```
Proyecto_ClientesServidor/
├── GestionServiciosAutomotrices.sln
├── GestionServiciosAutomotrices.API/
│   ├── Controllers/
│   │   └── TicketsController.cs      # CRUD de tickets (parcial)
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
│   │   └── AppDbContext.cs
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

4. Abrir Swagger en: **https://localhost:7122/swagger**

## Endpoints (Fase 1)

| Método | Ruta                      | Estado                                  |
|--------|---------------------------|-----------------------------------------|
| GET    | /api/tickets              | ✅ Funcionando                          |
| GET    | /api/tickets/{id}         | ✅ Funcionando                          |
| POST   | /api/tickets              | 🟡 Parcial (no procesa servicios ni calcula total) |
| PUT    | /api/tickets/{id}         | ⛔ Pendiente (devuelve 501)             |
| PATCH  | /api/tickets/{id}/estado  | ⛔ Pendiente (devuelve 501)             |
| DELETE | /api/tickets/{id}         | ⛔ Pendiente (devuelve 501)             |

## Pruebas con Postman

Importar el archivo `postman/GestionServiciosAutomotrices.postman_collection.json` en Postman (botón *Import*). La colección incluye:

- GET de todos los tickets y por id.
- POST con caso válido, caso que falla validación (descripción corta) y caso de vehículo inexistente.
- PUT y DELETE que por ahora responden **501 Not Implemented**.

La variable `baseUrl` de la colección apunta a `https://localhost:7122`.

## Pendientes para las siguientes fases

- [ ] Completar PUT, PATCH y DELETE de tickets (con reglas de transición de estados).
- [ ] Guardar los servicios del ticket en `TicketServicios` y calcular el total.
- [ ] Generación de folio consecutivo (`TKT-2026-0001`).
- [ ] Controladores de Clientes, Vehículos, Mecánicos y Servicios.
- [ ] Migraciones de EF Core en lugar del script SQL manual.
- [ ] CORS para el cliente web.
- [ ] Autenticación con JWT y roles.

---
*Fase 1 entregada como avance del proyecto. El código contiene comentarios `TODO` que indican la funcionalidad planeada para las siguientes entregas.*

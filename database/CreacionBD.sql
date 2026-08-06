/* ============================================================
   Proyecto: Gestión de Servicios Automotrices
   Script de creación de la base de datos - FASE 3
   Motor: SQL Server (probado en LocalDB y SQL Server Express)

   Ejecutar desde la terminal:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -i CreacionBD.sql

   NOTA: En fases posteriores este script se reemplazará por
   migraciones de Entity Framework Core.
   ============================================================ */

CREATE DATABASE GestionServiciosAutomotricesDB;
GO

USE GestionServiciosAutomotricesDB;
GO

/* ----------------------- Clientes ----------------------- */
CREATE TABLE Clientes (
    IdCliente       INT IDENTITY(1,1) PRIMARY KEY,
    Nombre          NVARCHAR(100) NOT NULL,
    Apellidos       NVARCHAR(100) NOT NULL,
    Telefono        NVARCHAR(15)  NULL,
    Correo          NVARCHAR(150) NULL,
    Direccion       NVARCHAR(250) NULL,
    FechaRegistro   DATETIME2     NOT NULL DEFAULT GETDATE()
);
GO

/* ----------------------- Vehiculos ----------------------- */
CREATE TABLE Vehiculos (
    IdVehiculo   INT IDENTITY(1,1) PRIMARY KEY,
    IdCliente    INT           NOT NULL,
    Marca        NVARCHAR(50)  NOT NULL,
    Modelo       NVARCHAR(50)  NOT NULL,
    Anio         INT           NOT NULL,
    Placas       NVARCHAR(10)  NOT NULL,
    Color        NVARCHAR(30)  NULL,
    NumeroSerie  NVARCHAR(17)  NULL,

    CONSTRAINT FK_Vehiculos_Clientes
        FOREIGN KEY (IdCliente) REFERENCES Clientes(IdCliente),
    CONSTRAINT UQ_Vehiculos_Placas UNIQUE (Placas),
    CONSTRAINT CK_Vehiculos_Anio CHECK (Anio BETWEEN 1950 AND 2027)
);
GO

/* ----------------------- Mecanicos ----------------------- */
CREATE TABLE Mecanicos (
    IdMecanico    INT IDENTITY(1,1) PRIMARY KEY,
    Nombre        NVARCHAR(100) NOT NULL,
    Apellidos     NVARCHAR(100) NOT NULL,
    Especialidad  NVARCHAR(100) NULL,
    Telefono      NVARCHAR(15)  NULL,
    Activo        BIT           NOT NULL DEFAULT 1
);
GO

/* ----------------------- Servicios ----------------------- */
CREATE TABLE Servicios (
    IdServicio        INT IDENTITY(1,1) PRIMARY KEY,
    Nombre            NVARCHAR(100) NOT NULL,
    Descripcion       NVARCHAR(500) NULL,
    Precio            DECIMAL(10,2) NOT NULL DEFAULT 0,
    TiempoEstimadoMin INT           NULL,
    Activo            BIT           NOT NULL DEFAULT 1,

    CONSTRAINT CK_Servicios_Precio CHECK (Precio >= 0)
);
GO

/* ----------------------- Tickets ----------------------- */
/* Estado: 1=Abierto, 2=EnProceso, 3=Terminado, 4=Entregado, 5=Cancelado */
CREATE TABLE Tickets (
    IdTicket             INT IDENTITY(1,1) PRIMARY KEY,
    Folio                NVARCHAR(20)  NOT NULL,
    IdVehiculo           INT           NOT NULL,
    IdMecanico           INT           NULL,          -- se puede asignar después
    DescripcionProblema  NVARCHAR(500) NOT NULL,
    Estado               INT           NOT NULL DEFAULT 1,
    FechaCreacion        DATETIME2     NOT NULL DEFAULT GETDATE(),
    FechaEstimadaEntrega DATETIME2     NULL,
    FechaEntrega         DATETIME2     NULL,
    Observaciones        NVARCHAR(500) NULL,
    Total                DECIMAL(10,2) NOT NULL DEFAULT 0,

    CONSTRAINT FK_Tickets_Vehiculos
        FOREIGN KEY (IdVehiculo) REFERENCES Vehiculos(IdVehiculo),
    CONSTRAINT FK_Tickets_Mecanicos
        FOREIGN KEY (IdMecanico) REFERENCES Mecanicos(IdMecanico),
    CONSTRAINT UQ_Tickets_Folio UNIQUE (Folio),
    CONSTRAINT CK_Tickets_Estado CHECK (Estado BETWEEN 1 AND 5)
);
GO

/* ----------------------- Usuarios ----------------------- */
/* Cuentas que pueden iniciar sesión en el sistema.           */
/* La contraseña se guarda como hash (PBKDF2), nunca en claro.*/
CREATE TABLE Usuarios (
    IdUsuario       INT IDENTITY(1,1) PRIMARY KEY,
    NombreUsuario   NVARCHAR(50)  NOT NULL,
    NombreCompleto  NVARCHAR(100) NOT NULL,
    ContrasenaHash  NVARCHAR(500) NOT NULL,
    Rol             NVARCHAR(30)  NOT NULL DEFAULT N'Recepcionista',
    Activo          BIT           NOT NULL DEFAULT 1,
    FechaRegistro   DATETIME2     NOT NULL DEFAULT GETDATE(),
    UltimoAcceso    DATETIME2     NULL,

    CONSTRAINT UQ_Usuarios_NombreUsuario UNIQUE (NombreUsuario),
    CONSTRAINT CK_Usuarios_Rol CHECK (Rol IN (N'Administrador', N'Recepcionista', N'Mecanico'))
);
GO

/* ------------------- TicketServicios -------------------- */
/* Relación muchos a muchos entre Tickets y Servicios.       */
CREATE TABLE TicketServicios (
    IdTicket       INT NOT NULL,
    IdServicio     INT NOT NULL,
    PrecioAplicado DECIMAL(10,2) NOT NULL DEFAULT 0,

    CONSTRAINT PK_TicketServicios PRIMARY KEY (IdTicket, IdServicio),
    CONSTRAINT FK_TicketServicios_Tickets
        FOREIGN KEY (IdTicket) REFERENCES Tickets(IdTicket),
    CONSTRAINT FK_TicketServicios_Servicios
        FOREIGN KEY (IdServicio) REFERENCES Servicios(IdServicio)
);
GO

/* ============================================================
   Datos de prueba para poder probar los endpoints
   ============================================================ */

/* Los textos llevan el prefijo N (N'...') porque las columnas son NVARCHAR:
   así los acentos y la ñ se guardan como Unicode y no se corrompen. */

INSERT INTO Clientes (Nombre, Apellidos, Telefono, Correo, Direccion) VALUES
(N'Juan',  N'Pérez García',    N'6621234567', N'juan.perez@example.com',  N'Calle Reforma 123, Col. Centro'),
(N'María', N'López Hernández', N'6629876543', N'maria.lopez@example.com', N'Av. Universidad 456'),
(N'Carlos',N'Ramírez Soto',    N'6625551020', NULL,                       NULL);
GO

INSERT INTO Vehiculos (IdCliente, Marca, Modelo, Anio, Placas, Color, NumeroSerie) VALUES
(1, N'Nissan',     N'Versa',  2019, N'ABC-123-A', N'Rojo',   NULL),
(1, N'Toyota',     N'Hilux',  2022, N'XYZ-789-B', N'Blanco', NULL),
(2, N'Volkswagen', N'Jetta',  2017, N'JKL-456-C', N'Gris',   NULL),
(3, N'Ford',       N'Ranger', 2015, N'QWE-321-D', N'Negro',  NULL);
GO

INSERT INTO Mecanicos (Nombre, Apellidos, Especialidad, Telefono) VALUES
(N'Roberto', N'Domínguez Ríos', N'Motor y transmisión', N'6621112233'),
(N'Ana',     N'Castro Vega',    N'Sistema eléctrico',   N'6624445566'),
(N'Luis',    N'Miranda Paz',    N'Suspensión y frenos', NULL);
GO

INSERT INTO Servicios (Nombre, Descripcion, Precio, TiempoEstimadoMin) VALUES
(N'Cambio de aceite',      N'Incluye filtro y hasta 5 litros de aceite sintético', 850.00, 45),
(N'Afinación mayor',       N'Bujías, filtros, limpieza de inyectores',            2500.00, 180),
(N'Frenos delanteros',     N'Cambio de balatas y rectificado de discos',          1800.00, 120),
(N'Alineación y balanceo', NULL,                                                   600.00, 60),
(N'Diagnóstico general',   N'Escaneo por computadora y revisión de 20 puntos',     400.00, 60);
GO

/* El usuario administrador inicial lo crea la propia aplicación al arrancar
   (ver Services/ServicioUsuarios.AsegurarAdministradorAsync), porque el hash
   de la contraseña debe calcularlo el mismo algoritmo que luego la verifica.
   Credenciales iniciales:  usuario "admin"  contraseña "Admin123!"          */

/* Un ticket de ejemplo ya registrado */
INSERT INTO Tickets (Folio, IdVehiculo, IdMecanico, DescripcionProblema, Estado, FechaEstimadaEntrega) VALUES
(N'TKT-2026-0001', 1, 1, N'El motor se apaga en los altos y el ventilador no enciende.', 2, DATEADD(DAY, 3, GETDATE()));
GO

/* ============================================================
   PENDIENTE (siguientes fases):
   - Procedimientos almacenados para reportes.
   - Índices adicionales según las consultas más frecuentes.
   - Tablas de usuarios y roles para la autenticación.

   NOTA: Tickets.Total lo calcula la aplicación al crear o editar
   el ticket, sumando el PrecioAplicado de sus registros en
   TicketServicios (así el histórico no cambia si el catálogo sube
   de precio).
   ============================================================ */

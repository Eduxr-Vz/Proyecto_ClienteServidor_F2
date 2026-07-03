/* ============================================================
   Proyecto: Gestión de Servicios Automotrices
   Script de creación de la base de datos - FASE 2
   Motor: SQL Server (probado en SQL Server Express)

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

INSERT INTO Clientes (Nombre, Apellidos, Telefono, Correo, Direccion) VALUES
('Juan',  'Pérez García',    '6621234567', 'juan.perez@example.com',  'Calle Reforma 123, Col. Centro'),
('María', 'López Hernández', '6629876543', 'maria.lopez@example.com', 'Av. Universidad 456'),
('Carlos','Ramírez Soto',    '6625551020', NULL,                      NULL);
GO

INSERT INTO Vehiculos (IdCliente, Marca, Modelo, Anio, Placas, Color, NumeroSerie) VALUES
(1, 'Nissan',     'Versa',  2019, 'ABC-123-A', 'Rojo',   NULL),
(1, 'Toyota',     'Hilux',  2022, 'XYZ-789-B', 'Blanco', NULL),
(2, 'Volkswagen', 'Jetta',  2017, 'JKL-456-C', 'Gris',   NULL),
(3, 'Ford',       'Ranger', 2015, 'QWE-321-D', 'Negro',  NULL);
GO

INSERT INTO Mecanicos (Nombre, Apellidos, Especialidad, Telefono) VALUES
('Roberto', 'Domínguez Ríos', 'Motor y transmisión', '6621112233'),
('Ana',     'Castro Vega',    'Sistema eléctrico',   '6624445566'),
('Luis',    'Miranda Paz',    'Suspensión y frenos', NULL);
GO

INSERT INTO Servicios (Nombre, Descripcion, Precio, TiempoEstimadoMin) VALUES
('Cambio de aceite',      'Incluye filtro y hasta 5 litros de aceite sintético', 850.00, 45),
('Afinación mayor',       'Bujías, filtros, limpieza de inyectores',            2500.00, 180),
('Frenos delanteros',     'Cambio de balatas y rectificado de discos',          1800.00, 120),
('Alineación y balanceo', NULL,                                                  600.00, 60),
('Diagnóstico general',   'Escaneo por computadora y revisión de 20 puntos',     400.00, 60);
GO

/* Un ticket de ejemplo ya registrado */
INSERT INTO Tickets (Folio, IdVehiculo, IdMecanico, DescripcionProblema, Estado, FechaEstimadaEntrega) VALUES
('TKT-2026-0001', 1, 1, 'El motor se apaga en los altos y el ventilador no enciende.', 2, DATEADD(DAY, 3, GETDATE()));
GO

/* ============================================================
   PENDIENTE (Fase 3):
   - Procedimientos almacenados para reportes.
   - Índices adicionales según las consultas más frecuentes.

   NOTA: Tickets.Total se calcula en la API al crear el ticket,
   sumando el PrecioAplicado de sus registros en TicketServicios.
   ============================================================ */

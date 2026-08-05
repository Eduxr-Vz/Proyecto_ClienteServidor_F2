using GestionServiciosAutomotrices.API.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestionServiciosAutomotrices.API.Services
{
    /// <summary>
    /// Genera la orden de servicio de un ticket en PDF, con el formato que
    /// el taller entregaría impresa al cliente.
    ///
    /// Se usa QuestPDF: el documento se describe con código C# (encabezado,
    /// contenido y pie) y la librería se encarga de paginar y dibujar.
    /// </summary>
    public static class TicketPdf
    {
        private const string AzulOscuro = "#1a3d5c";
        private const string AzulClaro = "#2d5a80";
        private const string GrisFila = "#eef3f8";
        private const string GrisTexto = "#666666";
        private const string Blanco = "#ffffff";

        /// <summary>
        /// Devuelve el PDF del ticket como arreglo de bytes, listo para
        /// enviarlo al navegador con File(...).
        /// </summary>
        public static byte[] Generar(Ticket ticket)
        {
            var documento = Document.Create(contenedor =>
            {
                contenedor.Page(pagina =>
                {
                    pagina.Size(PageSizes.Letter);
                    pagina.Margin(2, Unit.Centimetre);
                    pagina.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                    pagina.Header().Element(c => Encabezado(c, ticket));
                    pagina.Content().Element(c => Contenido(c, ticket));
                    pagina.Footer().Element(Pie);
                });
            });

            return documento.GeneratePdf();
        }

        /// <summary>
        /// Genera un PDF con el listado de tickets (reporte del taller).
        /// </summary>
        public static byte[] GenerarListado(IReadOnlyList<Ticket> tickets, string subtitulo)
        {
            var documento = Document.Create(contenedor =>
            {
                contenedor.Page(pagina =>
                {
                    pagina.Size(PageSizes.Letter.Landscape());
                    pagina.Margin(1.5f, Unit.Centimetre);
                    pagina.DefaultTextStyle(x => x.FontSize(9).FontFamily("Helvetica"));

                    pagina.Header().Element(c => EncabezadoListado(c, subtitulo, tickets.Count));
                    pagina.Content().Element(c => TablaListado(c, tickets));
                    pagina.Footer().Element(Pie);
                });
            });

            return documento.GeneratePdf();
        }

        // ----------------------- Orden de servicio -----------------------

        private static void Encabezado(IContainer contenedor, Ticket ticket)
        {
            contenedor.Column(columna =>
            {
                columna.Item().Row(fila =>
                {
                    fila.RelativeItem().Column(izquierda =>
                    {
                        izquierda.Item().Text("Taller — Gestión de Servicios Automotrices")
                            .FontSize(15).Bold().FontColor(AzulOscuro);
                        izquierda.Item().Text("Orden de servicio")
                            .FontSize(11).FontColor(AzulClaro);
                    });

                    fila.ConstantItem(160).Column(derecha =>
                    {
                        derecha.Item().AlignRight().Text(ticket.Folio)
                            .FontSize(15).Bold().FontColor(AzulOscuro);
                        derecha.Item().AlignRight().Text(EtiquetaEstado(ticket.Estado))
                            .FontSize(10).FontColor(ColorEstado(ticket.Estado));
                    });
                });

                columna.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(AzulOscuro);
                columna.Item().PaddingBottom(12);
            });
        }

        private static void Contenido(IContainer contenedor, Ticket ticket)
        {
            contenedor.Column(columna =>
            {
                columna.Spacing(14);

                // Datos del cliente y del vehículo, lado a lado.
                columna.Item().Row(fila =>
                {
                    fila.RelativeItem().Element(c => Bloque(c, "Cliente", new[]
                    {
                        ("Nombre", NombreCliente(ticket)),
                        ("Teléfono", ticket.Vehiculo?.Cliente?.Telefono ?? "—"),
                        ("Correo", ticket.Vehiculo?.Cliente?.Correo ?? "—"),
                    }));

                    fila.ConstantItem(14);

                    fila.RelativeItem().Element(c => Bloque(c, "Vehículo", new[]
                    {
                        ("Unidad", DescribirVehiculo(ticket)),
                        ("Placas", ticket.Vehiculo?.Placas ?? "—"),
                        ("Color", ticket.Vehiculo?.Color ?? "—"),
                    }));
                });

                // Datos de la orden.
                columna.Item().Element(c => Bloque(c, "Datos de la orden", new[]
                {
                    ("Fecha de ingreso", ticket.FechaCreacion.ToString("dd/MM/yyyy HH:mm")),
                    ("Entrega estimada", ticket.FechaEstimadaEntrega?.ToString("dd/MM/yyyy HH:mm") ?? "Por definir"),
                    ("Fecha de entrega", ticket.FechaEntrega?.ToString("dd/MM/yyyy HH:mm") ?? "Pendiente"),
                    ("Mecánico asignado", NombreMecanico(ticket)),
                }));

                // Problema reportado.
                columna.Item().Column(bloque =>
                {
                    bloque.Item().Text("Problema reportado por el cliente")
                        .FontSize(11).Bold().FontColor(AzulClaro);
                    bloque.Item().PaddingTop(4).Background(GrisFila).Padding(8)
                        .Text(ticket.DescripcionProblema);
                });

                // Servicios y total.
                columna.Item().Column(bloque =>
                {
                    bloque.Item().Text("Servicios aplicados")
                        .FontSize(11).Bold().FontColor(AzulClaro);
                    bloque.Item().PaddingTop(4).Element(c => TablaServicios(c, ticket));
                });

                // Observaciones del taller, solo si existen.
                if (!string.IsNullOrWhiteSpace(ticket.Observaciones))
                {
                    columna.Item().Column(bloque =>
                    {
                        bloque.Item().Text("Observaciones del taller")
                            .FontSize(11).Bold().FontColor(AzulClaro);
                        bloque.Item().PaddingTop(4).Background(GrisFila).Padding(8)
                            .Text(ticket.Observaciones);
                    });
                }

                // Firmas.
                columna.Item().PaddingTop(30).Row(fila =>
                {
                    fila.RelativeItem().Element(c => Firma(c, "Firma del cliente"));
                    fila.ConstantItem(40);
                    fila.RelativeItem().Element(c => Firma(c, "Firma del taller"));
                });
            });
        }

        private static void Bloque(IContainer contenedor, string titulo, (string, string)[] campos)
        {
            contenedor.Column(columna =>
            {
                columna.Item().Text(titulo).FontSize(11).Bold().FontColor(AzulClaro);
                columna.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                    .Column(cuerpo =>
                    {
                        foreach (var (etiqueta, valor) in campos)
                        {
                            cuerpo.Item().Row(fila =>
                            {
                                fila.ConstantItem(105).Text(etiqueta).FontColor(GrisTexto);
                                fila.RelativeItem().Text(valor);
                            });
                        }
                    });
            });
        }

        private static void TablaServicios(IContainer contenedor, Ticket ticket)
        {
            contenedor.Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.RelativeColumn(4);
                    columnas.RelativeColumn(2);
                    columnas.ConstantColumn(90);
                });

                tabla.Header(encabezado =>
                {
                    encabezado.Cell().Background(AzulOscuro).Padding(6)
                        .Text("Servicio").FontColor(Colors.White).Bold();
                    encabezado.Cell().Background(AzulOscuro).Padding(6)
                        .Text("Descripción").FontColor(Colors.White).Bold();
                    encabezado.Cell().Background(AzulOscuro).Padding(6).AlignRight()
                        .Text("Importe").FontColor(Colors.White).Bold();
                });

                if (ticket.TicketServicios.Count == 0)
                {
                    tabla.Cell().ColumnSpan(3).Padding(8).AlignCenter()
                        .Text("Aún no se han registrado servicios en esta orden.")
                        .FontColor(GrisTexto).Italic();
                }

                var alterna = false;
                foreach (var ts in ticket.TicketServicios)
                {
                    var fondo = alterna ? GrisFila : Blanco;
                    alterna = !alterna;

                    tabla.Cell().Background(fondo).Padding(6).Text(ts.Servicio?.Nombre ?? "Servicio");
                    tabla.Cell().Background(fondo).Padding(6)
                        .Text(ts.Servicio?.Descripcion ?? "—").FontColor(GrisTexto);
                    tabla.Cell().Background(fondo).Padding(6).AlignRight()
                        .Text(ts.PrecioAplicado.ToString("C"));
                }

                tabla.Cell().ColumnSpan(2).Padding(6).AlignRight().Text("Total").Bold();
                tabla.Cell().Padding(6).AlignRight()
                    .Text(ticket.Total.ToString("C")).Bold().FontSize(12).FontColor(AzulOscuro);
            });
        }

        private static void Firma(IContainer contenedor, string texto)
        {
            contenedor.Column(columna =>
            {
                columna.Item().PaddingBottom(4).LineHorizontal(0.8f).LineColor(Colors.Grey.Medium);
                columna.Item().AlignCenter().Text(texto).FontSize(9).FontColor(GrisTexto);
            });
        }

        // ----------------------- Listado -----------------------

        private static void EncabezadoListado(IContainer contenedor, string subtitulo, int total)
        {
            contenedor.Column(columna =>
            {
                columna.Item().Text("Taller — Gestión de Servicios Automotrices")
                    .FontSize(14).Bold().FontColor(AzulOscuro);
                columna.Item().Text($"Reporte de tickets · {subtitulo} · {total} registro(s)")
                    .FontSize(10).FontColor(AzulClaro);
                columna.Item().Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(9).FontColor(GrisTexto);
                columna.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(AzulOscuro);
                columna.Item().PaddingBottom(10);
            });
        }

        private static void TablaListado(IContainer contenedor, IReadOnlyList<Ticket> tickets)
        {
            contenedor.Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.ConstantColumn(85);   // folio
                    columnas.RelativeColumn(3);    // vehículo
                    columnas.RelativeColumn(3);    // cliente
                    columnas.RelativeColumn(3);    // mecánico
                    columnas.ConstantColumn(70);   // estado
                    columnas.ConstantColumn(70);   // total
                    columnas.ConstantColumn(65);   // fecha
                });

                tabla.Header(encabezado =>
                {
                    foreach (var (titulo, derecha) in new[]
                    {
                        ("Folio", false), ("Vehículo", false), ("Cliente", false),
                        ("Mecánico", false), ("Estado", false), ("Total", true), ("Creado", false),
                    })
                    {
                        var celda = encabezado.Cell().Background(AzulOscuro).Padding(5);
                        (derecha ? celda.AlignRight() : celda).Text(titulo).FontColor(Colors.White).Bold();
                    }
                });

                if (tickets.Count == 0)
                {
                    tabla.Cell().ColumnSpan(7).Padding(10).AlignCenter()
                        .Text("No hay tickets que coincidan con los filtros.")
                        .FontColor(GrisTexto).Italic();
                }

                var alterna = false;
                foreach (var t in tickets)
                {
                    var fondo = alterna ? GrisFila : Blanco;
                    alterna = !alterna;

                    tabla.Cell().Background(fondo).Padding(5).Text(t.Folio).Bold();
                    tabla.Cell().Background(fondo).Padding(5).Text(DescribirVehiculo(t));
                    tabla.Cell().Background(fondo).Padding(5).Text(NombreCliente(t));
                    tabla.Cell().Background(fondo).Padding(5).Text(NombreMecanico(t));
                    tabla.Cell().Background(fondo).Padding(5)
                        .Text(EtiquetaEstado(t.Estado)).FontColor(ColorEstado(t.Estado));
                    tabla.Cell().Background(fondo).Padding(5).AlignRight().Text(t.Total.ToString("C"));
                    tabla.Cell().Background(fondo).Padding(5).Text(t.FechaCreacion.ToString("dd/MM/yy"));
                }

                var totalGeneral = tickets.Sum(t => t.Total);
                tabla.Cell().ColumnSpan(5).Padding(6).AlignRight().Text("Total general").Bold();
                tabla.Cell().Padding(6).AlignRight()
                    .Text(totalGeneral.ToString("C")).Bold().FontColor(AzulOscuro);
                tabla.Cell().Padding(6).Text("");
            });
        }

        // ----------------------- Apoyo -----------------------

        private static void Pie(IContainer contenedor)
        {
            contenedor.Column(columna =>
            {
                columna.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                columna.Item().Row(fila =>
                {
                    fila.RelativeItem().Text("Documento generado por el sistema de gestión del taller.")
                        .FontSize(8).FontColor(GrisTexto);
                    fila.RelativeItem().AlignRight().Text(texto =>
                    {
                        texto.DefaultTextStyle(x => x.FontSize(8).FontColor(GrisTexto));
                        texto.Span("Página ");
                        texto.CurrentPageNumber();
                        texto.Span(" de ");
                        texto.TotalPages();
                    });
                });
            });
        }

        private static string DescribirVehiculo(Ticket t) =>
            t.Vehiculo != null
                ? $"{t.Vehiculo.Marca} {t.Vehiculo.Modelo} {t.Vehiculo.Anio}"
                : "—";

        private static string NombreCliente(Ticket t) =>
            t.Vehiculo?.Cliente != null
                ? $"{t.Vehiculo.Cliente.Nombre} {t.Vehiculo.Cliente.Apellidos}"
                : "—";

        private static string NombreMecanico(Ticket t) =>
            t.Mecanico != null ? $"{t.Mecanico.Nombre} {t.Mecanico.Apellidos}" : "Sin asignar";

        private static string EtiquetaEstado(EstadoTicket estado) => estado switch
        {
            EstadoTicket.EnProceso => "En proceso",
            _ => estado.ToString(),
        };

        private static string ColorEstado(EstadoTicket estado) => estado switch
        {
            EstadoTicket.Abierto => "#185fa5",
            EstadoTicket.EnProceso => "#854f0b",
            EstadoTicket.Terminado => "#0f6e56",
            EstadoTicket.Entregado => "#3b6d11",
            EstadoTicket.Cancelado => "#5f5e5a",
            _ => "#000000",
        };

        /// <summary>
        /// Nombre sugerido para el archivo que descarga el navegador.
        /// </summary>
        public static string NombreArchivo(Ticket ticket) =>
            $"OrdenServicio_{ticket.Folio}.pdf";
    }
}

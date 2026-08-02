using GestionServiciosAutomotrices.API.Data;
using GestionServiciosAutomotrices.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Services
{
    /// <summary>
    /// Llena ViewBag.TicketsPendientes antes de mostrar cualquier vista, para
    /// que el contador del menú tenga su valor inicial al cargar la página.
    /// A partir de ahí, SignalR lo mantiene actualizado sin recargar.
    ///
    /// Se aplica solo a los controladores MVC; los de la API no devuelven vistas.
    /// </summary>
    public class ContadorPendientesFilter : IAsyncActionFilter
    {
        private readonly AppDbContext _context;

        public ContadorPendientesFilter(AppDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.Controller is Controller controlador)
            {
                controlador.ViewBag.TicketsPendientes = await _context.Tickets
                    .CountAsync(t => t.Estado != EstadoTicket.Entregado &&
                                     t.Estado != EstadoTicket.Cancelado);
            }

            await next();
        }
    }
}

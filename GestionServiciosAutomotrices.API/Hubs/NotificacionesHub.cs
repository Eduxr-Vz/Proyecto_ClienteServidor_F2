using Microsoft.AspNetCore.SignalR;

namespace GestionServiciosAutomotrices.API.Hubs
{
    /// <summary>
    /// Hub de SignalR para las notificaciones en tiempo real del taller.
    ///
    /// Un hub es el punto de conexión permanente entre el servidor y los
    /// navegadores: a diferencia de una petición HTTP normal (que termina en
    /// cuanto se responde), esta conexión queda abierta y permite que el
    /// servidor envíe datos al cliente cuando algo sucede, sin que el usuario
    /// recargue la página.
    ///
    /// Los clientes se conectan a la ruta /hubs/notificaciones (ver Program.cs)
    /// y escuchan el evento "RecibirNotificacion".
    /// </summary>
    public class NotificacionesHub : Hub
    {
        // Nombre del método que escuchan los navegadores. Se define como
        // constante para no repetir el texto en el servidor.
        public const string EventoNotificacion = "RecibirNotificacion";

        /// <summary>
        /// Grupo al que se suscriben todos los navegadores que tienen abierta
        /// la aplicación. Permite enviar avisos "a todo el taller".
        /// </summary>
        public const string GrupoTaller = "taller";

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoTaller);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoTaller);
            await base.OnDisconnectedAsync(exception);
        }

        // TODO (Fase 4): Cuando exista autenticación, crear grupos por rol
        // (recepción / mecánicos) para enviar avisos dirigidos.
    }
}

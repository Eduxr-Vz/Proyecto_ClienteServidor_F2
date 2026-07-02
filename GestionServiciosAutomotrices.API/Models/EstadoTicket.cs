namespace GestionServiciosAutomotrices.API.Models
{
    /// <summary>
    /// Estados posibles de un ticket de servicio.
    /// Pendiente: revisar con el taller si se necesita un estado "En espera de refacciones".
    /// </summary>
    public enum EstadoTicket
    {
        Abierto = 1,
        EnProceso = 2,
        Terminado = 3,
        Entregado = 4,
        Cancelado = 5
    }
}

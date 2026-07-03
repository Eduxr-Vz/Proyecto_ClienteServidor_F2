using GestionServiciosAutomotrices.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionServiciosAutomotrices.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<Mecanico> Mecanicos { get; set; }
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketServicio> TicketServicios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Llave compuesta de la tabla intermedia Ticket-Servicio.
            modelBuilder.Entity<TicketServicio>()
                .HasKey(ts => new { ts.IdTicket, ts.IdServicio });

            // Llaves foráneas explícitas de la tabla intermedia; sin esto EF
            // genera columnas sombra (TicketIdTicket) que no existen en la BD.
            modelBuilder.Entity<TicketServicio>()
                .HasOne(ts => ts.Ticket)
                .WithMany(t => t.TicketServicios)
                .HasForeignKey(ts => ts.IdTicket);

            modelBuilder.Entity<TicketServicio>()
                .HasOne(ts => ts.Servicio)
                .WithMany(s => s.TicketServicios)
                .HasForeignKey(ts => ts.IdServicio);

            // Las placas no se pueden repetir.
            modelBuilder.Entity<Vehiculo>()
                .HasIndex(v => v.Placas)
                .IsUnique();

            // El folio del ticket tampoco se puede repetir.
            modelBuilder.Entity<Ticket>()
                .HasIndex(t => t.Folio)
                .IsUnique();

            // Evitar borrado en cascada: no queremos perder tickets
            // si se elimina un vehículo o un mecánico por error.
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Vehiculo)
                .WithMany(v => v.Tickets)
                .HasForeignKey(t => t.IdVehiculo)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Mecanico)
                .WithMany(m => m.Tickets)
                .HasForeignKey(t => t.IdMecanico)
                .OnDelete(DeleteBehavior.Restrict);

            // TODO (Fase 3): Revisar si conviene borrado lógico en Clientes y Vehiculos.
        }
    }
}

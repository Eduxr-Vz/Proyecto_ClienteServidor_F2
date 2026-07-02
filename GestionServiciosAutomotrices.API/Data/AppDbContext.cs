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

            // TODO (Fase 2): Agregar datos semilla (HasData) para el catálogo de servicios.
            // TODO (Fase 2): Revisar si conviene borrado lógico en Clientes y Vehiculos.
        }
    }
}

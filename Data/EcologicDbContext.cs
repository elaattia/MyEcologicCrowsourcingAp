using Microsoft.EntityFrameworkCore;
using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.Data
{
    public class EcologicDbContext : DbContext
    {
        public EcologicDbContext(DbContextOptions<EcologicDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Organisation> Organisations { get; set; }
        public DbSet<PointDechet> PointDechets { get; set; }
        public DbSet<Itineraire> Itineraires { get; set; }
        public DbSet<Vehicule> Vehicules { get; set; }
        public DbSet<OptimisationRequest> OptimisationRequests { get; set; }
        public DbSet<OptimisationResult> OptimisationResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<PointDechet>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<Organisation>()
                .HasOne(o => o.Vehicule)
                .WithMany()
                .HasForeignKey(o => o.VehiculeId);

            modelBuilder.Entity<Itineraire>()
                .HasOne(r => r.Organisation)
                .WithMany(o => o.Itineraires)
                .HasForeignKey(r => r.OrganisationId);

            modelBuilder.Entity<OptimisationResult>()
                .HasOne(or => or.OptimisationRequest)
                .WithOne()
                .HasForeignKey<OptimisationResult>(or => or.OptimisationRequestId);
        }
    }
}

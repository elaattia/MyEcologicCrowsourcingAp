using Microsoft.EntityFrameworkCore;
using MyEcologicCrowsourcingApp.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
        public DbSet<Depot> Depots { get; set; }
        public DbSet<OptimisationRequest> OptimisationRequests { get; set; }
        public DbSet<OptimisationResult> OptimisationResults { get; set; }
        public DbSet<RecommandationEcologique> RecommandationsEcologiques { get; set; }

        public DbSet<ForumCategory> ForumCategories { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<PostReaction> PostReactions { get; set; }
        public DbSet<CommentReaction> CommentReactions { get; set; }
        public DbSet<PostReport> PostReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Organisation)
                .WithMany(o => o.Users)
                .HasForeignKey(u => u.OrganisationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PointDechet>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PointDechet>()
                .HasOne(p => p.NettoyeParOrganisation)
                .WithMany()
                .HasForeignKey(p => p.NettoyeParOrganisationId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            modelBuilder.Entity<Vehicule>()
                .HasOne(v => v.Organisation)
                .WithMany(o => o.Vehicules)
                .HasForeignKey(v => v.OrganisationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Depot>()
                .HasOne(d => d.Organisation)
                .WithMany(o => o.Depots)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Itineraire>()
                .HasOne(i => i.Organisation)
                .WithMany(o => o.Itineraires)
                .HasForeignKey(i => i.OrganisationId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<OptimisationResult>()
                .HasOne(or => or.OptimisationRequest)
                .WithOne()
                .HasForeignKey<OptimisationResult>(or => or.OptimisationRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Organisation>()
                .HasOne(o => o.Representant)
                .WithMany()
                .HasForeignKey(o => o.RepresentantId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<PointDechet>()
                .HasIndex(p => p.Statut);

            modelBuilder.Entity<PointDechet>()
                .HasIndex(p => p.Zone);

            modelBuilder.Entity<PointDechet>()
                .HasIndex(p => new { p.Statut, p.Zone });

            modelBuilder.Entity<Vehicule>()
                .HasIndex(v => new { v.OrganisationId, v.EstDisponible });

            modelBuilder.Entity<Depot>()
                .HasIndex(d => new { d.OrganisationId, d.EstActif });

            modelBuilder.Entity<Itineraire>()
                .HasIndex(i => new { i.OrganisationId, i.Statut });

            modelBuilder.Entity<Itineraire>()
                .HasIndex(i => i.DateCreation);

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(256)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.Username)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Organisation>()
                .Property(o => o.Nom)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<Vehicule>()
                .Property(v => v.Immatriculation)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Depot>()
                .Property(d => d.Nom)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<Depot>()
                .Property(d => d.Adresse)
                .HasMaxLength(500);

            modelBuilder.Entity<PointDechet>()
                .Property(p => p.Zone)
                .HasMaxLength(200);

            modelBuilder.Entity<PointDechet>()
                .Property(p => p.Pays)
                .HasMaxLength(100);

            modelBuilder.Entity<PointDechet>()
                .Property(p => p.Latitude)
                .HasPrecision(10, 7);

            modelBuilder.Entity<PointDechet>()
                .Property(p => p.Longitude)
                .HasPrecision(10, 7);

            modelBuilder.Entity<Depot>()
                .Property(d => d.Latitude)
                .HasPrecision(10, 7);

            modelBuilder.Entity<Depot>()
                .Property(d => d.Longitude)
                .HasPrecision(10, 7);


            modelBuilder.Entity<RecommandationEcologique>()
                .HasOne(r => r.PointDechet)
                .WithMany(p => p.Recommandations)
                .HasForeignKey(r => r.PointDechetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecommandationEcologique>()
                .HasIndex(r => r.PointDechetId);

            modelBuilder.Entity<RecommandationEcologique>()
                .HasIndex(r => r.DateGeneration);
            

            modelBuilder.Entity<ForumCategory>()
                .Property(fc => fc.Name)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<ForumCategory>()
                .Property(fc => fc.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<ForumCategory>()
                .HasIndex(fc => fc.Slug)
                .IsUnique();

            modelBuilder.Entity<Post>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Post>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Posts)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Post>()
                .Property(p => p.Title)
                .HasMaxLength(300)
                .IsRequired();

            modelBuilder.Entity<Post>()
                .HasIndex(p => p.CategoryId);

            modelBuilder.Entity<Post>()
                .HasIndex(p => p.CreatedAt);

            modelBuilder.Entity<Post>()
                .HasIndex(p => p.IsPinned);

            modelBuilder.Entity<Post>()
                .HasIndex(p => new { p.CategoryId, p.IsPinned, p.CreatedAt });

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasIndex(c => c.PostId);

            modelBuilder.Entity<Comment>()
                .HasIndex(c => c.ParentCommentId);

            modelBuilder.Entity<Comment>()
                .HasIndex(c => c.CreatedAt);

            modelBuilder.Entity<PostReaction>()
                .HasOne(pr => pr.Post)
                .WithMany(p => p.Reactions)
                .HasForeignKey(pr => pr.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostReaction>()
                .HasOne(pr => pr.User)
                .WithMany()
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostReaction>()
                .HasIndex(pr => new { pr.PostId, pr.UserId })
                .IsUnique();

            modelBuilder.Entity<CommentReaction>()
                .HasOne(cr => cr.Comment)
                .WithMany(c => c.Reactions)
                .HasForeignKey(cr => cr.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommentReaction>()
                .HasOne(cr => cr.User)
                .WithMany()
                .HasForeignKey(cr => cr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommentReaction>()
                .HasIndex(cr => new { cr.CommentId, cr.UserId })
                .IsUnique();

            modelBuilder.Entity<PostReport>()
                .HasOne(pr => pr.Post)
                .WithMany(p => p.Reports)
                .HasForeignKey(pr => pr.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostReport>()
                .HasOne(pr => pr.ReportedBy)
                .WithMany()
                .HasForeignKey(pr => pr.ReportedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PostReport>()
                .HasOne(pr => pr.ReviewedBy)
                .WithMany()
                .HasForeignKey(pr => pr.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PostReport>()
                .HasIndex(pr => pr.Status);

            modelBuilder.Entity<PostReport>()
                .HasIndex(pr => pr.CreatedAt);


            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime()) : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                        property.SetValueConverter(dateTimeConverter);

                    if (property.ClrType == typeof(DateTime?))
                        property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }
}

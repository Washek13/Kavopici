using Kavopici.Models;
using Kavopici.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kavopici.Data;

public class KavopiciDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<CoffeeBlend> CoffeeBlends => Set<CoffeeBlend>();
    public DbSet<TastingSession> TastingSessions => Set<TastingSession>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<TastingNote> TastingNotes => Set<TastingNote>();
    public DbSet<RatingTastingNote> RatingTastingNotes => Set<RatingTastingNote>();

    public KavopiciDbContext(DbContextOptions<KavopiciDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(u => u.Name).IsUnique();
        });

        modelBuilder.Entity<CoffeeBlend>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Name).IsRequired().HasMaxLength(200);
            e.Property(b => b.Roaster).IsRequired().HasMaxLength(200);
            e.Property(b => b.Origin).HasMaxLength(200);
            e.Property(b => b.RoastLevel).HasConversion<int>();
            e.HasOne(b => b.Supplier)
                .WithMany(u => u.SuppliedBlends)
                .HasForeignKey(b => b.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TastingSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasOne(s => s.Blend)
                .WithMany(b => b.Sessions)
                .HasForeignKey(s => s.BlendId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Rating>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Stars).IsRequired();
            e.ToTable(t => t.HasCheckConstraint("CK_Rating_Stars", "[Stars] >= 1 AND [Stars] <= 5"));
            e.HasIndex(r => new { r.UserId, r.SessionId }).IsUnique();
            e.HasOne(r => r.Blend)
                .WithMany(b => b.Ratings)
                .HasForeignKey(r => r.BlendId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Session)
                .WithMany(s => s.Ratings)
                .HasForeignKey(r => r.SessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TastingNote>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Name).IsRequired().HasMaxLength(50);
            e.HasIndex(n => n.Name).IsUnique();

            // Seed default tasting notes
            e.HasData(
                new TastingNote { Id = 1, Name = "Ovocná" },
                new TastingNote { Id = 2, Name = "Ořechová" },
                new TastingNote { Id = 3, Name = "Čokoládová" },
                new TastingNote { Id = 4, Name = "Karamelová" },
                new TastingNote { Id = 5, Name = "Květinová" },
                new TastingNote { Id = 6, Name = "Kořeněná" },
                new TastingNote { Id = 7, Name = "Citrusová" },
                new TastingNote { Id = 8, Name = "Medová" }
            );
        });

        modelBuilder.Entity<RatingTastingNote>(e =>
        {
            e.HasKey(rtn => new { rtn.RatingId, rtn.TastingNoteId });
            e.HasOne(rtn => rtn.Rating)
                .WithMany(r => r.RatingTastingNotes)
                .HasForeignKey(rtn => rtn.RatingId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(rtn => rtn.TastingNote)
                .WithMany(n => n.RatingTastingNotes)
                .HasForeignKey(rtn => rtn.TastingNoteId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

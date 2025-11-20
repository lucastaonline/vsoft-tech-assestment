using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VSoftTechAssestment.Api.Models.Entities;

namespace VSoftTechAssestment.Api.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Models.Entities.Task> Tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Task entity
        builder.Entity<Models.Entities.Task>(entity =>
        {
            entity.ToTable("Tasks");
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Status)
                .HasConversion<int>();

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(1000000);

            // Convert DateTime to UTC automatically for PostgreSQL timestamp with time zone
            // This ensures all DateTime values are stored as UTC, regardless of their Kind
            entity.Property(e => e.DueDate)
                .HasConversion(
                    v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            entity.Property(e => e.CreatedAt)
                .HasConversion(
                    v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            entity.Property(e => e.UpdatedAt)
                .HasConversion<DateTime?>(
                    v => v.HasValue 
                        ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime())
                        : (DateTime?)null,
                    v => v.HasValue 
                        ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                        : (DateTime?)null);
        });
    }
}


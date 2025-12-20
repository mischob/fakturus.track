using Fakturus.Track.Backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Backend.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<WorkSession> WorkSessions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<VacationDay> VacationDays { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure WorkSession entity
        modelBuilder.Entity<WorkSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Date });
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.CalendarEventId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(450);
            entity.Property(e => e.CalendarUrl).HasMaxLength(2048);
            entity.Property(e => e.VacationDaysPerYear).HasDefaultValue(30);
            entity.Property(e => e.WorkHoursPerWeek).HasDefaultValue(40).HasPrecision(5, 2);
            entity.Property(e => e.WorkDays).HasDefaultValue(31); // Mo-Fr (0b0011111)
            entity.Property(e => e.Bundesland).HasMaxLength(2).HasDefaultValue("NW");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure VacationDay entity
        modelBuilder.Entity<VacationDay>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Date);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
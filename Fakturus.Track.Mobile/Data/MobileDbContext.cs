using Fakturus.Track.Mobile.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;

namespace Fakturus.Track.Mobile.Data;

public class MobileDbContext : DbContext
{
    public DbSet<WorkSessionEntity> WorkSessions { get; set; }
    public DbSet<VacationDayEntity> VacationDays { get; set; }
    public DbSet<UserSettingsEntity> UserSettings { get; set; }
    public DbSet<SchoolHolidayPeriodEntity> SchoolHolidayPeriods { get; set; }
    public DbSet<CalendarEventEntity> CalendarEvents { get; set; }
    public DbSet<SyncQueueEntity> SyncQueue { get; set; }

    private readonly string? _databasePath;

    public MobileDbContext()
    {
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "fakturus_track.db");
        _databasePath = databasePath;
    }

    public MobileDbContext(DbContextOptions<MobileDbContext> options) : base(options)
    {
        _databasePath = null; // Will be configured via options
    }

    public MobileDbContext(string databasePath)
    {
        _databasePath = databasePath;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var databasePath = _databasePath ?? Path.Combine(FileSystem.AppDataDirectory, "fakturus_track.db");
            optionsBuilder.UseSqlite($"Data Source={databasePath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure WorkSessionEntity
        modelBuilder.Entity<WorkSessionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Date });
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.IsPendingSync);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure VacationDayEntity
        modelBuilder.Entity<VacationDayEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Date });
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.IsPendingSync);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure UserSettingsEntity
        modelBuilder.Entity<UserSettingsEntity>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.IsPendingSync);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure SchoolHolidayPeriodEntity
        modelBuilder.Entity<SchoolHolidayPeriodEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Bundesland);
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure CalendarEventEntity
        modelBuilder.Entity<CalendarEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Uid });
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure SyncQueueEntity
        modelBuilder.Entity<SyncQueueEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}

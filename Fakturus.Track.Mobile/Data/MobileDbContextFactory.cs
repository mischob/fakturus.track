using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fakturus.Track.Mobile.Data;

public class MobileDbContextFactory : IDesignTimeDbContextFactory<MobileDbContext>
{
    public MobileDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MobileDbContext>();

        // For design-time, use a temporary database path
        var tempPath = Path.Combine(Path.GetTempPath(), "fakturus_track_design.db");
        optionsBuilder.UseSqlite($"Data Source={tempPath}");

        return new MobileDbContext(optionsBuilder.Options);
    }
}
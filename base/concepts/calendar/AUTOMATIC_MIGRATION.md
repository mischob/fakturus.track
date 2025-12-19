# Automatic Database Migration

## Overview

The Fakturus Track backend now automatically applies pending database migrations at application startup. This ensures the database schema is always up-to-date without requiring manual intervention.

## How It Works

When the backend application starts, it:

1. **Checks for pending migrations** using `context.Database.GetPendingMigrations()`
2. **Logs the migration status**:
   - If migrations are pending: Lists them and applies them
   - If no migrations are pending: Confirms database is up-to-date
3. **Applies migrations** using `context.Database.Migrate()`
4. **Handles errors gracefully**:
   - Logs any errors that occur
   - Throws exception to prevent app from starting with incorrect schema

## Benefits

### ✅ No Manual Steps Required
- Developers don't need to remember to run `dotnet ef database update`
- Deployments are simpler - just deploy and start the app
- Reduces human error in production deployments

### ✅ Consistent Database State
- Database schema is always in sync with the code
- Prevents runtime errors due to missing columns or tables
- Application won't start if migrations fail

### ✅ Better Logging
- Clear visibility into what migrations are being applied
- Easy to troubleshoot migration issues
- Audit trail of when migrations were applied

### ✅ Production-Ready
- Safe for production deployments
- Fails fast if there are migration issues
- No downtime due to schema mismatches

## Implementation

### Code Location
`Fakturus.Track.Backend/Program.cs` (lines ~250-265)

### Key Code

```csharp
// Apply pending database migrations automatically
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var dbLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var pendingMigrations = context.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            dbLogger.LogInformation("Applying {Count} pending migration(s): {Migrations}", 
                pendingMigrations.Count, 
                string.Join(", ", pendingMigrations));
            context.Database.Migrate();
            dbLogger.LogInformation("Database migrations applied successfully");
        }
        else
        {
            dbLogger.LogInformation("Database is up to date, no pending migrations");
        }
    }
    catch (Exception ex)
    {
        dbLogger.LogError(ex, "An error occurred while applying database migrations");
        throw; // Re-throw to prevent app from starting with incorrect database schema
    }
}
```

## Log Examples

### No Pending Migrations
```
[Information] Database is up to date, no pending migrations
```

### Pending Migrations Applied
```
[Information] Applying 1 pending migration(s): 20251219080540_AddUserAndCalendarSupport
[Information] Database migrations applied successfully
```

### Migration Error
```
[Error] An error occurred while applying database migrations
Npgsql.PostgresException: Connection failed...
```

## Development Workflow

### Creating a New Migration

1. Make changes to your entities or DbContext
2. Create a migration:
   ```bash
   dotnet ef migrations add YourMigrationName --project Fakturus.Track.Backend
   ```
3. Restart the backend application
4. The migration will be applied automatically

### Reverting a Migration

If you need to revert a migration:

```bash
# Remove the last migration (before it's applied)
dotnet ef migrations remove --project Fakturus.Track.Backend

# Or revert to a specific migration (after it's applied)
dotnet ef database update PreviousMigrationName --project Fakturus.Track.Backend
```

## Production Deployment

### Standard Deployment
1. Deploy the new code to production
2. Start the application
3. Migrations are applied automatically
4. Application starts with correct schema

### Zero-Downtime Deployment
For zero-downtime deployments with multiple instances:

1. Ensure migrations are backward-compatible
2. Deploy new code to all instances
3. Restart instances one at a time
4. First instance to start will apply migrations
5. Other instances will detect no pending migrations

### Rollback Strategy
If a deployment needs to be rolled back:

1. Stop the new version
2. Deploy the previous version
3. Manually revert the database migration if needed:
   ```bash
   dotnet ef database update PreviousMigrationName
   ```

## Best Practices

### ✅ DO
- Test migrations in development before deploying
- Review migration SQL before deploying to production
- Keep migrations small and focused
- Use descriptive migration names
- Monitor logs during deployment

### ❌ DON'T
- Don't make breaking changes without a migration strategy
- Don't skip testing migrations
- Don't ignore migration errors
- Don't manually modify the database schema in production

## Troubleshooting

### Application Won't Start
**Symptom:** Application crashes on startup with database error

**Solution:**
1. Check the logs for migration errors
2. Verify database connection string
3. Ensure database user has migration permissions
4. Check if migration SQL is valid for your database

### Migration Already Applied
**Symptom:** Error saying migration was already applied

**Solution:**
1. Check `__EFMigrationsHistory` table in database
2. Verify migration files match database state
3. If needed, manually update `__EFMigrationsHistory`

### Permission Denied
**Symptom:** Database user doesn't have permission to create tables

**Solution:**
1. Grant appropriate permissions to database user:
   ```sql
   GRANT CREATE ON DATABASE fakturus_track TO your_user;
   GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO your_user;
   ```

## Comparison: Before vs After

### Before (Manual Migration)
```bash
# Developer workflow
1. dotnet ef migrations add NewFeature
2. dotnet ef database update  # ← Easy to forget!
3. dotnet run

# Production deployment
1. Deploy code
2. SSH into server
3. Run dotnet ef database update  # ← Manual step
4. Restart application
```

### After (Automatic Migration)
```bash
# Developer workflow
1. dotnet ef migrations add NewFeature
2. dotnet run  # ← Migration applied automatically!

# Production deployment
1. Deploy code
2. Restart application  # ← Migration applied automatically!
```

## Security Considerations

### Database Permissions
The application database user needs permissions to:
- Create tables
- Alter tables
- Create indexes
- Insert into `__EFMigrationsHistory` table

### Production Safety
- Migrations are applied in a transaction (when supported by database)
- Application won't start if migration fails
- No partial schema updates

## Future Enhancements

Potential improvements for the future:

1. **Migration Validation**: Pre-check migrations before applying
2. **Backup Before Migration**: Automatic database backup before applying migrations
3. **Migration Timeout**: Configure timeout for long-running migrations
4. **Dry Run Mode**: Test migrations without applying them
5. **Migration Notifications**: Send alerts when migrations are applied in production

## References

- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Applying Migrations at Runtime](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
- [Production Migration Strategies](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing)

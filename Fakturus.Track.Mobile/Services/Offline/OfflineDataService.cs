using System.Linq.Expressions;
using Fakturus.Track.Mobile.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fakturus.Track.Mobile.Services.Offline;

public class OfflineDataService<T>(MobileDbContext context, ILogger<OfflineDataService<T>> logger) : IOfflineDataService<T>
    where T : class
{
    protected readonly MobileDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();
    protected readonly ILogger<OfflineDataService<T>> Logger = logger;

    public virtual async Task<T?> GetByIdAsync(object id)
    {
        Logger.LogDebug("[Database] [{EntityType}] GetByIdAsync - Id: {Id}", typeof(T).Name, id);
        try
        {
            var result = await DbSet.FindAsync(id);
            Logger.LogDebug("[Database] [{EntityType}] GetByIdAsync completed - Found: {Found}", typeof(T).Name, result != null);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Database] [{EntityType}] Error in GetByIdAsync - Id: {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task<List<T>> GetAllAsync()
    {
        Logger.LogDebug("[Database] [{EntityType}] GetAllAsync", typeof(T).Name);
        try
        {
            var result = await DbSet.ToListAsync();
            Logger.LogDebug("[Database] [{EntityType}] GetAllAsync completed - Count: {Count}", typeof(T).Name, result.Count);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Database] [{EntityType}] Error in GetAllAsync", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        Logger.LogDebug("[Database] [{EntityType}] FindAsync", typeof(T).Name);
        try
        {
            var result = await DbSet.Where(predicate).ToListAsync();
            Logger.LogDebug("[Database] [{EntityType}] FindAsync completed - Count: {Count}", typeof(T).Name, result.Count);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Database] [{EntityType}] Error in FindAsync", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        Logger.LogDebug("[Database] [{EntityType}] AddAsync", typeof(T).Name);
        try
        {
            await DbSet.AddAsync(entity);
            await Context.SaveChangesAsync();
            Logger.LogInformation("[Database] [{EntityType}] AddAsync completed successfully", typeof(T).Name);
            return entity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Database] [{EntityType}] Error in AddAsync", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        Logger.LogDebug("[Database] [{EntityType}] UpdateAsync", typeof(T).Name);
        try
        {
            DbSet.Update(entity);
            await Context.SaveChangesAsync();
            Logger.LogInformation("[Database] [{EntityType}] UpdateAsync completed successfully", typeof(T).Name);
            return entity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Database] [{EntityType}] Error in UpdateAsync", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task DeleteAsync(object id)
    {
        Logger.LogDebug("[Database] [{EntityType}] DeleteAsync by Id - Id: {Id}", typeof(T).Name, id);
        try
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                await DeleteAsync(entity);
            }
            else
            {
                Logger.LogWarning("[Database] [{EntityType}] DeleteAsync - Entity with Id {Id} not found", typeof(T).Name, id);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Database] [{EntityType}] Error in DeleteAsync by Id - Id: {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task DeleteAsync(T entity)
    {
        Logger.LogDebug("[Database] [{EntityType}] DeleteAsync", typeof(T).Name);
        try
        {
            DbSet.Remove(entity);
            await Context.SaveChangesAsync();
            Logger.LogInformation("[Database] [{EntityType}] DeleteAsync completed successfully", typeof(T).Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Database] [{EntityType}] Error in DeleteAsync", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        Logger.LogDebug("[Database] [{EntityType}] CountAsync - HasPredicate: {HasPredicate}", typeof(T).Name, predicate != null);
        try
        {
            int count;
            if (predicate == null)
                count = await DbSet.CountAsync();
            else
                count = await DbSet.CountAsync(predicate);
            
            Logger.LogDebug("[Database] [{EntityType}] CountAsync completed - Count: {Count}", typeof(T).Name, count);
            return count;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Database] [{EntityType}] Error in CountAsync", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        Logger.LogDebug("[Database] [{EntityType}] ExistsAsync", typeof(T).Name);
        try
        {
            var exists = await DbSet.AnyAsync(predicate);
            Logger.LogDebug("[Database] [{EntityType}] ExistsAsync completed - Exists: {Exists}", typeof(T).Name, exists);
            return exists;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Database] [{EntityType}] Error in ExistsAsync", typeof(T).Name);
            throw;
        }
    }
}
using System.Linq.Expressions;

namespace Fakturus.Track.Mobile.Services.Offline;

public interface IOfflineDataService<T> where T : class
{
    Task<T?> GetByIdAsync(object id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(object id);
    Task DeleteAsync(T entity);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
}

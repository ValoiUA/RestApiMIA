using System.Linq.Expressions;

namespace MyTodoApi.Repositories;

public interface IRepository<T> where T : class
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(string id);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task CreateAsync(T entity);
    Task<bool> UpdateAsync(string id, T entity);
    Task<bool> DeleteAsync(string id);
}
using System.Linq.Expressions;
using HRS.Domain.Enums;

namespace HRS.Domain.Interfaces;

public interface ICrudMongoDBRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity, string id);
    Task RemoveAsync(string id);
    Task RemoveRangeAsync(IEnumerable<string> ids);
    Task UpdateStatusByOrderIdAsync(string orderId, RentalStatus status);
}

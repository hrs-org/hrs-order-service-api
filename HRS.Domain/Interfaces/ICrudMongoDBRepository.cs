using System.Linq.Expressions;
using HRS.Domain.Enums;

namespace HRS.Domain.Interfaces;

public interface ICrudMongoDBRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity, object id);
    Task RemoveAsync(object id);
    Task RemoveRangeAsync(IEnumerable<object> ids);
    Task UpdateStatusByOrderIdAsync(object orderId, RentalStatus status);
}

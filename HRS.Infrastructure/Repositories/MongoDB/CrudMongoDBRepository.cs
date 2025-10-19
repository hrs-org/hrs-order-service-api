using System.Linq.Expressions;
using HRS.Domain.Interfaces;
using MongoDB.Driver;

namespace HRS.Infrastructure.Repositories;

public class CrudMongoDBRepository<T> : ICrudMongoDBRepository<T> where T : class
{
    protected readonly IMongoCollection<T> _collection;

    public CrudMongoDBRepository(IMongoDatabase database, string collectionName)
    {
        _collection = database.GetCollection<T>(collectionName);
    }

    public async Task<T?> GetByIdAsync(object id) =>
        await _collection.Find(Builders<T>.Filter.Eq("Id", id)).FirstOrDefaultAsync();

    public async Task<IEnumerable<T>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await _collection.Find(predicate).ToListAsync();

    public async Task AddAsync(T entity) =>
        await _collection.InsertOneAsync(entity);

    public async Task AddRangeAsync(IEnumerable<T> entities) =>
        await _collection.InsertManyAsync(entities);

    public async Task UpdateAsync(T entity, object id) =>
        await _collection.ReplaceOneAsync(Builders<T>.Filter.Eq("Id", id), entity);

    public async Task RemoveAsync(object id) =>
        await _collection.DeleteOneAsync(Builders<T>.Filter.Eq("Id", id));

    public async Task RemoveRangeAsync(IEnumerable<object> ids) =>
        await _collection.DeleteManyAsync(Builders<T>.Filter.In("Id", ids));
}

using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using MongoDB.Driver;

namespace HRS.Infrastructure.Repositories;

public class RentalOrderItemMongoDBRepository : CrudMongoDBRepository<RentalOrderItemMongoDB>, IRentalOrderItemMongoDBRepository
{
     private readonly IMongoCollection<RentalOrderItemMongoDB> _items;

    public RentalOrderItemMongoDBRepository(IMongoDatabase db) : base(db)
    {
        _items = db.GetCollection<RentalOrderItemMongoDB>("RentalOrderItemMongoDBs");
    }

    public async Task<int> GetReservedQuantityAsync(int itemId, DateTime startDate, DateTime endDate)
    {
        var filter = Builders<RentalOrderItemMongoDB>.Filter.And(
                Builders<RentalOrderItemMongoDB>.Filter.Eq(i => i.ItemId, itemId),
                Builders<RentalOrderItemMongoDB>.Filter.Or(
                    Builders<RentalOrderItemMongoDB>.Filter.Eq(i => i.RentalOrderStatus, RentalStatus.Booked),
                    Builders<RentalOrderItemMongoDB>.Filter.Eq(i => i.RentalOrderStatus, RentalStatus.Rented)
                ),
                Builders<RentalOrderItemMongoDB>.Filter.Lte(i => i.RentalOrderStartDate, endDate),
                Builders<RentalOrderItemMongoDB>.Filter.Gte(i => i.RentalOrderEndDate, startDate)
            );

            var items = await _items.Find(filter).ToListAsync();

            return items.Sum(i => i.Quantity);
    }
}


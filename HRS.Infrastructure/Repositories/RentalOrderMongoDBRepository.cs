using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace HRS.Infrastructure.Repositories;
public class RentalOrderMongoDBRepository : CrudMongoDBRepository<RentalOrderMongoDB>, IRentalOrderMongoDBRepository
{
    private readonly IMongoCollection<RentalOrderMongoDB> _itemscollection;
    private readonly IMongoClient _client;
    public RentalOrderMongoDBRepository(IMongoDatabase database, IMongoClient client) : base(database)
    {
        _client = client;
        _itemscollection = database.GetCollection<RentalOrderMongoDB>("RentalOrders");
    }
    public async Task<int> GetReservedQuantityAsync(string itemId, DateTime startDate, DateTime endDate)
    {
        var filter = Builders<RentalOrderMongoDB>.Filter.And(
            Builders<RentalOrderMongoDB>.Filter.In(i => i.Status, new[] { RentalStatus.Booked, RentalStatus.Rented }),
            Builders<RentalOrderMongoDB>.Filter.Lte(i => i.StartDate, endDate),
            Builders<RentalOrderMongoDB>.Filter.Gte(i => i.EndDate, startDate)
            );
        var orders = await _itemscollection.Find(filter).ToListAsync();

        var totalReserved = 0;

        foreach (var order in orders)
        {
            totalReserved += order.RentalOrderItems.Where(i => i.ItemId == itemId).Sum(item => item.Quantity);
            totalReserved += (from package in order.RentalOrderPackages from item in package.PackageItems.Where(i => i.ItemId == itemId) select item.QuantityPerPackageSnapshot * package.Quantity).Sum();
        }

        return totalReserved;
    }
    public async Task<IEnumerable<RentalOrderMongoDB>> GetByStatusesWithDetailsAsync(RentalStatus[] statuses)
    {
        var filter = Builders<RentalOrderMongoDB>.Filter.In(ro => ro.Status, statuses);
        return await _itemscollection.Find(filter).ToListAsync();
    }

    public async Task<IClientSessionHandle> BeginTransactionAsync()
    {
        var session = await _client.StartSessionAsync();
        session.StartTransaction();
        return session;
    }

    public async Task<RentalOrderMongoDB?> GetByStripeSessionIdAsync(string sessionId) =>
        await _itemscollection.Find(ro => ro.StripeSessionId == sessionId).FirstOrDefaultAsync();

    public async Task<IEnumerable<RentalOrderMongoDB>> GetByStatusesAndStoreId(RentalStatus[] statuses, int storeId)
    {
        var filter = Builders<RentalOrderMongoDB>.Filter.And(
            Builders<RentalOrderMongoDB>.Filter.In(ro => ro.Status, statuses),
            Builders<RentalOrderMongoDB>.Filter.Eq(ro => ro.StoreId, storeId)
        );
        return await _itemscollection.Find(filter).ToListAsync();
    }

}




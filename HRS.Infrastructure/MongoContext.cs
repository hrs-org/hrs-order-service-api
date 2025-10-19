using MongoDB.Driver;
using HRS.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace HRS.Infrastructure;

public class MongoContext
{
    private readonly IMongoDatabase _database;

    public MongoContext(IMongoClient client)
    {
        _database = client.GetDatabase("hrsdb-order");
    }

    public IMongoDatabase Database => _database;

    public IMongoCollection<RentalOrderPackageItemMongoDB> RentalOrderPackageItemMongoDBs => _database.GetCollection<RentalOrderPackageItemMongoDB>("RentalOrderPackageItems");
    public IMongoCollection<RentalOrderItemMongoDB> RentalOrderItemMongoDBs => _database.GetCollection<RentalOrderItemMongoDB>("RentalOrderItems");
    public IMongoCollection<RentalOrderPackageMongoDB> RentalOrderPackageMongoDBs => _database.GetCollection<RentalOrderPackageMongoDB>("RentalOrderPackages");
}

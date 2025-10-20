using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace HRS.Infrastructure.Repositories;

public class RentalOrderPackageItemMongoDBRepository : CrudMongoDBRepository<RentalOrderPackageItemMongoDB>, IRentalOrderPackageItemMongoDBRepository
{
    private readonly IMongoCollection<RentalOrderPackageMongoDB> _packages;

        public RentalOrderPackageItemMongoDBRepository(IMongoDatabase db) : base(db)
        {
            _packages = db.GetCollection<RentalOrderPackageMongoDB>("RentalOrderPackages");
        }

    public async Task<int> GetReservedQuantityFromPackagesAsync(int itemId, DateTime startDate, DateTime endDate)
        {
            // filter packages that contain the item and order is booked/rented during the period
            var filter = Builders<RentalOrderPackageMongoDB>.Filter.ElemMatch(
                p => p.Items,
                i => i.ItemId == itemId
            );

            var packages = await _packages.Find(filter).ToListAsync();

            int totalReserved = 0;

            foreach (var package in packages)
            {
                if ((package.RentalOrderStatus == RentalStatus.Booked || package.RentalOrderStatus == RentalStatus.Rented) &&
                    package.RentalOrderStartDate <= endDate && package.RentalOrderEndDate >= startDate)
                    {
                     totalReserved += package.Items
                     .Where(i => i.ItemId == itemId)
                        .Sum(i => i.QuantityPerPackageSnapshot * package.Quantity);
                    }
            }

            return totalReserved;
        }

  }



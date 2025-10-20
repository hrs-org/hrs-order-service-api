using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace HRS.Infrastructure.Repositories;
public class RentalOrderPackageMongoDBRepository : CrudMongoDBRepository<RentalOrderPackageMongoDB>, IRentalOrderPackageMongoDBRepository
{
    public RentalOrderPackageMongoDBRepository(IMongoDatabase database)
        : base(database)
    {
    }
}

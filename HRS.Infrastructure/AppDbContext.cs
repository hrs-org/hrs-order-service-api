using HRS.Domain.Entities;
using HRS.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace HRS.Infrastructure;

public class AppDbContext : DbContext
{


    public AppDbContext(DbContextOptions<AppDbContext> options)
    {

    }

    public DbSet<RentalOrder> RentalOrders { get; set; } = default!;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new RentalOrderConfiguration());
    }

    // public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    // {
    //     await HandleRentalOrderStatusChangesAsync();
    //     return await base.SaveChangesAsync(cancellationToken);
    // }

    // private async Task HandleRentalOrderStatusChangesAsync()
    // {
    //     var changedOrders = ChangeTracker.Entries<RentalOrder>()
    //         .Where(e => e.State == EntityState.Modified);

    //     foreach (var entry in changedOrders)
    //     {

    //         var oldStatusObj = entry.OriginalValues[nameof(RentalOrder.Status)];
    //         var newStatusObj = entry.CurrentValues[nameof(RentalOrder.Status)];

    //         if (oldStatusObj != null && newStatusObj != null)
    //         {
    //             var oldStatus = oldStatusObj.ToString();
    //             var newStatus = newStatusObj.ToString();

    //             if (oldStatus != newStatus)
    //                 {
    //                 var orderId = entry.Entity.Id.ToString();

    //             // Update MongoDB collections
    //             var itemCollection = _mongoDb.GetCollection<RentalOrderItemMongoDB>("RentalOrderItems");
    //             var packageCollection = _mongoDb.GetCollection<RentalOrderPackageMongoDB>("RentalOrderPackages");

    //             // Update RentalOrderItemMongoDB
    //             var itemFilter = Builders<RentalOrderItemMongoDB>.Filter.Eq(i => i.RentalOrderId, orderId);
    //             var itemUpdate = Builders<RentalOrderItemMongoDB>.Update.Set(i => i.RentalOrderStatus, newStatus);
    //             await itemCollection.UpdateManyAsync(itemFilter, itemUpdate);

    //             // Update RentalOrderPackageMongoDB
    //             var packageFilter = Builders<RentalOrderPackageMongoDB>.Filter.Eq(p => p.RentalOrderId, orderId);
    //             var packageUpdate = Builders<RentalOrderPackageMongoDB>.Update.Set(p => p.RentalOrderStatus, newStatus);
    //             await packageCollection.UpdateManyAsync(packageFilter, packageUpdate);
    //                 }
    //         }
    //     }



    // }
}

using HRS.Domain.Entities;
using HRS.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = default!;
    public DbSet<UserSession> UserSessions { get; set; } = default!;
    public DbSet<UserVerification> UserVerifications { get; set; } = default!;
    public DbSet<Item> Items { get; set; } = default!;

    public DbSet<ItemRate> ItemRates { get; set; } = default!;
    public DbSet<Package> Packages { get; set; } = default!;
    public DbSet<PackageItem> PackageItems { get; set; } = default!;
    public DbSet<PackageRate> PackageRates { get; set; } = default!;
    public DbSet<RentalOrder> RentalOrders { get; set; } = default!;
    public DbSet<RentalOrderItem> RentalOrderItems { get; set; } = default!;
    public DbSet<RentalOrderPackage> RentalOrderPackages { get; set; } = default!;
    public DbSet<RentalOrderPackageItem> RentalOrderPackageItems { get; set; } = default!;
    public DbSet<ItemMaintenance> ItemMaintenances { get; set; } = default!;
    public DbSet<Payment> Payments { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserSessionConfiguration());
        modelBuilder.ApplyConfiguration(new UserVerificationConfiguration());
        modelBuilder.ApplyConfiguration(new ItemConfiguration());
        modelBuilder.ApplyConfiguration(new PackageConfiguration());
        modelBuilder.ApplyConfiguration(new PackageItemConfiguration());
        modelBuilder.ApplyConfiguration(new PackageRateConfiguration());
        modelBuilder.ApplyConfiguration(new RentalOrderConfiguration());
        modelBuilder.ApplyConfiguration(new RentalOrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new RentalOrderPackageConfiguration());
        modelBuilder.ApplyConfiguration(new RentalOrderPackageItemConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
    }
}

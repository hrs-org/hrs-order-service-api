using HRS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRS.Infrastructure.Configuration;

public class RentalOrderConfiguration : IEntityTypeConfiguration<RentalOrder>
{
    public void Configure(EntityTypeBuilder<RentalOrder> builder)
    {
        builder.ToTable("RentalOrders");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedBy)
            .WithMany()
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedBy)
            .WithMany()
            .HasForeignKey(e => e.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReturnedBy)
            .WithMany()
            .HasForeignKey(e => e.ReturnedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ClosedBy)
            .WithMany()
            .HasForeignKey(e => e.ClosedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.RentalOrderItems)
            .WithOne(i => i.RentalOrder)
            .HasForeignKey(i => i.RentalOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.RentalOrderPackages)
            .WithOne(p => p.RentalOrder)
            .HasForeignKey(p => p.RentalOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Payments)
            .WithOne(p => p.RentalOrder)
            .HasForeignKey(p => p.RentalOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Channel)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.PaymentType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.TotalAmount)
            .HasPrecision(10, 2);

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Channel);
        builder.HasIndex(e => e.StripeSessionId);
    }
}

public class RentalOrderItemConfiguration : IEntityTypeConfiguration<RentalOrderItem>
{
    public void Configure(EntityTypeBuilder<RentalOrderItem> builder)
    {
        builder.ToTable("RentalOrderItems");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.RentalOrder)
            .WithMany(o => o.RentalOrderItems)
            .HasForeignKey(e => e.RentalOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Item)
            .WithMany()
            .HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.ItemRate)
            .WithMany()
            .HasForeignKey(e => e.ItemRateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.ItemNameSnapshot)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.DailyRateSnapshot)
            .HasPrecision(10, 2);

        builder.Property(e => e.ConditionRemarks)
            .HasMaxLength(500);

        builder.HasIndex(e => e.ItemId);
        builder.HasIndex(e => e.RentalOrderId);
    }
}

public class RentalOrderPackageConfiguration : IEntityTypeConfiguration<RentalOrderPackage>
{
    public void Configure(EntityTypeBuilder<RentalOrderPackage> builder)
    {
        builder.ToTable("RentalOrderPackages");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.RentalOrder)
            .WithMany(o => o.RentalOrderPackages)
            .HasForeignKey(e => e.RentalOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Package)
            .WithMany()
            .HasForeignKey(e => e.PackageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.PackageRate)
            .WithMany()
            .HasForeignKey(e => e.PackageRateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Items)
            .WithOne(i => i.RentalOrderPackage)
            .HasForeignKey(i => i.RentalOrderPackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.PackageNameSnapshot)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.DailyRateSnapshot)
            .HasPrecision(10, 2);

        builder.HasIndex(e => e.PackageId);
        builder.HasIndex(e => e.RentalOrderId);
    }
}

public class RentalOrderPackageItemConfiguration : IEntityTypeConfiguration<RentalOrderPackageItem>
{
    public void Configure(EntityTypeBuilder<RentalOrderPackageItem> builder)
    {
        builder.ToTable("RentalOrderPackageItems");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.RentalOrderPackage)
            .WithMany(p => p.Items)
            .HasForeignKey(e => e.RentalOrderPackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Item)
            .WithMany()
            .HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.ItemNameSnapshot)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(e => e.ItemId);
        builder.HasIndex(e => e.RentalOrderPackageId);
    }
}

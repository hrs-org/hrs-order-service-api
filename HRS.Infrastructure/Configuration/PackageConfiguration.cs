using HRS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRS.Infrastructure.Configuration;

public class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.BasePrice)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        // Relationships
        builder.HasMany(p => p.PackageItems)
            .WithOne(pi => pi.Package)
            .HasForeignKey(pi => pi.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PackageRates)
            .WithOne(pr => pr.Package)
            .HasForeignKey(pr => pr.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.CreatedBy)
            .WithMany()
            .HasForeignKey(p => p.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.UpdatedBy)
            .WithMany()
            .HasForeignKey(p => p.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PackageItemConfiguration : IEntityTypeConfiguration<PackageItem>
{
    public void Configure(EntityTypeBuilder<PackageItem> builder)
    {
        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.Quantity)
            .IsRequired()
            .HasDefaultValue(1);

        // Prevent duplicates (PackageId + ItemId)
        builder.HasIndex(pi => new { pi.PackageId, pi.ItemId })
            .IsUnique();

        // Relationships
        builder.HasOne(pi => pi.Package)
            .WithMany(p => p.PackageItems)
            .HasForeignKey(pi => pi.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pi => pi.Item)
            .WithMany()
            .HasForeignKey(pi => pi.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PackageRateConfiguration : IEntityTypeConfiguration<PackageRate>
{
    public void Configure(EntityTypeBuilder<PackageRate> builder)
    {
        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.MinDays)
            .IsRequired();

        builder.Property(pr => pr.DailyRate)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(pr => pr.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(pr => pr.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(pr => pr.Package)
            .WithMany(p => p.PackageRates)
            .HasForeignKey(pr => pr.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pr => pr.CreatedBy)
            .WithMany()
            .HasForeignKey(pr => pr.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.UpdatedBy)
            .WithMany()
            .HasForeignKey(pr => pr.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

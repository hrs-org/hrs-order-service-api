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






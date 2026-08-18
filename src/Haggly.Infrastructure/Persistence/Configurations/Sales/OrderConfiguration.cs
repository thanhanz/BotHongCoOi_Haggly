using Haggly.Domain.Modules.Identity;
using Haggly.Domain.Modules.Sales;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Sales;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", "sales", table =>
        {
            table.HasCheckConstraint(
                "CK_orders_amount_bounds",
                "\"TotalToCharge\" >= 0 AND \"TotalPaid\" >= 0 AND \"TotalPaid\" <= \"TotalToCharge\"");
        });
        builder.HasKey(order => order.Id);
        builder.HasIndex(order => order.OrderNo).IsUnique();
        builder.HasIndex(order => new { order.BuyerId, order.PlacedAt });
        builder.Property(order => order.OrderNo).HasMaxLength(64).IsRequired();
        builder.Property(order => order.BuyerId).IsRequired();
        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(order => order.TotalToCharge).HasPrecision(18, 2).IsRequired();
        builder.Property(order => order.TotalPaid).HasPrecision(18, 2).IsRequired();
        builder.Property(order => order.Currency).HasMaxLength(3).IsRequired();
        builder.Property(order => order.CancellationReason).HasMaxLength(500);
        builder.ConfigureAuditable();

        builder.HasOne(order => order.Buyer)
            .WithMany()
            .HasForeignKey(order => order.BuyerId)
            .HasPrincipalKey(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(order => order.StallFulfillments)
            .WithOne(fulfillment => fulfillment.Order)
            .HasForeignKey(fulfillment => fulfillment.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

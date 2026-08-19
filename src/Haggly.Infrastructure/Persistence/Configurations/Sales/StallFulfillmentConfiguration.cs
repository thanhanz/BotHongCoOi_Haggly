using Haggly.Domain.Modules.Markets;
using Haggly.Domain.Modules.Sales;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Sales;

internal sealed class StallFulfillmentConfiguration : IEntityTypeConfiguration<StallFulfillment>
{
    public void Configure(EntityTypeBuilder<StallFulfillment> builder)
    {
        builder.ToTable("stall_fulfillments", "sales", table =>
        {
            table.HasCheckConstraint(
                "CK_stall_fulfillments_amount_bounds",
                "\"Subtotal\" >= 0 AND \"FinalAmount\" >= 0 AND \"PaidAmount\" >= 0 AND \"PaidAmount\" <= \"FinalAmount\"");
        });
        builder.HasKey(fulfillment => fulfillment.Id);
        builder.HasIndex(fulfillment => fulfillment.FulfillmentNo).IsUnique();
        builder.HasIndex(fulfillment => new { fulfillment.OrderId, fulfillment.StallId }).IsUnique();
        builder.Property(fulfillment => fulfillment.FulfillmentNo).HasMaxLength(128).IsRequired();
        builder.Property(fulfillment => fulfillment.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(fulfillment => fulfillment.Subtotal).HasPrecision(18, 2).IsRequired();
        builder.Property(fulfillment => fulfillment.FinalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(fulfillment => fulfillment.PaidAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(fulfillment => fulfillment.PickupCode).HasMaxLength(64);
        builder.Property(fulfillment => fulfillment.CancellationReason).HasMaxLength(500);
        builder.ConfigureAuditable();

        builder.HasOne(fulfillment => fulfillment.Stall)
            .WithMany()
            .HasForeignKey(fulfillment => fulfillment.StallId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(fulfillment => fulfillment.OrderItems)
            .WithOne(item => item.StallFulfillment)
            .HasForeignKey(item => item.StallFulfillmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

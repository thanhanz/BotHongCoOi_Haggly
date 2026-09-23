using Haggly.Domain.Modules.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Payments;

internal sealed class PaymentAllocationConfiguration
    : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("payment_allocations", "payments", table =>
        {
            table.HasCheckConstraint(
                "CK_payment_allocations_amount",
                "\"AllocatedAmount\" > 0");
        });
        builder.HasKey(allocation => allocation.Id);
        builder.HasIndex(allocation => new
        {
            allocation.PaymentTransactionId,
            allocation.StallFulfillmentId
        }).IsUnique();
        builder.Property(allocation => allocation.AllocationType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(allocation => allocation.AllocatedAmount)
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(allocation => allocation.AllocatedAt).IsRequired();
        builder.Property(allocation => allocation.CreatedAt).IsRequired();

        builder.HasOne(allocation => allocation.PaymentTransaction)
            .WithMany(transaction => transaction.Allocations)
            .HasForeignKey(allocation => allocation.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(allocation => allocation.StallFulfillment)
            .WithMany()
            .HasForeignKey(allocation => allocation.StallFulfillmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

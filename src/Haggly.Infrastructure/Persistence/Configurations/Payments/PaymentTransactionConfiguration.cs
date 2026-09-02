using Haggly.Domain.Modules.Payments;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Payments;

internal sealed class PaymentTransactionConfiguration
    : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transactions", "payments", table =>
        {
            table.HasCheckConstraint(
                "CK_payment_transactions_amount",
                "\"Amount\" > 0");
        });

        builder.HasKey(transaction => transaction.Id);
        builder.HasIndex(transaction => new { transaction.PaymentId, transaction.CreatedAt });
        builder.HasIndex(transaction => transaction.ProviderTransactionId)
            .IsUnique()
            .HasFilter("\"ProviderTransactionId\" IS NOT NULL");

        builder.Property(transaction => transaction.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(transaction => transaction.ProviderTransactionId)
            .HasMaxLength(256);
        builder.Property(transaction => transaction.Amount)
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(transaction => transaction.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(transaction => transaction.ProviderResponseCode)
            .HasMaxLength(100);
        builder.Property(transaction => transaction.ProviderResponseData)
            .HasMaxLength(4000);
        builder.Property(transaction => transaction.FailureReason)
            .HasMaxLength(1000);
        builder.ConfigureAuditable();

        builder.HasOne(transaction => transaction.Payment)
            .WithMany(payment => payment.Transactions)
            .HasForeignKey(transaction => transaction.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

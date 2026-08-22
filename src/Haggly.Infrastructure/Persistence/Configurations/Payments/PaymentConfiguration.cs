using Haggly.Domain.Modules.Payments;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Payments;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", "payments", table =>
        {
            table.HasCheckConstraint(
                "CK_payments_amounts",
                "\"AmountDue\" > 0 AND \"AmountPaid\" >= 0 AND \"AmountPaid\" <= \"AmountDue\"");
        });
        builder.HasKey(payment => payment.Id);
        builder.HasIndex(payment => payment.OrderId).IsUnique();
        builder.HasIndex(payment => payment.PaymentNo).IsUnique();
        builder.Property(payment => payment.PaymentNo).HasMaxLength(64).IsRequired();
        builder.Property(payment => payment.AmountDue).HasPrecision(18, 2).IsRequired();
        builder.Property(payment => payment.AmountPaid).HasPrecision(18, 2).IsRequired();
        builder.Property(payment => payment.Currency).HasMaxLength(3).IsRequired();
        builder.Property(payment => payment.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(payment => payment.InitiatedAt).IsRequired();
        builder.ConfigureAuditable();

        builder.HasOne(payment => payment.Order)
            .WithMany()
            .HasForeignKey(payment => payment.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(payment => payment.PaymentMethod);
    }
}

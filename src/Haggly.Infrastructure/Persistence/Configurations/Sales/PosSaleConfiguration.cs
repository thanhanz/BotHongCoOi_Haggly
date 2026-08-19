using Haggly.Domain.Modules.Sales;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Sales;

internal sealed class PosSaleConfiguration : IEntityTypeConfiguration<PosSale>
{
    public void Configure(EntityTypeBuilder<PosSale> builder)
    {
        builder.ToTable("pos_sales", "sales", table =>
        {
            table.HasCheckConstraint("CK_pos_sales_total_amount_bounds", "\"TotalAmount\" >= 0");
            table.HasCheckConstraint("CK_pos_sales_amount_paid_bounds", "\"AmountPaid\" >= 0 AND \"AmountPaid\" = \"TotalAmount\"");
        });
        builder.HasKey(sale => sale.Id);
        builder.HasIndex(sale => sale.SaleNo).IsUnique();
        builder.HasIndex(sale => new { sale.StallId, sale.ClientRequestId }).IsUnique();
        builder.Property(sale => sale.SaleNo).HasMaxLength(64).IsRequired();
        builder.Property(sale => sale.ClientRequestId).HasMaxLength(100).IsRequired();
        builder.Property(sale => sale.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(sale => sale.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(sale => sale.PaymentMethod).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(sale => sale.PaymentStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(sale => sale.AmountPaid).HasPrecision(18, 2).IsRequired();
        builder.Property(sale => sale.CompletedAt).IsRequired();
        builder.ConfigureAuditable();

        builder.HasMany(sale => sale.Items)
            .WithOne(item => item.PosSale)
            .HasForeignKey(item => item.PosSaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

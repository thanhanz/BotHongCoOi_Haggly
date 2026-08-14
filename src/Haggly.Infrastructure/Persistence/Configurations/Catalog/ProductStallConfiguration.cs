using Haggly.Domain.Modules.Catalog;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class ProductStallConfiguration : IEntityTypeConfiguration<ProductStall>
{
    public void Configure(EntityTypeBuilder<ProductStall> builder)
    {
        builder.ToTable("product_stalls", "catalog");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.StallId, x.ProductId }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        builder.Property(x => x.DisplayName).HasMaxLength(200);
        builder.Property(x => x.SellingUnit).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.MinimumOrderQuantity).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.DefaultUnitPrice).HasPrecision(18, 2).IsRequired();
        builder.ConfigureSoftDeletable();
        builder.HasQueryFilter(x => x.DeletedAt == null);
        builder.HasOne(x => x.Stall).WithMany().HasForeignKey(x => x.StallId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany(x => x.ProductStalls).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

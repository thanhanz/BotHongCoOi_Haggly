using Haggly.Domain.Modules.Catalog;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", "catalog");
        builder.HasKey(product => product.Id);
        builder.HasIndex(product => new { product.CategoryId, product.Name })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(product => new { product.CategoryId, product.Status });
        builder.Property(product => product.Name).HasMaxLength(200).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(1000);
        builder.Property(product => product.ImageUrl).HasMaxLength(2048);
        builder.Property(product => product.DefaultUnit)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(product => product.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.ConfigureSoftDeletable();
        builder.HasQueryFilter(product => product.DeletedAt == null);

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

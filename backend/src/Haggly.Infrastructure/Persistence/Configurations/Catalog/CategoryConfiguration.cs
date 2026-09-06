using Haggly.Domain.Modules.Catalog;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories", "catalog");
        builder.HasKey(category => category.Id);
        builder.HasIndex(category => category.Slug)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(category => category.ParentCategoryId);
        builder.HasIndex(category => new { category.Status, category.DisplayOrder, category.Name });
        builder.Property(category => category.Name).HasMaxLength(200).IsRequired();
        builder.Property(category => category.Slug).HasMaxLength(200).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(1000);
        builder.Property(category => category.ImageUrl).HasMaxLength(2048);
        builder.Property(category => category.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.ConfigureSoftDeletable();
        builder.HasQueryFilter(category => category.DeletedAt == null);

        builder.HasOne(category => category.ParentCategory)
            .WithMany(category => category.Children)
            .HasForeignKey(category => category.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

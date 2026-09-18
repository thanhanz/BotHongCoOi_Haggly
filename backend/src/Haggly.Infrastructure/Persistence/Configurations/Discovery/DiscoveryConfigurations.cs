using Haggly.Domain.Modules.Discovery;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Discovery;

internal sealed class CanonicalIngredientConfiguration : IEntityTypeConfiguration<CanonicalIngredient>
{
    public void Configure(EntityTypeBuilder<CanonicalIngredient> builder)
    {
        builder.ToTable("canonical_ingredients", "discovery"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired(); builder.Property(x => x.Name).HasMaxLength(200).IsRequired(); builder.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired(); builder.Property(x => x.Category).HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique(); builder.HasIndex(x => x.NormalizedName).IsUnique(); builder.HasIndex(x => new { x.IsActive, x.Name });
    }
}
internal sealed class CommonDishConfiguration : IEntityTypeConfiguration<CommonDish>
{
    public void Configure(EntityTypeBuilder<CommonDish> builder)
    {
        builder.ToTable("common_dishes", "discovery"); builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalId).HasMaxLength(64).IsRequired(); builder.Property(x => x.Name).HasMaxLength(200).IsRequired(); builder.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired(); builder.Property(x => x.Category).HasMaxLength(100);
        builder.HasIndex(x => x.ExternalId).IsUnique(); builder.HasIndex(x => x.NormalizedName).IsUnique(); builder.HasIndex(x => new { x.IsActive, x.Name });
    }
}
internal sealed class CommonDishIngredientConfiguration : IEntityTypeConfiguration<CommonDishIngredient>
{
    public void Configure(EntityTypeBuilder<CommonDishIngredient> builder)
    {
        builder.ToTable("common_dish_ingredients", "discovery"); builder.HasKey(x => new { x.DishId, x.CanonicalIngredientId }); builder.HasIndex(x => x.CanonicalIngredientId);
        builder.HasOne(x => x.Dish).WithMany(x => x.Ingredients).HasForeignKey(x => x.DishId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CanonicalIngredient).WithMany(x => x.Dishes).HasForeignKey(x => x.CanonicalIngredientId).OnDelete(DeleteBehavior.Restrict);
    }
}
internal sealed class ProductIngredientMappingConfiguration : IEntityTypeConfiguration<ProductIngredientMapping>
{
    public void Configure(EntityTypeBuilder<ProductIngredientMapping> builder)
    {
        builder.ToTable("product_ingredient_mappings", "discovery"); builder.HasKey(x => x.ProductId); builder.HasIndex(x => new { x.CanonicalIngredientId, x.Status });
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired(); builder.Property(x => x.MappingMethod).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Product).WithOne().HasForeignKey<ProductIngredientMapping>(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CanonicalIngredient).WithMany(x => x.ProductMappings).HasForeignKey(x => x.CanonicalIngredientId).OnDelete(DeleteBehavior.Restrict);
    }
}

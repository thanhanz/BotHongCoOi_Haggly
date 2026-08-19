using Haggly.Domain.Modules.Identity;
using Haggly.Domain.Modules.Sales;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Sales;

internal sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("carts", "sales");
        builder.HasKey(cart => cart.Id);
        builder.HasIndex(cart => cart.BuyerId).IsUnique();
        builder.Property(cart => cart.BuyerId).IsRequired();
        builder.ConfigureAuditable();

        builder.HasOne<BuyerProfile>()
            .WithMany()
            .HasForeignKey(cart => cart.BuyerId)
            .HasPrincipalKey(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(cart => cart.Items)
            .WithOne(item => item.Cart)
            .HasForeignKey(item => item.CartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

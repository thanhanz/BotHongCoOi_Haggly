using Haggly.Domain.Modules.Inventory;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class InventoryConfiguration : IEntityTypeConfiguration<DomainInventory>
{
    public void Configure(EntityTypeBuilder<DomainInventory> builder)
    {
        builder.ToTable("inventories", "inventory");
        builder.HasKey(inventory => inventory.Id);
        builder.HasIndex(inventory => inventory.StallId).IsUnique();
        builder.ConfigureAuditable();
        builder.HasOne(inventory => inventory.Stall)
            .WithOne()
            .HasForeignKey<DomainInventory>(inventory => inventory.StallId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

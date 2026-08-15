using Haggly.Domain.Modules.Inventory;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class DailyProductListingConfiguration : IEntityTypeConfiguration<DailyProductListing>
{
    public void Configure(EntityTypeBuilder<DailyProductListing> builder)
    {
        builder.ToTable("daily_product_listings", "inventory", table =>
        {
            table.HasCheckConstraint(
                "CK_daily_product_listings_quantity_bounds",
                "\"OpeningQuantity\" >= 0 AND \"CurrentQuantity\" >= 0 AND \"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"CurrentQuantity\"");
            table.HasCheckConstraint(
                "CK_daily_product_listings_available_quantity_bounds",
                "\"AvailableQuantity\" >= 0 AND \"AvailableQuantity\" = \"CurrentQuantity\" - \"ReservedQuantity\"");
        });
        builder.HasKey(listing => listing.Id);
        builder.HasIndex(listing => new { listing.InventorySessionId, listing.ProductStallId }).IsUnique();
        builder.Property(listing => listing.ProductNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(listing => listing.SellingUnitSnapshot)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(listing => listing.PublicUnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(listing => listing.OpeningQuantity).HasPrecision(18, 3).IsRequired();
        builder.Property(listing => listing.CurrentQuantity).HasPrecision(18, 3).IsRequired();
        builder.Property(listing => listing.ReservedQuantity).HasPrecision(18, 3).IsRequired();
        builder.Property(listing => listing.AvailableQuantity).HasPrecision(18, 3).IsRequired();
        builder.Property(listing => listing.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(listing => listing.Version)
            .IsConcurrencyToken()
            .ValueGeneratedNever()
            .IsRequired();
        builder.ConfigureAuditable();

        builder.HasOne(listing => listing.InventorySession)
            .WithMany(session => session.DailyProductListings)
            .HasForeignKey(listing => listing.InventorySessionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(listing => listing.ProductStall)
            .WithMany()
            .HasForeignKey(listing => listing.ProductStallId)
            .OnDelete(DeleteBehavior.Restrict);

        // Reservation persistence belongs to the later Sales/order workflow.
        builder.Ignore(listing => listing.InventoryReservations);
    }
}

using Haggly.Domain.Modules.Inventory;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class InventorySessionConfiguration : IEntityTypeConfiguration<InventorySession>
{
    public void Configure(EntityTypeBuilder<InventorySession> builder)
    {
        builder.ToTable("inventory_sessions", "inventory");
        builder.HasKey(session => session.Id);
        builder.HasIndex(session => new { session.StallId, session.BusinessDate }).IsUnique();
        builder.Property(session => session.BusinessDate).IsRequired();
        builder.Property(session => session.OpenedAt).IsRequired();
        builder.Property(session => session.OpenedBy).IsRequired();
        builder.Property(session => session.ClosedAt);
        builder.Property(session => session.ClosedBy);
        builder.Property(session => session.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(session => session.Notes).HasMaxLength(1000);
        builder.ConfigureAuditable();

        builder.HasOne(session => session.Stall)
            .WithMany()
            .HasForeignKey(session => session.StallId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

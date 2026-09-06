using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Identity;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles", "identity");
        builder.HasKey(userRole => userRole.Id);
        builder.HasIndex(userRole => new { userRole.UserId, userRole.RoleId }).IsUnique();
        builder.Property(userRole => userRole.AssignedAt).IsRequired();
        builder.Property(userRole => userRole.IsActive).IsRequired();
        builder.ConfigureAuditable();
    }
}

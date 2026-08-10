using Haggly.Domain.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Common;

internal static class EntityConfigurationExtensions
{
    public static void ConfigureAuditable<T>(this EntityTypeBuilder<T> builder)
        where T : AuditableEntity
    {
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.CreatedBy);
        builder.Property(entity => entity.UpdatedAt);
        builder.Property(entity => entity.UpdatedBy);
    }

    public static void ConfigureSoftDeletable<T>(this EntityTypeBuilder<T> builder)
        where T : SoftDeletableEntity
    {
        builder.ConfigureAuditable();
        builder.Property(entity => entity.DeletedAt);
        builder.Property(entity => entity.DeletedBy);
    }

    public static void ConfigureAuditableRecord<T>(this EntityTypeBuilder<T> builder)
        where T : class
    {
        builder.Property("CreatedAt").IsRequired();
        builder.Property("CreatedBy");
        builder.Property("UpdatedAt");
        builder.Property("UpdatedBy");
    }
}

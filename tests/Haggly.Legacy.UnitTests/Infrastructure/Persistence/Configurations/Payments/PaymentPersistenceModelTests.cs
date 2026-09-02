using Haggly.Domain.Modules.Payments;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Persistence.Configurations.Payments;

public sealed class PaymentPersistenceModelTests
{
    [Fact]
    public void Model_WhenBuilt_MapsPaymentToPaymentsSchemaWithUniqueOrder()
    {
        using var context = new HagglyDbContext(new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql("Host=localhost;Database=haggly;Username=postgres;Password=postgres")
            .Options);

        var entity = context.Model.FindEntityType(typeof(Payment));

        Assert.NotNull(entity);
        Assert.Equal("payments", entity.GetSchema());
        Assert.Equal("payments", entity.GetTableName());
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique && index.Properties.Single().Name == nameof(Payment.OrderId));
    }
}

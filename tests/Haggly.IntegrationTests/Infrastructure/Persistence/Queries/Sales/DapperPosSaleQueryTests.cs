using Dapper;
using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Persistence.Queries.Sales;
using Xunit;

namespace Haggly.IntegrationTests.Infrastructure.Persistence.Queries.Sales;

public sealed class DapperPosSaleQueryTests
{
    [Fact]
    public async Task GetPageAsync_WhenSalesExist_ReturnsPagedSaleHeaders()
    {
        var db = new DapperDbContext(IntegrationTestDatabase.CreateConfiguration());
        var stallId = Guid.NewGuid();
        var olderSaleId = Guid.NewGuid();
        var newerSaleId = Guid.NewGuid();
        var otherStallSaleId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var connection = await db.OpenConnectionAsync(CancellationToken.None))
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO sales.pos_sales
                    ("Id", "StallId", "SaleNo", "ClientRequestId", "Status", "TotalAmount",
                     "CompletedBy", "CompletedAt", "PaymentMethod", "PaymentStatus", "AmountPaid", "CreatedAt")
                VALUES
                    (@OlderSaleId, @StallId, @OlderSaleNo, @OlderRequestId, 'COMPLETED', 10.00,
                     @CompletedBy, @OlderCompletedAt, 'CASH', 'PAID', 10.00, @OlderCompletedAt),
                    (@NewerSaleId, @StallId, @NewerSaleNo, @NewerRequestId, 'COMPLETED', 20.00,
                     @CompletedBy, @NewerCompletedAt, 'CASH', 'PAID', 20.00, @NewerCompletedAt),
                    (@OtherStallSaleId, @OtherStallId, @OtherSaleNo, @OtherRequestId, 'COMPLETED', 30.00,
                     @CompletedBy, @OtherCompletedAt, 'CASH', 'PAID', 30.00, @OtherCompletedAt);

                """,
                new
                {
                    OlderSaleId = olderSaleId,
                    NewerSaleId = newerSaleId,
                    OtherStallSaleId = otherStallSaleId,
                    StallId = stallId,
                    OtherStallId = Guid.NewGuid(),
                    OlderSaleNo = $"POS-{olderSaleId:N}",
                    NewerSaleNo = $"POS-{newerSaleId:N}",
                    OtherSaleNo = $"POS-{otherStallSaleId:N}",
                    OlderRequestId = Guid.NewGuid().ToString("N"),
                    NewerRequestId = Guid.NewGuid().ToString("N"),
                    OtherRequestId = Guid.NewGuid().ToString("N"),
                    CompletedBy = Guid.NewGuid(),
                    OlderCompletedAt = now.AddMinutes(-2),
                    NewerCompletedAt = now.AddMinutes(-1),
                    OtherCompletedAt = now
                });
        }

        var result = await new DapperPosSaleQuery(db)
            .GetPageAsync(stallId, page: 1, pageSize: 1, CancellationToken.None);

        var sale = Assert.Single(result.Items);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(newerSaleId, sale.Id);
        Assert.Empty(sale.Items);
    }
}

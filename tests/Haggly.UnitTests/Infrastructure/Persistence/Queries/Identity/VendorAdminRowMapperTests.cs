using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Persistence.Queries.Identity;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Persistence.Queries.Identity;

public sealed class VendorRowMapperTests
{
    [Fact]
    public void MapDatabaseRow_ConvertsEnumsAndUtcDatesToResponseDto()
    {
        var userId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Unspecified);
        var approvedAt = new DateTime(2026, 8, 12, 11, 0, 0, DateTimeKind.Unspecified);

        var result = VendorRowMapper.ToVendorQueryDto(new VendorRow(
            userId, "vendor@example.com", "0900000000", "Vendor One", "Vendor Stall",
            "BR-1", "TAX-1", "ACTIVE", "APPROVED", approvedAt, approverId,
            createdAt, null, null));

        Assert.Equal(UserStatus.ACTIVE, result.UserStatus);
        Assert.Equal(ApprovalStatus.APPROVED, result.ApprovalStatus);
        Assert.Equal(new DateTimeOffset(approvedAt, TimeSpan.Zero), result.ApprovedAt);
        Assert.Equal(new DateTimeOffset(createdAt, TimeSpan.Zero), result.CreatedAt);
        Assert.Equal(approverId, result.ApprovedBy);
    }
}

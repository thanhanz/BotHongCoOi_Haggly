using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Common;
using Haggly.Application.Modules.Identity.Administration;
using Haggly.Application.Modules.Identity.Administration.Queries;
using Haggly.Application.Modules.Identity.Dtos;
using Haggly.Domain.Modules.Identity;
using Xunit;

namespace Haggly.UnitTests;

public sealed class IdentityVendorQueryTests
{
    [Fact]
    public async Task HandleGetVendors_WithDefaults_ReturnsPagedVendorDtos()
    {
        var vendor = new VendorAdminDto(
            Guid.NewGuid(), "vendor@example.com", "0900000000", "Vendor One", "Vendor Stall",
            null, null, UserStatus.PENDING, ApprovalStatus.PENDING, null, null,
            DateTimeOffset.UtcNow, null, null);
        var query = new RecordingVendorAdminQuery(vendor);
        var handler = new GetVendorsHandler(query);

        var result = await handler.Handle(new GetVendorsQuery(), CancellationToken.None);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(vendor.UserId, result.Items.FirstOrDefault()?.UserId);
        Assert.Null(query.LastFilter!.ApprovalStatus);
    }

    [Fact]
    public async Task HandleGetVendors_WithFilterAndSearch_PassesNormalizedValues()
    {
        var query = new RecordingVendorAdminQuery();
        var handler = new GetVendorsHandler(query);

        await handler.Handle(
            new GetVendorsQuery(ApprovalStatus.APPROVED, "  fish  ", 2, 50),
            CancellationToken.None);

        Assert.Equal(ApprovalStatus.APPROVED, query.LastFilter!.ApprovalStatus);
        Assert.Equal("fish", query.LastFilter.Search);
        Assert.Equal(2, query.LastFilter.Page);
        Assert.Equal(50, query.LastFilter.PageSize);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task HandleGetVendors_WithInvalidPaging_ThrowsValidationException(
        int page,
        int pageSize)
    {
        var handler = new GetVendorsHandler(new RecordingVendorAdminQuery());

        await Assert.ThrowsAsync<VendorQueryValidationException>(() =>
            handler.Handle(new GetVendorsQuery(null, null, page, pageSize), CancellationToken.None));
    }

    private sealed class RecordingVendorAdminQuery(VendorAdminDto? item = null)
        : IVendorAdminQuery
    {
        public VendorListFilter? LastFilter { get; private set; }

        public Task<PagedResult<VendorAdminDto>> GetPageAsync(
            VendorListFilter filter,
            CancellationToken cancellationToken)
        {
            LastFilter = filter;
            var items = item is null ? Array.Empty<VendorAdminDto>() : [item];
            return Task.FromResult(new PagedResult<VendorAdminDto>(
                items, filter.Page, filter.PageSize, items.Length));
        }
    }
}

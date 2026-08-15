using Haggly.Domain.Modules.Inventory;

namespace Haggly.Api.Endpoints.Inventory.Requests;

public sealed record UpdateDailyProductListingRequest(
    decimal? PublicUnitPrice,
    DailyListingStatus? Status,
    long ExpectedVersion);

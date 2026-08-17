using Haggly.Domain.Modules.Catalog;

namespace Haggly.Api.Endpoints.Catalog.Requests;

public sealed record UpdateProductStallRequest(string? DisplayName, ProductUnit? SellingUnit,
    decimal? MinimumOrderQuantity, decimal? CurrentUnitPrice, bool? IsNegotiable, bool? IsActive,
    long ExpectedVersion);

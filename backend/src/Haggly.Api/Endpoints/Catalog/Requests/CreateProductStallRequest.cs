using Haggly.Domain.Modules.Catalog;

namespace Haggly.Api.Endpoints.Catalog.Requests;

public sealed record CreateProductStallRequest(Guid ProductId, string? DisplayName,
    ProductUnit SellingUnit, decimal MinimumOrderQuantity, decimal CurrentUnitPrice, bool IsNegotiable);

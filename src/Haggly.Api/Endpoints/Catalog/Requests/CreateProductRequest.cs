using Haggly.Domain.Modules.Catalog;

namespace Haggly.Api.Endpoints.Catalog.Requests;

public sealed record CreateProductRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    ProductUnit DefaultUnit,
    string? ImageUrl);

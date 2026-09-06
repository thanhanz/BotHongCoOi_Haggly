namespace Haggly.Api.Endpoints.Catalog.Requests;

public sealed record CreateCategoryRequest(
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    Guid? ParentCategoryId,
    int DisplayOrder);

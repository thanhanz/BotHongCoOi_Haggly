namespace Haggly.Application.Modules.Catalog.Queries.Products;

public sealed record ProductListFilter(
    Guid? CategoryId,
    int Page,
    int PageSize);

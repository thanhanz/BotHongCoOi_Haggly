namespace Haggly.Application.Modules.Inventory.Queries;

public sealed record ProductListingListFilter(
    Guid? CategoryId,
    Guid? StallId,
    string Sort,
    int Page,
    int PageSize);

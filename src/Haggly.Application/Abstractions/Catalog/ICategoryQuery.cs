using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Queries.Categories;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Abstractions.Catalog;

public interface ICategoryQuery
{
    Task<PagedResult<Category>> GetPageAsync(
        CategoryListFilter filter,
        CancellationToken cancellationToken);

    Task<Category?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken);
}

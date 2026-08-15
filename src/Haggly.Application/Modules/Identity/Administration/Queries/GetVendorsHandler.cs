using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Common;
using Haggly.Application.Modules.Identity.Administration.Queries;
using Haggly.Application.Modules.Identity.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Identity.Administration.Queries;

public sealed class GetVendorsHandler(IVendorAdminQuery query)
    : IRequestHandler<GetVendorsQuery, PagedResult<VendorQueryDto>>
{
    public Task<PagedResult<VendorQueryDto>> Handle(
        GetVendorsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Page < 1)
            throw new VendorQueryValidationException("Page must be at least 1.");

        if (request.PageSize is < 1 or > 100)
            throw new VendorQueryValidationException("Page size must be between 1 and 100.");

        if (request.ApprovalStatus is not null && !Enum.IsDefined(request.ApprovalStatus.Value))
            throw new VendorQueryValidationException("A valid approval status is required.");

        var filter = new VendorListFilter(
            request.ApprovalStatus,
            string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            request.Page,
            request.PageSize);

        return query.GetPageAsync(filter, cancellationToken);
    }
}

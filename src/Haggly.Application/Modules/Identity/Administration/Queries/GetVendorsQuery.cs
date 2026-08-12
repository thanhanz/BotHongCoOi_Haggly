using Haggly.Application.Common;
using Haggly.Application.Modules.Identity.Dtos;
using Haggly.Domain.Modules.Identity;
using MediatR;

namespace Haggly.Application.Modules.Identity.Administration.Queries;

public sealed record GetVendorsQuery(
    ApprovalStatus? ApprovalStatus = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<VendorAdminDto>>;

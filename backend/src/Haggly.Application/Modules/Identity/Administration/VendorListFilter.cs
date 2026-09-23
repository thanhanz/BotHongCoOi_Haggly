using Haggly.Domain.Modules.Identity;

namespace Haggly.Application.Modules.Identity.Administration;

public sealed record VendorListFilter(
    ApprovalStatus? ApprovalStatus,
    string? Search,
    int Page,
    int PageSize);

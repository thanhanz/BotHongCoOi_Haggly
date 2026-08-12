using Haggly.Application.Modules.Identity.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Identity.Administration.Commands;

public sealed record ApproveVendorCommand(Guid VendorId, Guid ApprovedBy)
    : IRequest<VendorQueryDto>;

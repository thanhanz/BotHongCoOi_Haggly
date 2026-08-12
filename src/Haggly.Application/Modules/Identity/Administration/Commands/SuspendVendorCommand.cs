using Haggly.Application.Modules.Identity.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Identity.Administration.Commands;

public sealed record SuspendVendorCommand(Guid VendorId, Guid SuspendedBy)
    : IRequest<VendorAdminDto>;

using Haggly.Application.Modules.Identity.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Identity.Administration.Commands;

public sealed record RejectVendorCommand(Guid VendorId, Guid RejectedBy)
    : IRequest<VendorAdminDto>;

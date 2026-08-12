using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Administration.Commands;
using Haggly.Application.Modules.Identity.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Identity.Administration;

public sealed class SuspendVendorHandler(
    IVendorAdminCommandRepository repository,
    TimeProvider timeProvider)
    : VendorCommandHandlerBase(repository, timeProvider),
      IRequestHandler<SuspendVendorCommand, VendorAdminDto>
{
    public async Task<VendorAdminDto> Handle(
        SuspendVendorCommand request,
        CancellationToken cancellationToken)
    {
        EnsureActor(request.SuspendedBy);
        var aggregate = await LoadAsync(request.VendorId, cancellationToken);

        try
        {
            aggregate.VendorProfile.Suspend(
                aggregate.User,
                request.SuspendedBy,
                GetUtcNow());
        }
        catch (InvalidOperationException exception)
        {
            throw Conflict(exception);
        }

        return await SaveAsync(aggregate, cancellationToken);
    }
}

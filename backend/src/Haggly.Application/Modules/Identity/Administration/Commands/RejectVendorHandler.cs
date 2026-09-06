using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Administration.Commands;
using Haggly.Application.Modules.Identity.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Identity.Administration.Commands;

public sealed class RejectVendorHandler(
    IVendorAdminCommandRepository repository,
    TimeProvider timeProvider)
    : VendorCommandHandlerBase(repository, timeProvider),
      IRequestHandler<RejectVendorCommand, VendorQueryDto>
{
    public async Task<VendorQueryDto> Handle(
        RejectVendorCommand request,
        CancellationToken cancellationToken)
    {
        EnsureActor(request.RejectedBy);
        var aggregate = await LoadAsync(request.VendorId, cancellationToken);

        try
        {
            aggregate.VendorProfile.Reject(
                aggregate.User,
                request.RejectedBy,
                GetUtcNow());
        }
        catch (InvalidOperationException exception)
        {
            throw Conflict(exception);
        }

        return await SaveAsync(aggregate, cancellationToken);
    }
}

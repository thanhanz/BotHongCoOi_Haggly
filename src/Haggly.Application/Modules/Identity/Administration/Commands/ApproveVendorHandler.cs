using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Administration.Commands;
using Haggly.Application.Modules.Identity.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Identity.Administration.Commands;

public sealed class ApproveVendorHandler(
    IVendorAdminCommandRepository repository,
    TimeProvider timeProvider)
    : VendorCommandHandlerBase(repository, timeProvider),
      IRequestHandler<ApproveVendorCommand, VendorQueryDto>
{
    public async Task<VendorQueryDto> Handle(
        ApproveVendorCommand request,
        CancellationToken cancellationToken)
    {
        EnsureActor(request.ApprovedBy);
        var aggregate = await LoadAsync(request.VendorId, cancellationToken);

        try
        {
            aggregate.VendorProfile.Approve(
                aggregate.User,
                request.ApprovedBy,
                GetUtcNow());
        }
        catch (InvalidOperationException exception)
        {
            throw Conflict(exception);
        }

        return await SaveAsync(aggregate, cancellationToken);
    }
}

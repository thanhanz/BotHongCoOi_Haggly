using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Dtos;

namespace Haggly.Application.Modules.Identity.Administration;

public abstract class VendorCommandHandlerBase(
    IVendorAdminCommandRepository repository,
    TimeProvider timeProvider)
{
    private readonly IVendorAdminCommandRepository _repository = repository;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected async Task<VendorAdminAggregate> LoadAsync(
        Guid vendorId,
        CancellationToken cancellationToken)
    {
        if (vendorId == Guid.Empty)
            throw new VendorCommandValidationException("A valid vendor ID is required.");

        return await _repository.FindByIdAsync(vendorId, cancellationToken)
            ?? throw new VendorNotFoundException("The vendor was not found.");
    }

    protected DateTimeOffset GetUtcNow() => _timeProvider.GetUtcNow();

    protected static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new VendorCommandValidationException("A valid administrator ID is required.");
    }

    protected async Task<VendorQueryDto> SaveAsync(
        VendorAdminAggregate aggregate,
        CancellationToken cancellationToken)
    {
        await _repository.SaveChangesAsync(cancellationToken);
        return VendorQueryDto.From(aggregate.User, aggregate.VendorProfile);
    }

    protected static VendorTransitionConflictException Conflict(Exception exception)
        => new(exception.Message);
}

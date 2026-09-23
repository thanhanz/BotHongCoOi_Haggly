using Haggly.Application.Abstractions.Discovery;
using Haggly.Domain.Modules.Discovery;
using MediatR;

namespace Haggly.Application.Modules.Discovery.Commands;

public sealed class SetProductIngredientMappingHandler(
    IDiscoveryCommandRepository repository,
    TimeProvider timeProvider)
    : IRequestHandler<SetProductIngredientMappingCommand>
{
    public async Task Handle(
        SetProductIngredientMappingCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ProductId == Guid.Empty)
        {
            throw new DiscoveryValidationException("A valid product ID is required.");
        }

        var status = ParseStatus(request.Status);
        await repository.SetProductMappingAsync(
            request.ProductId,
            request.CanonicalIngredientId,
            status,
            ProductIngredientMappingMethod.MANUAL,
            timeProvider.GetUtcNow(),
            cancellationToken);
    }

    private static ProductIngredientMappingStatus ParseStatus(string? status)
        => status?.Trim().ToUpperInvariant() switch
        {
            null or "APPROVED" => ProductIngredientMappingStatus.APPROVED,
            "REJECTED" => ProductIngredientMappingStatus.REJECTED,
            _ => throw new DiscoveryValidationException(
                "Status must be APPROVED or REJECTED.")
        };
}

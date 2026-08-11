using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Commands;
using Haggly.Application.Modules.Markets.Exceptions;
using MediatR;

namespace Haggly.Application.Modules.Markets.Handlers;

public sealed class DeleteMarketHandler(IMarketCommandRepository repository)
    : IRequestHandler<DeleteMarketCommand, bool>
{
    public async Task<bool> Handle(
        DeleteMarketCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Id == Guid.Empty)
            throw new MarketValidationException("A valid market ID is required.");

        var market = await repository.FindByIdAsync(command.Id, cancellationToken)
            ?? throw new MarketNotFoundException("The market was not found.");

        market.DeletedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}

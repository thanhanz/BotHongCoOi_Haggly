using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Commands.Stalls;
using Haggly.Application.Modules.Markets.Exceptions.Stalls;
using MediatR;

namespace Haggly.Application.Modules.Markets.Commands.Stalls;

public sealed class DeleteStallHandler(IStallCommandRepository repository)
    : IRequestHandler<DeleteStallCommand, bool>
{
    public async Task<bool> Handle(
        DeleteStallCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Id == Guid.Empty)
            throw new StallValidationException("A valid stall ID is required.");

        var stall = await repository.FindByIdAsync(command.Id, cancellationToken)
            ?? throw new StallNotFoundException("The stall was not found.");

        stall.DeletedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}

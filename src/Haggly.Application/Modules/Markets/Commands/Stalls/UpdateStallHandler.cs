using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Commands.Stalls;
using Haggly.Application.Modules.Markets.Dtos.Stalls;
using Haggly.Application.Modules.Markets.Exceptions.Stalls;
using Haggly.Application.Modules.Markets.Validation.Stalls;
using MediatR;

namespace Haggly.Application.Modules.Markets.Commands.Stalls;

public sealed class UpdateStallHandler(IStallCommandRepository repository)
    : IRequestHandler<UpdateStallCommand, StallDto>
{
    public async Task<StallDto> Handle(
        UpdateStallCommand command,
        CancellationToken cancellationToken)
    {
        StallValidation.Validate(command);

        var stall = await repository.FindByIdAsync(command.Id, cancellationToken)
            ?? throw new StallNotFoundException("The stall was not found.");

        if (!await repository.MarketExistsAsync(command.MarketId, cancellationToken))
            throw new StallNotFoundException("The market was not found.");

        if (!await repository.VendorExistsAsync(command.VendorId, cancellationToken))
            throw new StallNotFoundException("The vendor was not found.");

        var code = command.Code.Trim();
        if (await repository.CodeExistsAsync(command.MarketId, code, command.Id, cancellationToken))
            throw new StallConflictException("A stall with this code already exists in the market.");

        stall.MarketId = command.MarketId;
        stall.VendorId = command.VendorId;
        stall.Code = code;
        stall.Name = command.Name.Trim();
        stall.LocationDescription = command.LocationDescription?.Trim();
        stall.PhoneNumber = command.PhoneNumber?.Trim();
        stall.Status = command.Status;
        stall.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);

        return StallDto.From(stall);
    }
}

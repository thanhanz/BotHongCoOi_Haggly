using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Commands.Stalls;
using Haggly.Application.Modules.Markets.Dtos.Stalls;
using Haggly.Application.Modules.Markets.Exceptions.Stalls;
using Haggly.Application.Modules.Markets.Validation.Stalls;
using Haggly.Domain.Modules.Markets;
using Haggly.Application.Common.Time;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;
using MediatR;

namespace Haggly.Application.Modules.Markets.Commands.Stalls;

public sealed class CreateStallHandler(IStallCommandRepository repository, IBusinessClock businessClock)
    : IRequestHandler<CreateStallCommand, StallDto>
{
    public async Task<StallDto> Handle(
        CreateStallCommand command,
        CancellationToken cancellationToken)
    {
        StallValidation.Validate(command);

        if (!await repository.MarketExistsAsync(command.MarketId, cancellationToken))
            throw new StallNotFoundException("The market was not found.");

        if (!await repository.VendorExistsAsync(command.VendorId, cancellationToken))
            throw new StallNotFoundException("The vendor was not found.");

        var code = command.Code.Trim();
        if (await repository.CodeExistsAsync(command.MarketId, code, null, cancellationToken))
            throw new StallConflictException("A stall with this code already exists in the market.");

        var stall = new Stall
        {
            MarketId = command.MarketId,
            VendorId = command.VendorId,
            Code = code,
            Name = command.Name.Trim(),
            LocationDescription = command.LocationDescription?.Trim(),
            PhoneNumber = command.PhoneNumber?.Trim()
        };

        await repository.AddAsync(stall, cancellationToken);
        await repository.AddInventoryAsync(
            DomainInventory.Create(stall.Id, command.ActorUserId, businessClock.GetNow()),
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return StallDto.From(stall);
    }
}

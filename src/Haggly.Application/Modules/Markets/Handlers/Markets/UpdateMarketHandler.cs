using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Commands.Markets;
using Haggly.Application.Modules.Markets.Dtos.Markets;
using Haggly.Application.Modules.Markets.Exceptions.Markets;
using Haggly.Application.Modules.Markets.Validation.Markets;
using MediatR;

namespace Haggly.Application.Modules.Markets.Handlers.Markets;

public sealed class UpdateMarketHandler(IMarketCommandRepository repository)
    : IRequestHandler<UpdateMarketCommand, MarketDto>
{
    public async Task<MarketDto> Handle(
        UpdateMarketCommand command,
        CancellationToken cancellationToken)
    {
        MarketValidation.Validate(command);

        var market = await repository.FindByIdAsync(command.Id, cancellationToken)
            ?? throw new MarketNotFoundException("The market was not found.");
        var code = command.Code.Trim();

        if (await repository.CodeExistsAsync(code, command.Id, cancellationToken))
            throw new MarketConflictException("A market with this code already exists.");

        market.Code = code;
        market.Name = command.Name.Trim();
        market.Address = command.Address.Trim();
        market.Latitude = command.Latitude;
        market.Longitude = command.Longitude;
        market.OpeningTime = command.OpeningTime;
        market.ClosingTime = command.ClosingTime;
        market.Status = command.Status;
        market.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);

        return MarketDto.From(market);
    }
}

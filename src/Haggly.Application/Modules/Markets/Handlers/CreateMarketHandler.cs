using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Commands;
using Haggly.Application.Modules.Markets.Dtos;
using Haggly.Application.Modules.Markets.Exceptions;
using Haggly.Application.Modules.Markets.Validation;
using Haggly.Domain.Modules.Markets;
using MediatR;

namespace Haggly.Application.Modules.Markets.Handlers;

public sealed class CreateMarketHandler(IMarketCommandRepository repository)
    : IRequestHandler<CreateMarketCommand, MarketDto>
{
    public async Task<MarketDto> Handle(
        CreateMarketCommand command,
        CancellationToken cancellationToken)
    {
        MarketValidation.Validate(command);
        var code = command.Code.Trim();

        if (await repository.CodeExistsAsync(code, null, cancellationToken))
            throw new MarketConflictException("A market with this code already exists.");

        var market = new Market
        {
            Code = code,
            Name = command.Name.Trim(),
            Address = command.Address.Trim(),
            Latitude = command.Latitude,
            Longitude = command.Longitude,
            OpeningTime = command.OpeningTime,
            ClosingTime = command.ClosingTime
        };

        await repository.AddAsync(market, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return MarketDto.From(market);
    }
}

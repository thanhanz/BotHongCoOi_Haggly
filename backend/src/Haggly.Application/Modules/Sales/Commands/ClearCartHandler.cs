using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Application.Modules.Sales.Handlers;
using Haggly.Application.Modules.Sales.Validation;
using MediatR;

namespace Haggly.Application.Modules.Sales.Commands;

public sealed class ClearCartHandler(
    ICartCommandRepository repository,
    ICartQuery query,
    IBusinessClock businessClock)
    : IRequestHandler<ClearCartCommand, CartDto>
{
    public async Task<CartDto> Handle(
        ClearCartCommand command,
        CancellationToken cancellationToken)
    {
        CartValidation.Validate(command);
        var cart = await repository.FindByBuyerIdAsync(command.BuyerId, cancellationToken)
            ?? throw new CartNotFoundException("The buyer cart was not found.");

        cart.Clear(businessClock.GetNow());
        await repository.SaveChangesAsync(cancellationToken);
        return await CartHandlerHelpers.ReadAsync(query, command.BuyerId, cancellationToken);
    }
}

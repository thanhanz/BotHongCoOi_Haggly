using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Handlers;
using Haggly.Application.Modules.Sales.Validation;
using MediatR;

namespace Haggly.Application.Modules.Sales.Queries;

public sealed class GetCartHandler(ICartQuery query)
    : IRequestHandler<GetCartQuery, CartDto>
{
    public async Task<CartDto> Handle(
        GetCartQuery request,
        CancellationToken cancellationToken)
    {
        CartValidation.Validate(request);
        return await CartHandlerHelpers.ReadAsync(
            query,
            request.BuyerId,
            cancellationToken);
    }
}

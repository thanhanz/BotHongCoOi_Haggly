using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Domain.Modules.Payments;
using MediatR;

namespace Haggly.Application.Modules.Sales.Commands;

public sealed record CompletePosSaleCommand(
    Guid StallId,
    Guid ActorUserId,
    string ClientRequestId,
    IReadOnlyCollection<PosSaleLineInput> Items,
    PaymentMethodCode PaymentMethod = PaymentMethodCode.CASH,
    decimal? AmountPaid = null) : IRequest<PosSaleDto>;

public sealed record PosSaleLineInput(
    Guid InventoryItemId,
    decimal Quantity,
    long ExpectedInventoryVersion,
    long ExpectedProductStallVersion);

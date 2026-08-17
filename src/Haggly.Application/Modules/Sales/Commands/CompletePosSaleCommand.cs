using Haggly.Application.Modules.Sales.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Sales.Commands;

public sealed record CompletePosSaleCommand(
    Guid StallId,
    Guid ActorUserId,
    string ClientRequestId,
    IReadOnlyCollection<PosSaleLineInput> Items) : IRequest<PosSaleDto>;

public sealed record PosSaleLineInput(
    Guid InventoryItemId,
    decimal Quantity,
    long ExpectedInventoryVersion,
    long ExpectedProductStallVersion);

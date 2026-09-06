using Haggly.Application.Modules.Inventory.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Commands;

public sealed record AdjustInventoryCommand(
    Guid StallId,
    Guid InventoryItemId,
    Guid ActorUserId,
    decimal QuantityDelta,
    string Reason,
    long ExpectedVersion) : IRequest<InventoryItemDto>;

using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Domain.Modules.Inventory;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Queries;

public sealed record GetInventoryLedgerQuery(
    Guid StallId,
    Guid ownerId,
    DateOnly? BusinessDate,
    Guid? ListingId,
    InventoryTransactionType? TransactionType,
    int Page,
    int PageSize) : IRequest<PagedResult<InventoryLedgerDto>>;

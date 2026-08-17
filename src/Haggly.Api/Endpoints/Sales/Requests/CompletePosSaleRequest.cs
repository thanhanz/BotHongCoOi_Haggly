namespace Haggly.Api.Endpoints.Sales.Requests;

using Haggly.Domain.Modules.Payments;

public sealed record CompletePosSaleRequest(
    string ClientRequestId,
    IReadOnlyCollection<CompletePosSaleItemRequest> Items,
    PaymentMethodCode PaymentMethod = PaymentMethodCode.CASH,
    decimal? AmountPaid = null);

public sealed record CompletePosSaleItemRequest(
    Guid InventoryItemId,
    decimal Quantity,
    long ExpectedInventoryVersion,
    long ExpectedProductStallVersion);

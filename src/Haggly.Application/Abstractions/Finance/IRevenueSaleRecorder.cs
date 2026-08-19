namespace Haggly.Application.Abstractions.Finance;

public interface IRevenueSaleRecorder
{
    Task RecordCompletedPosSaleAsync(
        CompletedPosSaleRevenue revenue,
        CancellationToken cancellationToken);
}

public sealed record CompletedPosSaleRevenue(
    Guid SaleId,
    Guid StallId,
    decimal GrossAmount,
    DateTimeOffset OccurredAt);

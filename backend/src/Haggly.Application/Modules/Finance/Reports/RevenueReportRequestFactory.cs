using Haggly.Application.Modules.Finance.Exceptions;

namespace Haggly.Application.Modules.Finance.Reports;

internal static class RevenueReportRequestFactory
{
    private static readonly TimeSpan MaximumRange = TimeSpan.FromDays(366);

    public static VendorRevenueReportRequest CreateVendorRequest(
        DateTimeOffset now,
        DateTimeOffset? from,
        DateTimeOffset? to,
        SaleChannel? saleChannel,
        Guid? stallId)
    {
        ValidateOptionalId(stallId, "stall");
        var period = CreatePeriod(now, from, to, saleChannel);

        return new VendorRevenueReportRequest(
            period.From,
            period.To,
            period.SaleChannel,
            stallId);
    }

    public static AdminRevenueReportRequest CreateAdminRequest(
        DateTimeOffset now,
        DateTimeOffset? from,
        DateTimeOffset? to,
        SaleChannel? saleChannel,
        Guid? marketId,
        Guid? vendorId,
        Guid? stallId)
    {
        ValidateOptionalId(marketId, "market");
        ValidateOptionalId(vendorId, "vendor");
        ValidateOptionalId(stallId, "stall");
        var period = CreatePeriod(now, from, to, saleChannel);

        return new AdminRevenueReportRequest(
            period.From,
            period.To,
            period.SaleChannel,
            marketId,
            vendorId,
            stallId);
    }

    private static RevenuePeriod CreatePeriod(
        DateTimeOffset now,
        DateTimeOffset? from,
        DateTimeOffset? to,
        SaleChannel? saleChannel)
    {
        var utcNow = now.ToUniversalTime();
        var effectiveFrom = from?.ToUniversalTime()
            ?? new DateTimeOffset(utcNow.UtcDateTime.Date, TimeSpan.Zero);
        var effectiveTo = to?.ToUniversalTime() ?? utcNow;
        var effectiveSaleChannel = saleChannel ?? SaleChannel.ALL;

        if (!Enum.IsDefined(effectiveSaleChannel))
        {
            throw new RevenueReportValidationException("A valid sale channel is required.");
        }

        if (effectiveFrom > effectiveTo)
        {
            throw new RevenueReportValidationException(
                "The report start time must not be later than the end time.");
        }

        if (effectiveTo - effectiveFrom > MaximumRange)
        {
            throw new RevenueReportValidationException(
                "The report period must not exceed 366 days.");
        }

        return new RevenuePeriod(effectiveFrom, effectiveTo, effectiveSaleChannel);
    }

    private static void ValidateOptionalId(Guid? id, string name)
    {
        if (id == Guid.Empty)
        {
            throw new RevenueReportValidationException($"A valid {name} ID is required.");
        }
    }

    private sealed record RevenuePeriod(
        DateTimeOffset From,
        DateTimeOffset To,
        SaleChannel SaleChannel);
}

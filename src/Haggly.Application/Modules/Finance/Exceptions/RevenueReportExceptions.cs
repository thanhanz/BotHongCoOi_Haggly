namespace Haggly.Application.Modules.Finance.Exceptions;

public sealed class RevenueReportValidationException(string message) : Exception(message);

public sealed class RevenueReportNotFoundException(string message) : Exception(message);

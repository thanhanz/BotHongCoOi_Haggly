namespace Haggly.Application.Modules.Payments.Exceptions;

public sealed class PaymentNotFoundException(string message) : Exception(message);

public sealed class PaymentConflictException(string message) : Exception(message);

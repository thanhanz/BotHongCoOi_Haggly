namespace Haggly.Application.Modules.Sales.Exceptions;

public sealed class PosSaleValidationException(string message) : Exception(message);
public sealed class PosSaleNotFoundException(string message) : Exception(message);
public sealed class PosSaleForbiddenException(string message) : Exception(message);
public sealed class PosSaleConflictException(string message) : Exception(message);

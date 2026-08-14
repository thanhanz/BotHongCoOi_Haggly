namespace Haggly.Application.Modules.Catalog.Exceptions.ProductStalls;

public sealed class ProductStallValidationException(string message) : Exception(message);
public sealed class ProductStallNotFoundException(string message) : Exception(message);
public sealed class ProductStallConflictException(string message) : Exception(message);
public sealed class ProductStallForbiddenException(string message) : Exception(message);

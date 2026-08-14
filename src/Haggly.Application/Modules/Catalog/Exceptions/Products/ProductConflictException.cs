namespace Haggly.Application.Modules.Catalog.Exceptions.Products;

public sealed class ProductConflictException(string message) : Exception(message);

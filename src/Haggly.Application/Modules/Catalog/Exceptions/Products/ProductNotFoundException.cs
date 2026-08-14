namespace Haggly.Application.Modules.Catalog.Exceptions.Products;

public sealed class ProductNotFoundException(string message) : Exception(message);

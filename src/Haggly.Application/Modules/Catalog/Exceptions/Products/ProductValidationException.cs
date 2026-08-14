namespace Haggly.Application.Modules.Catalog.Exceptions.Products;

public sealed class ProductValidationException(string message) : Exception(message);

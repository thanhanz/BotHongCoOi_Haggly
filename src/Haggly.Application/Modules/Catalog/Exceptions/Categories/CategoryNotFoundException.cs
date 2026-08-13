namespace Haggly.Application.Modules.Catalog.Exceptions.Categories;

public sealed class CategoryNotFoundException(string message) : Exception(message);

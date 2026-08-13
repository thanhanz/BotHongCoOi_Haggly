namespace Haggly.Application.Modules.Catalog.Exceptions.Categories;

public sealed class CategoryConflictException(string message) : Exception(message);

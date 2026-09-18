namespace Haggly.Application.Modules.Discovery.Dtos;

public sealed record CanonicalIngredientResult(
    Guid Id,
    string Code,
    string Name,
    string? Category);

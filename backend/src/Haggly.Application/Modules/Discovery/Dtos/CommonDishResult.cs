namespace Haggly.Application.Modules.Discovery.Dtos;

public sealed record CommonDishResult(
    Guid DishId,
    string ExternalId,
    string Name,
    string? Category);

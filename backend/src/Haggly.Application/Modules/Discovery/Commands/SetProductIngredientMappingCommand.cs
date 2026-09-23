using MediatR;

namespace Haggly.Application.Modules.Discovery.Commands;

public sealed record SetProductIngredientMappingCommand(
    Guid ProductId,
    Guid? CanonicalIngredientId,
    string? Status) : IRequest;

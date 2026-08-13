using MediatR;

namespace Haggly.Application.Modules.Markets.Commands.Stalls;

public sealed record DeleteStallCommand(Guid Id) : IRequest<bool>;

using MediatR;

namespace Haggly.Application.Modules.Markets.Commands.Markets;

public sealed record DeleteMarketCommand(Guid Id) : IRequest<bool>;

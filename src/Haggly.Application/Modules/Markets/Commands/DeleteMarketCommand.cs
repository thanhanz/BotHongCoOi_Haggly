using MediatR;

namespace Haggly.Application.Modules.Markets.Commands;

public sealed record DeleteMarketCommand(Guid Id) : IRequest<bool>;

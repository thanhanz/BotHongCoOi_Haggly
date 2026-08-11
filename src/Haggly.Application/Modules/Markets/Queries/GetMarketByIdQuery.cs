using Haggly.Application.Modules.Markets.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Markets.Queries;

public sealed record GetMarketByIdQuery(Guid Id) : IRequest<MarketDto>;

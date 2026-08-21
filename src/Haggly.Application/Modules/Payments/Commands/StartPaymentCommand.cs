using Haggly.Application.Modules.Payments.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Payments.Commands;

public sealed record StartPaymentCommand(Guid OrderId) : IRequest<PaymentDto>;

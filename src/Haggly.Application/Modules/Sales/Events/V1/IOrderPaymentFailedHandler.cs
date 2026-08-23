using Haggly.Application.Common.Messaging;
using Haggly.Application.Modules.Payments.Events.V1;

namespace Haggly.Application.Modules.Sales.Events.V1;

public interface IOrderPaymentFailedHandler
    : IDomainEventConsumer<PaymentFailedEvent>;

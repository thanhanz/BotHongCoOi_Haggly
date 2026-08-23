using Haggly.Application.Common.Messaging;
using Haggly.Application.Modules.Payments.Events.V1;

namespace Haggly.Application.Modules.Finance.Events.V1;

public interface IFinancePaymentSucceededHandler
    : IDomainEventConsumer<PaymentSucceededEvent>;

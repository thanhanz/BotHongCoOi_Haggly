using Haggly.Application.Common.Messaging;
using Haggly.Application.Modules.Payments.Events.V1;

namespace Haggly.Application.Modules.Inventory.Events.V1;

public interface IInventoryPaymentSucceededHandler
    : IDomainEventConsumer<PaymentSucceeded>;

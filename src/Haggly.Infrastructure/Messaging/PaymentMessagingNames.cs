namespace Haggly.Infrastructure.Messaging;

public static class PaymentMessagingNames
{
    public const string PaymentRequestedExchange = "payments.payment-requested.v1";
    public const string PaymentSucceededExchange = "payments.payment-succeeded.v1";
    public const string PaymentFailedExchange = "payments.payment-failed.v1";

    public const string PaymentRequestedQueue = "payments-payment-requested-v1";
    public const string PaymentRequestedErrorQueue = PaymentRequestedQueue + "_error";

    public const string FinancePaymentSucceededQueue = "finance-payment-succeeded-v1";
    public const string FinancePaymentSucceededErrorQueue = FinancePaymentSucceededQueue + "_error";

    public const string InventoryPaymentSucceededQueue = "inventory-payment-succeeded-v1";
    public const string InventoryPaymentSucceededErrorQueue = InventoryPaymentSucceededQueue + "_error";

    public const string OrderPaymentSucceededQueue = "order-payment-succeeded-v1";
    public const string OrderPaymentSucceededErrorQueue = OrderPaymentSucceededQueue + "_error";

    public const string PaymentProcessingFaultsQueue = "payment-processing-faults-v1";
}

namespace Haggly.Domain.Modules.Payments;

public enum PaymentMethodCode
{
    Cash,
    BankTransfer,
    Momo,
    ZaloPay,
    VnPay
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Paid,
    PartiallyPaid,
    Failed,
    Refunded,
    Cancelled
}

public enum PaymentTransactionType
{
    Payment,
    Refund
}

public enum PaymentTransactionStatus
{
    Pending,
    Success,
    Failed,
    Cancelled
}

public enum PaymentAllocationType
{
    Payment,
    Refund
}

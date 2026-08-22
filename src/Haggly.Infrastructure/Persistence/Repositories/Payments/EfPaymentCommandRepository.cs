using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Modules.Payments.Exceptions;
using Haggly.Domain.Modules.Payments;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Haggly.Infrastructure.Persistence.Repositories.Payments;

public sealed class EfPaymentCommandRepository(HagglyDbContext dbContext)
    : IPaymentCommandRepository
{
    public Task<Payment?> FindByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken)
        => dbContext.Payments.SingleOrDefaultAsync(
            payment => payment.Id == paymentId,
            cancellationToken);

    public Task<PaymentOrderSnapshot?> FindOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken)
        => dbContext.Orders
            .Where(order => order.Id == orderId)
            .Select(order => new PaymentOrderSnapshot(
                order.Id,
                order.BuyerId,
                order.Status,
                order.TotalToCharge,
                order.Currency))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Payment?> FindByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken)
        => dbContext.Payments.SingleOrDefaultAsync(
            payment => payment.OrderId == orderId,
            cancellationToken);

    public Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        dbContext.Payments.Add(payment);
        return Task.CompletedTask;
    }

    public Task AddTransactionAsync(
        PaymentTransaction transaction,
        CancellationToken cancellationToken)
    {
        dbContext.PaymentTransactions.Add(transaction);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException postgres
                && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new PaymentConflictException("A payment already exists for this order.");
        }
    }
}

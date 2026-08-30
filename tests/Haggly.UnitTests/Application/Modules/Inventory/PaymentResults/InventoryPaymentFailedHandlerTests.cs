using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Inventory.Events.V1;
using Haggly.Application.Modules.Payments.Events.V1;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Inventory.PaymentResults;

public sealed class InventoryPaymentFailedHandlerTests
{
    private readonly IInboxRepository _inbox = Substitute.For<IInboxRepository>();
    private readonly IInventoryPaymentRepository _inventory = Substitute.For<IInventoryPaymentRepository>();
    private readonly IInventoryUnitOfWork _unitOfWork = Substitute.For<IInventoryUnitOfWork>();
    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task HandleAsync_NewPaymentFailure_ReleasesReservedInventory()
    {
        // Arrange
        var message = CreateMessage();
        _inbox.TryAddAsync(Arg.Any<string>(), message.EventId, Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(true);
        _clock.GetNow().Returns(Now.AddMinutes(1));
        ConfigureTransaction();

        // Act
        await CreateSubject().HandleAsync(message, CancellationToken.None);

        // Assert
        await _inventory.Received(1).ReleaseAsync(message.OrderId, message.OccurredAt, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicatePaymentFailure_DoesNotReleaseInventory()
    {
        // Arrange
        var message = CreateMessage();
        _inbox.TryAddAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(false);
        ConfigureTransaction();

        // Act
        await CreateSubject().HandleAsync(message, CancellationToken.None);

        // Assert
        await _inventory.DidNotReceive().ReleaseAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    private InventoryPaymentFailedHandler CreateSubject() => new(_inbox, _inventory, _unitOfWork, _clock);
    private void ConfigureTransaction() => _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Func<CancellationToken, Task<bool>>>()(ci.ArgAt<CancellationToken>(1)));
    private static PaymentFailedEvent CreateMessage() => new(Guid.Parse("E2000000-0000-0000-0000-000000000001"), Guid.Parse("E2000000-0000-0000-0000-000000000002"), Now, Guid.Parse("E2000000-0000-0000-0000-000000000003"), Guid.Parse("E2000000-0000-0000-0000-000000000004"), Guid.Parse("E2000000-0000-0000-0000-000000000005"), 150_000m, "VND", "declined");
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 14, 0, 0, TimeSpan.Zero);
}

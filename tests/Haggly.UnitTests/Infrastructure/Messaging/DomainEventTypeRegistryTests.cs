using Haggly.Domain.Common.Events.V1;
using Haggly.Infrastructure.Messaging.Serialization;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Messaging;

public sealed class DomainEventTypeRegistryTests
{
    [Fact]
    public void GetEventType_WhenEventTypeIsRegistered_ReturnsStableName()
    {
        var registry = CreateRegistry();

        var eventType = registry.GetEventType(typeof(TestDomainEvent));

        Assert.Equal("tests.domain-event.v1", eventType);
    }

    [Fact]
    public void GetClrType_WhenEventNameIsRegistered_ReturnsClrType()
    {
        var registry = CreateRegistry();

        var clrType = registry.GetClrType("tests.domain-event.v1");

        Assert.Equal(typeof(TestDomainEvent), clrType);
    }

    [Fact]
    public void GetEventType_WhenEventTypeIsNotRegistered_ThrowsInvalidOperationException()
    {
        var registry = new DomainEventTypeRegistry([]);

        Assert.Throws<InvalidOperationException>(() =>
            registry.GetEventType(typeof(TestDomainEvent)));
    }

    [Fact]
    public void Constructor_WhenStableNameIsRegisteredTwice_ThrowsArgumentException()
    {
        var registrations = new[]
        {
            DomainEventTypeRegistration.For<TestDomainEvent>("tests.domain-event.v1"),
            DomainEventTypeRegistration.For<SecondTestDomainEvent>("tests.domain-event.v1")
        };

        Assert.Throws<ArgumentException>(() => new DomainEventTypeRegistry(registrations));
    }

    private static DomainEventTypeRegistry CreateRegistry()
        => new(
        [
            DomainEventTypeRegistration.For<TestDomainEvent>("tests.domain-event.v1")
        ]);

    private sealed record TestDomainEvent(
        Guid EventId,
        Guid CorrelationId,
        DateTimeOffset OccurredAt) : IDomainEvent;

    private sealed record SecondTestDomainEvent(
        Guid EventId,
        Guid CorrelationId,
        DateTimeOffset OccurredAt) : IDomainEvent;
}

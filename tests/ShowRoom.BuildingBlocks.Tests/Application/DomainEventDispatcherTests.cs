using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Domain.Primitives;
using ShowRoom.BuildingBlocks.Persistence;
using Xunit;

namespace ShowRoom.BuildingBlocks.Tests.Application;

public sealed class DomainEventDispatcherTests
{
    private sealed record TestEvent(Guid Id, string Payload) : DomainEvent(Id);

    private sealed class RecordingHandler(List<string> seen) : IDomainEventHandler<TestEvent>
    {
        public Task Handle(TestEvent domainEvent, CancellationToken cancellationToken)
        {
            seen.Add(domainEvent.Payload);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IDomainEventHandler<TestEvent>
    {
        public Task Handle(TestEvent domainEvent, CancellationToken cancellationToken)
            => throw new InvalidOperationException("boom");
    }

    [Fact]
    public async Task DispatchAsync_invokes_every_registered_handler_for_the_event()
    {
        // Arrange
        var seen = new List<string>();
        using var provider = new ServiceCollection()
            .AddSingleton(seen)
            .AddScoped<IDomainEventHandler<TestEvent>, RecordingHandler>()
            .AddLogging()
            .AddDomainEventDispatch()
            .BuildServiceProvider();

        var sut = provider.GetRequiredService<IDomainEventDispatcher>();

        // Act
        await sut.DispatchAsync([new TestEvent(Guid.NewGuid(), "hello")], CancellationToken.None);

        // Assert
        seen.Should().ContainSingle().Which.Should().Be("hello");
    }

    [Fact]
    public async Task DispatchAsync_is_best_effort_and_swallows_handler_failures()
    {
        // Arrange
        using var provider = new ServiceCollection()
            .AddScoped<IDomainEventHandler<TestEvent>, ThrowingHandler>()
            .AddLogging()
            .AddDomainEventDispatch()
            .BuildServiceProvider();

        var sut = provider.GetRequiredService<IDomainEventDispatcher>();

        // Act
        var act = async () => await sut.DispatchAsync([new TestEvent(Guid.NewGuid(), "x")], CancellationToken.None);

        // Assert — a failing reaction must not bubble (the transaction is already committed)
        await act.Should().NotThrowAsync();
    }
}

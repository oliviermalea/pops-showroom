using AwesomeAssertions;
using ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;
using Xunit;

namespace ShowRoom.Modules.Customer.Tests.Features.GetCustomerWithOrders;

/// <summary>
/// Guardrails on the read path's time budget: this AMQP round-trip serves an HTTP request, so its
/// worst case is latency the user actually waits through before the degraded answer.
/// </summary>
public sealed class OrderHistoryRetryPolicyTests
{
    /// <summary>
    /// The ceiling that matters: a caller must learn the order history is unavailable in a few
    /// seconds, not after a quarter of a minute.
    /// </summary>
    private static readonly TimeSpan AcceptableWorstCase = TimeSpan.FromSeconds(7);

    [Fact]
    public void The_worst_case_budget_stays_within_the_acceptable_ceiling()
    {
        // Arrange
        var sut = OrderHistoryRetryPolicy.WorstCaseBudget;

        // Act
        var exceeded = sut > AcceptableWorstCase;

        // Assert
        exceeded.Should().BeFalse(
            "the degraded answer must reach the caller in {0} at most (measured: {1})",
            AcceptableWorstCase,
            sut);
    }

    [Fact]
    public void One_cold_start_retry_is_kept()
    {
        // Arrange
        var sut = OrderHistoryRetryPolicy.MaxAttempts;

        // Act & Assert — a single attempt would surface every cold start as a false degradation,
        // more attempts would blow the latency budget.
        sut.Should().Be(2);
    }

    [Fact]
    public void The_warm_path_attempt_fails_fast_and_the_retry_gets_more_room()
    {
        // Arrange
        var first = OrderHistoryRetryPolicy.TimeoutFor(1);

        // Act
        var retry = OrderHistoryRetryPolicy.TimeoutFor(2);

        // Assert
        first.Should().BeLessThan(retry);
        first.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void The_budget_is_the_sum_of_every_attempt_and_backoff()
    {
        // Arrange
        var expected = OrderHistoryRetryPolicy.TimeoutFor(1)
            + OrderHistoryRetryPolicy.BackoffAfter(1)
            + OrderHistoryRetryPolicy.TimeoutFor(2);

        // Act
        var sut = OrderHistoryRetryPolicy.WorstCaseBudget;

        // Assert
        sut.Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void An_attempt_number_below_one_is_rejected(int attempt)
    {
        // Arrange
        var timeout = () => OrderHistoryRetryPolicy.TimeoutFor(attempt);

        // Act
        var backoff = () => OrderHistoryRetryPolicy.BackoffAfter(attempt);

        // Assert
        timeout.Should().Throw<ArgumentOutOfRangeException>();
        backoff.Should().Throw<ArgumentOutOfRangeException>();
    }
}

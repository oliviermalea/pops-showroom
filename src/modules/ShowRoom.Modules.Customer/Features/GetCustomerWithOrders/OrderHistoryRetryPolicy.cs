namespace ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;

/// <summary>
/// Time budget of the AMQP request/reply used to read the order history.
///
/// <para>This is a <b>read</b> path serving an HTTP request: its worst case is user-visible latency.
/// The budget is therefore explicit rather than left to Wolverine's 5s default timeout multiplied by a
/// retry count — that combination reached ~16s before a caller could learn the history was
/// unavailable, which is far too long for a page to hang.</para>
///
/// <para>What the retry actually buys is <b>cold start</b> absorption: the very first request after
/// startup provisions the RabbitMQ connection and the reply queue lazily, and can miss a short
/// deadline; the next attempt hits a warm path and returns in milliseconds. So one retry is kept —
/// with an escalating deadline: short on the warm-path attempt, longer on the one that has to absorb
/// provisioning.</para>
/// </summary>
internal static class OrderHistoryRetryPolicy
{
    /// <summary>Total number of attempts (one initial call plus one cold-start retry).</summary>
    internal const int MaxAttempts = 2;

    private static readonly TimeSpan FirstAttemptTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RetryTimeout = TimeSpan.FromSeconds(4);

    /// <summary>Deadline of the given 1-based attempt.</summary>
    internal static TimeSpan TimeoutFor(int attempt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attempt, 1);

        return attempt == 1 ? FirstAttemptTimeout : RetryTimeout;
    }

    /// <summary>Pause before the attempt following the given 1-based attempt.</summary>
    internal static TimeSpan BackoffAfter(int attempt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attempt, 1);

        return TimeSpan.FromMilliseconds(200 * attempt);
    }

    /// <summary>
    /// Worst case a caller can wait before the degraded answer: every attempt timing out, plus the
    /// backoffs between them. Kept as a property so the guardrail test can assert on it.
    /// </summary>
    internal static TimeSpan WorstCaseBudget
    {
        get
        {
            var budget = TimeSpan.Zero;

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                budget += TimeoutFor(attempt);

                if (attempt < MaxAttempts)
                {
                    budget += BackoffAfter(attempt);
                }
            }

            return budget;
        }
    }
}

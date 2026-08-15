using ShowRoom.Modules.Customer.Contracts.Messaging;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

namespace ShowRoom.Modules.Order.Features.OnCustomerRegistered;

/// <summary>
/// This slice's Wolverine routing, applied automatically as an <see cref="IWolverineExtension"/>: listen on
/// the Customer service's RabbitMQ queue for <see cref="CustomerRegisteredIntegrationEvent"/> with a
/// <b>durable inbox</b> (persisted on receipt, survives crashes) and handle it with
/// <see cref="CustomerRegisteredHandler"/>.
///
/// <para>Retry/dead-letter policy: a poison message (<see cref="UnprocessableCustomerRegisteredException"/>)
/// is retried a few times with a cooldown and, if it still fails, moved to the Wolverine dead-letter store
/// (<c>wolverine_business.wolverine_dead_letters</c>) instead of being retried forever. The policy is scoped
/// by exception type, so the request/reply <c>GetOrdersForCustomer</c> path is unaffected.</para>
/// </summary>
internal sealed class OnCustomerRegisteredMessaging : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.ListenToRabbitQueue(CustomerMessagingContract.CustomerRegisteredQueue).UseDurableInbox();
        options.Discovery.IncludeType(typeof(CustomerRegisteredHandler));

        options.OnException<UnprocessableCustomerRegisteredException>()
            .RetryWithCooldown(
                TimeSpan.FromMilliseconds(50),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(250))
            .Then.MoveToErrorQueue();
    }
}

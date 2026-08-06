using ShowRoom.Modules.Order.Contracts.Messaging;
using ShowRoom.Modules.Order.Persistence;
using Wolverine;
using Wolverine.RabbitMQ;

namespace ShowRoom.Modules.Order.Features.GetOrdersForCustomer;

/// <summary>
/// This slice's Wolverine routing, applied automatically as an <see cref="IWolverineExtension"/>: listen
/// on the Order query queue and handle <see cref="GetOrdersForCustomer"/> with
/// <see cref="GetOrdersForCustomerMessageHandler"/>. EF's <c>OrdersContext</c> is routed through the
/// service locator (Wolverine 6 forbids implicit service location by default). The transport itself is
/// configured centrally by <c>ConfigureShowRoomMessaging</c>.
/// </summary>
internal sealed class GetOrdersForCustomerMessaging : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.CodeGeneration.AlwaysUseServiceLocationFor<OrdersContext>();

        options.ListenToRabbitQueue(OrderMessagingContract.GetOrdersForCustomerQueue);

        options.Discovery.IncludeType(typeof(GetOrdersForCustomerMessageHandler));
    }
}

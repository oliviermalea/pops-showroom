using AwesomeAssertions;
using ShowRoom.Web.Features.Customer.CustomerOrders;
using ShowRoom.Web.Infrastructure.Api.Refit.Customer.Models;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Customer.CustomerOrders;

public sealed class CustomerOrdersMapperTests
{
    private const string CustomerPublicId = "cus_0123456789abcdef0123456789abcdef";

    [Fact]
    public void FromApi_maps_the_customer_and_its_orders()
    {
        // Arrange
        var response = CreateResponse(ordersAvailable: true, orders: [CreateOrder()]);

        // Act
        var sut = CustomerOrdersMapper.FromApi(response);

        // Assert
        sut.PublicId.Should().Be(CustomerPublicId);
        sut.DisplayName.Should().Be("Ada Lovelace");
        sut.Email.Should().Be("ada@showroom.test");
        sut.OrdersAvailable.Should().BeTrue();
        sut.HasOrders.Should().BeTrue();
        sut.IsEmpty.Should().BeFalse();
        sut.Orders.Should().ContainSingle();
    }

    [Fact]
    public void FromApi_formats_amounts_in_french_with_the_iso_currency_code()
    {
        // Arrange
        var order = CreateOrder() with { TotalAmount = 1234.5m, Currency = "eur" };
        var response = CreateResponse(ordersAvailable: true, orders: [order]);

        // Act
        var sut = CustomerOrdersMapper.FromApi(response);

        // Assert — le séparateur de milliers fr-FR dépend de la version d'ICU (espace fine insécable
        // ou espace insécable) : on n'assert donc pas ce caractère, seulement le reste du format.
        sut.Orders[0].TotalAmount.Should().StartWith("1").And.EndWith("234,50 EUR");
        sut.Orders[0].Lines[0].UnitPrice.Should().Be("12,50 EUR");
        sut.Orders[0].Lines[0].LineTotal.Should().Be("25,00 EUR");
    }

    [Fact]
    public void FromApi_propagates_the_order_currency_down_to_every_line()
    {
        // Arrange
        var order = CreateOrder() with { Currency = "USD" };
        var response = CreateResponse(ordersAvailable: true, orders: [order]);

        // Act
        var sut = CustomerOrdersMapper.FromApi(response);

        // Assert
        sut.Orders[0].Lines.Should().OnlyContain(line => line.UnitPrice.EndsWith("USD"));
    }

    [Fact]
    public void FromApi_sums_the_quantities_into_the_item_count()
    {
        // Arrange
        var order = CreateOrder() with
        {
            Lines =
            [
                CreateLine("prd_1", "Clavier", quantity: 2),
                CreateLine("prd_2", "Souris", quantity: 3),
            ],
        };
        var response = CreateResponse(ordersAvailable: true, orders: [order]);

        // Act
        var sut = CustomerOrdersMapper.FromApi(response);

        // Assert
        sut.Orders[0].ItemCount.Should().Be("5 articles");
    }

    [Fact]
    public void A_single_item_is_written_in_the_singular()
    {
        // Arrange
        var order = CreateOrder() with { Lines = [CreateLine("prd_1", "Clavier", quantity: 1)] };
        var response = CreateResponse(ordersAvailable: true, orders: [order]);

        // Act
        var sut = CustomerOrdersMapper.FromApi(response);

        // Assert
        sut.Orders[0].ItemCount.Should().Be("1 article");
    }

    [Fact]
    public void FromApi_formats_the_order_date_like_the_other_screens()
    {
        // Arrange
        var order = CreateOrder() with { OrderDate = new DateTimeOffset(2026, 3, 9, 22, 45, 0, TimeSpan.Zero) };
        var response = CreateResponse(ordersAvailable: true, orders: [order]);

        // Act
        var sut = CustomerOrdersMapper.FromApi(response);

        // Assert
        sut.Orders[0].OrderDate.Should().Be("09 mars 2026");
    }

    /// <summary>
    /// The degraded case: the customer is known, the Order service was unreachable over the bus. It
    /// must NOT read as "this customer has no order".
    /// </summary>
    [Fact]
    public void An_unavailable_order_service_is_not_an_empty_history()
    {
        // Arrange
        var response = CreateResponse(ordersAvailable: false, orders: []);

        // Act
        var sut = CustomerOrdersMapper.FromApi(response);

        // Assert
        sut.OrdersAvailable.Should().BeFalse();
        sut.HasOrders.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.DisplayName.Should().Be("Ada Lovelace");
    }

    [Fact]
    public void A_customer_without_any_order_is_an_empty_history()
    {
        // Arrange
        var response = CreateResponse(ordersAvailable: true, orders: []);

        // Act
        var sut = CustomerOrdersMapper.FromApi(response);

        // Assert
        sut.OrdersAvailable.Should().BeTrue();
        sut.IsEmpty.Should().BeTrue();
        sut.HasOrders.Should().BeFalse();
    }

    [Fact]
    public void FromApi_rejects_a_null_response()
    {
        // Arrange
        CustomerWithOrdersResponse? response = null;

        // Act
        var act = () => CustomerOrdersMapper.FromApi(response!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private static CustomerWithOrdersResponse CreateResponse(
        bool ordersAvailable,
        IReadOnlyList<OrderHistoryLineResponse> orders)
        => new(
            PublicId: CustomerPublicId,
            FirstName: "Ada",
            LastName: "Lovelace",
            DisplayName: "Ada Lovelace",
            Email: "ada@showroom.test",
            Phone: null,
            Status: "Active",
            RegisteredOn: new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero),
            OrdersAvailable: ordersAvailable,
            Orders: orders);

    private static OrderHistoryLineResponse CreateOrder() => new(
        OrderPublicId: "ord_0123456789abcdef0123456789abcdef",
        Status: "Confirmed",
        Currency: "EUR",
        OrderDate: new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero),
        TotalAmount: 25m,
        Lines: [CreateLine("prd_0123456789abcdef0123456789abcdef", "Clavier", quantity: 2)]);

    private static OrderLineDetailResponse CreateLine(string productPublicId, string name, int quantity) => new(
        ProductPublicId: productPublicId,
        ProductName: name,
        Quantity: quantity,
        UnitPrice: 12.5m,
        LineTotal: 12.5m * quantity);
}

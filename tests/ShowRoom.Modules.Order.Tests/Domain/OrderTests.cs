using AwesomeAssertions;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Order.Domain;
using ShowRoom.SharedKernel.Currencies;
using Xunit;
using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

namespace ShowRoom.Modules.Order.Tests.Domain;

public sealed class OrderTests
{
    private static PublicId NewCustomerId() => PublicIdFactory.ForCustomer().Value;
    private static PublicId NewProductId() => PublicIdFactory.ForContentNode().Value;

    private static OrderLineDraft Line(int quantity = 2, decimal unitPrice = 10m)
        => new(NewProductId(), "Widget", quantity, unitPrice);

    [Fact]
    public void Create_returns_a_pending_order_with_a_prefixed_public_id_and_computed_total()
    {
        // Arrange
        var customerPublicId = NewCustomerId();
        var lines = new[] { Line(quantity: 2, unitPrice: 10m), Line(quantity: 1, unitPrice: 5.5m) };

        // Act
        var sut = OrderAggregate.Create(customerPublicId, "eur", lines, DateTimeOffset.UtcNow);

        // Assert
        sut.IsSuccess.Should().BeTrue();
        sut.Value.Status.Should().Be(OrderStatus.Pending);
        sut.Value.PublicId.Prefix.Should().Be("ord");
        sut.Value.CustomerPublicId.Should().Be(customerPublicId);
        sut.Value.Currency.Code.Should().Be("EUR");
        sut.Value.Lines.Should().HaveCount(2);
        sut.Value.TotalAmount.Should().Be(25.5m);
    }

    [Fact]
    public void Create_defaults_currency_to_eur_when_omitted()
    {
        var sut = OrderAggregate.Create(NewCustomerId(), currency: null, new[] { Line() }, DateTimeOffset.UtcNow);

        sut.IsSuccess.Should().BeTrue();
        sut.Value.Currency.Should().Be(Currency.Default);
    }

    [Fact]
    public void Create_fails_when_no_lines_are_provided()
    {
        var sut = OrderAggregate.Create(NewCustomerId(), "EUR", Array.Empty<OrderLineDraft>(), DateTimeOffset.UtcNow);

        sut.IsFailure.Should().BeTrue();
        sut.Errors.Should().Contain(OrderErrors.NoLines);
    }

    [Fact]
    public void Create_fails_when_a_line_has_a_non_positive_quantity()
    {
        var sut = OrderAggregate.Create(
            NewCustomerId(),
            "EUR",
            new[] { Line(quantity: 0, unitPrice: 10m) },
            DateTimeOffset.UtcNow);

        sut.IsFailure.Should().BeTrue();
        sut.Errors.Should().Contain(OrderErrors.InvalidQuantity);
    }

    [Fact]
    public void Create_fails_when_a_line_has_a_negative_unit_price()
    {
        var sut = OrderAggregate.Create(
            NewCustomerId(),
            "EUR",
            new[] { Line(quantity: 1, unitPrice: -1m) },
            DateTimeOffset.UtcNow);

        sut.IsFailure.Should().BeTrue();
        sut.Errors.Should().Contain(OrderErrors.InvalidUnitPrice);
    }

    [Fact]
    public void Restore_rehydrates_state_without_regenerating_identity_or_raising_events()
    {
        // Arrange — an order as it would come back from the store: known id/dates, an advanced status.
        var id = OrderId.FromGuid(Guid.CreateVersion7());
        var publicId = PublicIdFactory.ForOrder().Value;
        var customerPublicId = NewCustomerId();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-3);
        var updatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var line = OrderLine.Create(NewProductId(), "Kayak", 2, 10m).Value;

        // Act
        var sut = OrderAggregate.Restore(
            id,
            publicId,
            customerPublicId,
            Currency.Eur,
            OrderStatus.Paid,
            [line],
            createdAt,
            updatedAt);

        // Assert — identity & history preserved (not regenerated), lines rehydrated, no events raised.
        sut.Id.Should().Be(id);
        sut.PublicId.Should().Be(publicId);
        sut.CustomerPublicId.Should().Be(customerPublicId);
        sut.Currency.Should().Be(Currency.Eur);
        sut.Status.Should().Be(OrderStatus.Paid);
        sut.CreatedAt.Should().Be(createdAt);
        sut.UpdatedAt.Should().Be(updatedAt);
        sut.Lines.Should().ContainSingle();
        sut.TotalAmount.Should().Be(20m);
        sut.DomainEvents.Should().BeEmpty();
    }
}

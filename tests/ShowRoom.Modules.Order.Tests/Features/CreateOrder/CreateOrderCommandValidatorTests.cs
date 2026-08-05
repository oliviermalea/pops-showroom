using AwesomeAssertions;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Order.Features.CreateOrder;
using Xunit;

namespace ShowRoom.Modules.Order.Tests.Features.CreateOrder;

public sealed class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _sut = new();

    private static CreateOrderCommand ValidCommand() => new()
    {
        CustomerPublicId = PublicIdFactory.ForCustomer().Value.Value,
        Currency = "EUR",
        Lines =
        [
            new CreateOrderLine
            {
                ProductPublicId = PublicIdFactory.ForContentNode().Value.Value,
                ProductName = "Widget",
                Quantity = 2,
                UnitPrice = 9.99m,
            },
        ],
    };

    [Fact]
    public void Valid_command_passes_validation()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Fails_when_customer_public_id_is_malformed()
    {
        var command = ValidCommand() with { CustomerPublicId = "not-a-public-id" };

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Fails_when_there_are_no_lines()
    {
        var command = ValidCommand() with { Lines = [] };

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Fails_when_a_line_quantity_is_not_positive()
    {
        var command = ValidCommand() with
        {
            Lines =
            [
                new CreateOrderLine
                {
                    ProductPublicId = PublicIdFactory.ForContentNode().Value.Value,
                    ProductName = "Widget",
                    Quantity = 0,
                    UnitPrice = 9.99m,
                },
            ],
        };

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}

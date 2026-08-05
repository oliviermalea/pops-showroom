using AwesomeAssertions;
using ShowRoom.SharedKernel.Emails;
using ShowRoom.Modules.Customer.Features.CreateCustomer;
using Xunit;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Tests.Features.CreateCustomer.Assemblers;

public sealed class CreateCustomerAssemblerTests
{
    [Fact]
    public void From_returns_the_customer_public_id()
    {
        // Arrange
        var sut = CustomerAggregate.Create(
            "Grace",
            "Hopper",
            Email.Create("grace.hopper@example.com").Value,
            phone: null,
            createdAt: DateTimeOffset.UtcNow);

        // Act
        var publicId = CreateCustomerAssembler.From(sut);

        // Assert
        publicId.Should().Be(sut.PublicId);
        publicId.Prefix.Should().Be("cus");
    }
}

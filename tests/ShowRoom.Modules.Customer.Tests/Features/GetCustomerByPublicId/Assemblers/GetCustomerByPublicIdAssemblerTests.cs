using AwesomeAssertions;
using Bogus;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Customer.Domain;
using ShowRoom.SharedKernel.Emails;
using ShowRoom.SharedKernel.PhoneNumbers;
using ShowRoom.Modules.Customer.Features.GetCustomerByPublicId;
using Xunit;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Tests.Features.GetCustomerByPublicId.Assemblers;

public sealed class GetCustomerByPublicIdAssemblerTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void From_maps_all_properties_for_a_complete_customer()
    {
        // Arrange
        var publicId = PublicIdFactory.ForCustomer().Value;
        var createdAt = DateTimeOffset.UtcNow.AddDays(-10);
        var updatedAt = DateTimeOffset.UtcNow;
        var firstName = _faker.Name.FirstName();
        var lastName = _faker.Name.LastName();
        var email = _faker.Internet.Email();
        const string phone = "+33123456789";

        var sut = CustomerAggregate.Restore(
            CustomerId.FromGuid(Guid.CreateVersion7()),
            publicId,
            firstName,
            lastName,
            Email.Create(email).Value,
            PhoneNumber.Create(phone).Value,
            CustomerStatus.Active,
            createdAt,
            updatedAt);

        // Act
        var response = GetCustomerByPublicIdAssembler.From(sut);

        // Assert
        response.PublicId.Should().Be(publicId);
        response.FirstName.Should().Be(firstName);
        response.LastName.Should().Be(lastName);
        response.DisplayName.Should().Be($"{firstName} {lastName}");
        response.Email.Should().Be(email.Trim().ToLowerInvariant());
        response.Phone.Should().Be(phone);
        response.Status.Should().Be("Active");
        response.RegisteredOn.Should().Be(createdAt);
    }

    [Fact]
    public void From_maps_null_phone_when_absent()
    {
        // Arrange
        var sut = CustomerAggregate.Restore(
            CustomerId.FromGuid(Guid.CreateVersion7()),
            PublicIdFactory.ForCustomer().Value,
            "Ada",
            "Lovelace",
            Email.Create("ada@example.com").Value,
            phone: null,
            CustomerStatus.Inactive,
            DateTimeOffset.UtcNow,
            updatedAt: null);

        // Act
        var response = GetCustomerByPublicIdAssembler.From(sut);

        // Assert
        response.Phone.Should().BeNull();
        response.Status.Should().Be("Inactive");
        response.DisplayName.Should().Be("Ada Lovelace");
    }
}

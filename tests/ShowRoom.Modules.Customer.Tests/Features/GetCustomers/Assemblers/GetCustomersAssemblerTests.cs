using AwesomeAssertions;
using Bogus;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Customer.Domain;
using ShowRoom.Modules.Customer.Features.GetCustomers;
using ShowRoom.SharedKernel.Emails;
using ShowRoom.SharedKernel.PhoneNumbers;
using Xunit;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Tests.Features.GetCustomers.Assemblers;

public sealed class GetCustomersAssemblerTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void ToSummary_projects_the_compact_list_fields()
    {
        // Arrange
        var publicId = PublicIdFactory.ForCustomer().Value;
        var createdAt = DateTimeOffset.UtcNow.AddDays(-3);
        var firstName = _faker.Name.FirstName();
        var lastName = _faker.Name.LastName();
        var email = _faker.Internet.Email();

        var customer = CustomerAggregate.Restore(
            CustomerId.FromGuid(Guid.CreateVersion7()),
            publicId,
            firstName,
            lastName,
            Email.Create(email).Value,
            PhoneNumber.Create("+33123456789").Value,
            CustomerStatus.Active,
            createdAt,
            updatedAt: null);

        // Act
        var sut = GetCustomersAssembler.ToSummary(customer);

        // Assert
        sut.PublicId.Should().Be(publicId);
        sut.FirstName.Should().Be(firstName);
        sut.LastName.Should().Be(lastName);
        sut.DisplayName.Should().Be($"{firstName} {lastName}");
        sut.Email.Should().Be(email.Trim().ToLowerInvariant());
        sut.Status.Should().Be("Active");
        sut.RegisteredOn.Should().Be(createdAt);
    }

    [Fact]
    public void ToSummary_reflects_an_inactive_status()
    {
        // Arrange
        var customer = CustomerAggregate.Restore(
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
        var sut = GetCustomersAssembler.ToSummary(customer);

        // Assert
        sut.Status.Should().Be("Inactive");
        sut.DisplayName.Should().Be("Ada Lovelace");
    }
}

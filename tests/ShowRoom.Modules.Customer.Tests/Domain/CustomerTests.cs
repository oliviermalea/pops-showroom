using System.Linq;
using AwesomeAssertions;
using ShowRoom.Modules.Customer.Domain;
using ShowRoom.SharedKernel.Emails;
using ShowRoom.SharedKernel.PhoneNumbers;
using Xunit;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Tests.Domain;

public sealed class CustomerTests
{
    private static CustomerAggregate NewCustomer(string email)
        => CustomerAggregate.Create("Grace", "Hopper", Email.Create(email).Value, phone: null, DateTimeOffset.UtcNow);

    [Fact]
    public void ChangeEmail_updates_the_address_stamps_audit_and_raises_the_event()
    {
        // Arrange
        var sut = NewCustomer("old@example.com");
        var now = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        var result = sut.ChangeEmail(Email.Create("new@example.com").Value, now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        sut.Email.Value.Should().Be("new@example.com");
        sut.UpdatedAt.Should().Be(now);

        sut.DomainEvents.Should().ContainSingle(e => e is CustomerEmailChanged);
        var raised = sut.DomainEvents.OfType<CustomerEmailChanged>().Single();
        raised.PreviousEmail.Should().Be("old@example.com");
        raised.NewEmail.Should().Be("new@example.com");
        raised.PublicId.Should().Be(sut.PublicId);
    }

    [Fact]
    public void ChangeEmail_is_idempotent_when_the_address_is_unchanged()
    {
        // Arrange
        var sut = NewCustomer("same@example.com");

        // Act — same address (case-insensitively) is a no-op
        var result = sut.ChangeEmail(Email.Create("SAME@example.com").Value, DateTimeOffset.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        sut.UpdatedAt.Should().BeNull();       // nothing mutated
        sut.DomainEvents.Should().BeEmpty();   // nothing raised
    }

    [Fact]
    public void Rename_changes_the_name_stamps_audit_and_raises_the_event()
    {
        var sut = NewCustomer("x@example.com"); // Grace Hopper
        var now = DateTimeOffset.UtcNow.AddMinutes(1);

        var result = sut.Rename("Ada", "Lovelace", now);

        result.IsSuccess.Should().BeTrue();
        sut.DisplayName.Should().Be("Ada Lovelace");
        sut.UpdatedAt.Should().Be(now);
        sut.DomainEvents.Should().ContainSingle(e => e is CustomerRenamed);
    }

    [Fact]
    public void Rename_fails_when_a_name_is_blank()
    {
        var sut = NewCustomer("x@example.com");

        sut.Rename("", "Lovelace", DateTimeOffset.UtcNow).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Rename_is_idempotent_when_the_name_is_unchanged()
    {
        var sut = NewCustomer("x@example.com"); // Grace Hopper

        var result = sut.Rename("Grace", "Hopper", DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        sut.UpdatedAt.Should().BeNull();
        sut.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangePhone_sets_then_clears_the_number_raising_events()
    {
        var sut = NewCustomer("x@example.com"); // no phone
        var now = DateTimeOffset.UtcNow;

        sut.ChangePhone(PhoneNumber.Create("+33123456789").Value, now).IsSuccess.Should().BeTrue();
        sut.Phone!.Value.Should().Be("+33123456789");
        sut.DomainEvents.OfType<CustomerPhoneChanged>().Should().ContainSingle(e => e.NewPhone == "+33123456789");

        sut.ChangePhone(null, now).IsSuccess.Should().BeTrue();
        sut.Phone.Should().BeNull();
        sut.DomainEvents.OfType<CustomerPhoneChanged>().Last().NewPhone.Should().BeNull();
    }

    [Fact]
    public void ChangePhone_is_idempotent_when_unchanged()
    {
        var sut = NewCustomer("x@example.com"); // no phone

        var result = sut.ChangePhone(null, DateTimeOffset.UtcNow); // still no phone

        result.IsSuccess.Should().BeTrue();
        sut.UpdatedAt.Should().BeNull();
        sut.DomainEvents.Should().BeEmpty();
    }

    // ---- Style 2 (coarse) : UpdateProfile + single CustomerProfileUpdated event ----

    [Fact]
    public void UpdateProfile_applies_all_fields_and_raises_a_single_coarse_event()
    {
        var sut = NewCustomer("old@example.com"); // Grace Hopper, no phone
        var now = DateTimeOffset.UtcNow.AddMinutes(1);

        var result = sut.UpdateProfile(
            "Ada", "Lovelace",
            Email.Create("new@example.com").Value,
            PhoneNumber.Create("+33123456789").Value,
            now);

        result.IsSuccess.Should().BeTrue();
        sut.DisplayName.Should().Be("Ada Lovelace");
        sut.Email.Value.Should().Be("new@example.com");
        sut.Phone!.Value.Should().Be("+33123456789");
        sut.UpdatedAt.Should().Be(now);

        sut.DomainEvents.Should().ContainSingle(e => e is CustomerProfileUpdated); // ONE event, not three
        sut.DomainEvents.OfType<CustomerProfileUpdated>().Single()
            .ChangedFields.Should().BeEquivalentTo(["Name", "Email", "Phone"]);
    }

    [Fact]
    public void UpdateProfile_reports_only_the_fields_that_changed()
    {
        var sut = NewCustomer("grace@example.com"); // Grace Hopper

        var result = sut.UpdateProfile(
            "Grace", "Hopper", // unchanged
            Email.Create("new@example.com").Value, // changed
            phone: null,
            DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        sut.DomainEvents.OfType<CustomerProfileUpdated>().Single()
            .ChangedFields.Should().BeEquivalentTo(["Email"]);
    }

    [Fact]
    public void UpdateProfile_is_idempotent_when_nothing_changes()
    {
        var sut = NewCustomer("grace@example.com"); // Grace Hopper, no phone

        var result = sut.UpdateProfile(
            "Grace", "Hopper",
            Email.Create("grace@example.com").Value,
            phone: null,
            DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        sut.UpdatedAt.Should().BeNull();
        sut.DomainEvents.Should().BeEmpty();
    }
}

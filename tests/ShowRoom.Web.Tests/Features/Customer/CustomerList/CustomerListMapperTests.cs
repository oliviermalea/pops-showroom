using AwesomeAssertions;
using ShowRoom.Web.Features.Customer.CustomerList;
using ShowRoom.Web.Infrastructure.Api.Refit.Customer.Models;
using ShowRoom.Web.Infrastructure.Api.Refit.Models;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Customer.CustomerList;

public sealed class CustomerListMapperTests
{
    [Fact]
    public void FromApi_maps_the_page_metadata_and_every_item()
    {
        // Arrange
        var response = CreatePage(page: 2, pageSize: 20, totalItems: 45, totalPages: 3, items:
        [
            CreateSummary("cus_0123456789abcdef0123456789abcdef", "Ada", "Lovelace", "Active"),
            CreateSummary("cus_fedcba9876543210fedcba9876543210", "Grace", "Hopper", "Inactive"),
        ]);

        // Act
        var sut = CustomerListMapper.FromApi(response);

        // Assert
        sut.Page.Should().Be(2);
        sut.PageSize.Should().Be(20);
        sut.TotalItems.Should().Be(45);
        sut.TotalPages.Should().Be(3);
        sut.Items.Should().HaveCount(2);
        sut.Items[0].DisplayName.Should().Be("Ada Lovelace");
        sut.Items[0].IsActive.Should().BeTrue();
        sut.Items[1].IsActive.Should().BeFalse();
    }

    [Fact]
    public void FromApi_formats_the_registration_date_like_the_detail_screen()
    {
        // Arrange
        var summary = CreateSummary("cus_0123456789abcdef0123456789abcdef", "Ada", "Lovelace", "Active")
            with { RegisteredOn = new DateTimeOffset(2026, 3, 9, 22, 45, 0, TimeSpan.Zero) };
        var response = CreatePage(1, 20, 1, 1, [summary]);

        // Act
        var sut = CustomerListMapper.FromApi(response);

        // Assert
        sut.Items[0].RegisteredOn.Should().Be("09 mars 2026");
    }

    [Fact]
    public void FromApi_falls_back_on_first_and_last_name_when_display_name_is_empty()
    {
        // Arrange
        var summary = CreateSummary("cus_0123456789abcdef0123456789abcdef", "Ada", "Lovelace", "Active")
            with { DisplayName = "   " };
        var response = CreatePage(1, 20, 1, 1, [summary]);

        // Act
        var sut = CustomerListMapper.FromApi(response);

        // Assert
        sut.Items[0].DisplayName.Should().Be("Ada Lovelace");
    }

    [Fact]
    public void An_empty_page_exposes_no_navigation_and_no_index()
    {
        // Arrange
        var response = CreatePage(page: 1, pageSize: 20, totalItems: 0, totalPages: 0, items: []);

        // Act
        var sut = CustomerListMapper.FromApi(response);

        // Assert
        sut.IsEmpty.Should().BeTrue();
        sut.HasPrevious.Should().BeFalse();
        sut.HasNext.Should().BeFalse();
        sut.FirstItemIndex.Should().Be(0);
        sut.LastItemIndex.Should().Be(0);
    }

    [Theory]
    [InlineData(1, 3, false, true)]
    [InlineData(2, 3, true, true)]
    [InlineData(3, 3, true, false)]
    [InlineData(1, 1, false, false)]
    public void Navigation_state_is_derived_from_the_page_numbers(
        int page,
        int totalPages,
        bool hasPrevious,
        bool hasNext)
    {
        // Arrange
        var response = CreatePage(page, pageSize: 20, totalItems: totalPages * 20, totalPages, items:
        [
            CreateSummary("cus_0123456789abcdef0123456789abcdef", "Ada", "Lovelace", "Active"),
        ]);

        // Act
        var sut = CustomerListMapper.FromApi(response);

        // Assert
        sut.HasPrevious.Should().Be(hasPrevious);
        sut.HasNext.Should().Be(hasNext);
    }

    [Fact]
    public void Item_indexes_describe_the_slice_shown_on_the_current_page()
    {
        // Arrange — page 3 of 20-item pages, holding 5 items: 41 to 45 out of 45.
        var items = Enumerable.Range(0, 5)
            .Select(i => CreateSummary($"cus_{i:d32}", "Ada", $"Lovelace{i}", "Active"))
            .ToArray();
        var response = CreatePage(page: 3, pageSize: 20, totalItems: 45, totalPages: 3, items);

        // Act
        var sut = CustomerListMapper.FromApi(response);

        // Assert
        sut.FirstItemIndex.Should().Be(41);
        sut.LastItemIndex.Should().Be(45);
    }

    [Fact]
    public void FromApi_rejects_a_null_response()
    {
        // Arrange
        PagedResponse<CustomerSummaryResponse>? response = null;

        // Act
        var act = () => CustomerListMapper.FromApi(response!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private static PagedResponse<CustomerSummaryResponse> CreatePage(
        int page,
        int pageSize,
        int totalItems,
        int totalPages,
        IReadOnlyList<CustomerSummaryResponse> items)
        => new(items, page, pageSize, totalItems, totalPages);

    private static CustomerSummaryResponse CreateSummary(
        string publicId,
        string firstName,
        string lastName,
        string status)
        => new(
            PublicId: publicId,
            FirstName: firstName,
            LastName: lastName,
            DisplayName: $"{firstName} {lastName}",
            Email: $"{firstName.ToLowerInvariant()}@showroom.test",
            Status: status,
            RegisteredOn: new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero));
}

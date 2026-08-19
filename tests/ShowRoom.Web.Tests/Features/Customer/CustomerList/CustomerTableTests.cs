using AwesomeAssertions;
using Bunit;
using ShowRoom.Web.Features.Customer.CustomerList;
using ShowRoom.Web.Tests.Doubles;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Customer.CustomerList;

public sealed class CustomerTableTests : BunitContext
{
    [Fact]
    public void Each_customer_is_a_row_linking_to_its_detail_page()
    {
        // Arrange
        var items = new[] { CustomerSamples.ListItem("1"), CustomerSamples.ListItem("2") };

        // Act
        var sut = Render<CustomerTable>(p => p.Add(x => x.Items, items));

        // Assert
        sut.FindAll("tbody tr").Should().HaveCount(2);
        sut.FindAll("tbody tr td a")[0].GetAttribute("href")
            .Should().Be($"/customers/{items[0].PublicId}");
        sut.Find("tbody tr").TextContent.Should().Contain("Ada Lovelace 1").And.Contain("ada1@showroom.test");
    }

    [Fact]
    public void The_table_is_readable_by_assistive_technology()
    {
        // Arrange & Act
        var sut = Render<CustomerTable>(p => p.Add(x => x.Items, [CustomerSamples.ListItem()]));

        // Assert — un intitulé, et des en-têtes de colonne explicitement portés par scope="col"
        sut.Find("caption").TextContent.Should().NotBeNullOrWhiteSpace();
        sut.FindAll("thead th[scope='col']").Should().HaveCount(4);
    }

    [Theory]
    [InlineData(true, "Active")]
    [InlineData(false, "Inactive")]
    public void The_status_of_a_customer_is_displayed_as_text_not_only_as_a_colour(bool active, string expected)
    {
        // Arrange & Act
        var sut = Render<CustomerTable>(p => p.Add(x => x.Items, [CustomerSamples.ListItem(active: active)]));

        // Assert
        sut.Find("tbody tr").TextContent.Should().Contain(expected);
    }

    [Fact]
    public void An_empty_page_renders_a_table_without_any_row()
    {
        // Arrange & Act
        var sut = Render<CustomerTable>(p => p.Add(x => x.Items, []));

        // Assert
        sut.FindAll("tbody tr").Should().BeEmpty();
    }
}

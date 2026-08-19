using AwesomeAssertions;
using Bunit;
using ShowRoom.Web.Features.Customer.CustomerOrders;
using ShowRoom.Web.Tests.Doubles;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Customer.CustomerOrders;

public sealed class CustomerOrderCardTests : BunitContext
{
    [Fact]
    public void The_order_header_carries_its_identity_date_status_and_total()
    {
        // Arrange & Act
        var sut = Render<CustomerOrderCard>(p => p.Add(x => x.Order, CustomerSamples.Order()));

        // Assert
        var header = sut.Find("header").TextContent;
        header.Should().Contain("ord_1")
            .And.Contain("01 février 2026")
            .And.Contain("Confirmed")
            .And.Contain("3 articles")
            .And.Contain("608,80 EUR");
    }

    [Fact]
    public void Every_product_line_is_detailed()
    {
        // Arrange & Act
        var sut = Render<CustomerOrderCard>(p => p.Add(x => x.Order, CustomerSamples.Order()));

        // Assert
        var rows = sut.FindAll("tbody tr");
        rows.Should().HaveCount(2);
        rows[0].TextContent.Should().Contain("Clavier mécanique").And.Contain("129,90 EUR").And.Contain("259,80 EUR");
        rows[1].TextContent.Should().Contain("Écran 27 pouces").And.Contain("349,00 EUR");
    }

    [Fact]
    public void The_lines_table_is_readable_by_assistive_technology()
    {
        // Arrange & Act
        var sut = Render<CustomerOrderCard>(p => p.Add(x => x.Order, CustomerSamples.Order()));

        // Assert
        sut.Find("caption").TextContent.Should().Contain("ord_1");
        sut.FindAll("thead th[scope='col']").Should().HaveCount(4);
    }
}

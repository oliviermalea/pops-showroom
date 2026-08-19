using AwesomeAssertions;
using Bunit;
using ShowRoom.Web.Features.Customer;
using ShowRoom.Web.Features.Customer.CustomerDetail;
using ShowRoom.Web.Tests.Doubles;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Customer.CustomerDetail;

public sealed class CustomerDetailCardTests : BunitContext
{
    [Fact]
    public void The_card_shows_every_field_of_the_customer()
    {
        // Arrange & Act
        var sut = Render<CustomerDetailCard>(p => p.Add(x => x.Customer, CustomerSamples.Detail()));

        // Assert
        var text = sut.Find("article").TextContent;
        text.Should().Contain("Ada Lovelace")
            .And.Contain(CustomerSamples.PublicId)
            .And.Contain("ada@showroom.test")
            .And.Contain("+33123456789")
            .And.Contain("15 janvier 2026");
    }

    [Fact]
    public void The_email_is_actionable()
    {
        // Arrange & Act
        var sut = Render<CustomerDetailCard>(p => p.Add(x => x.Customer, CustomerSamples.Detail()));

        // Assert
        sut.Find("a[href^='mailto:']").GetAttribute("href").Should().Be("mailto:ada@showroom.test");
    }

    [Fact]
    public void A_missing_phone_shows_the_placeholder_rather_than_an_empty_cell()
    {
        // Arrange
        var customer = CustomerSamples.Detail(phone: CustomerFormat.NotProvided);

        // Act
        var sut = Render<CustomerDetailCard>(p => p.Add(x => x.Customer, customer));

        // Assert
        sut.FindAll("dd").Select(dd => dd.TextContent.Trim())
            .Should().Contain(CustomerFormat.NotProvided);
    }

    [Fact]
    public void An_inactive_customer_is_distinguishable_by_text()
    {
        // Arrange & Act
        var sut = Render<CustomerDetailCard>(p => p.Add(x => x.Customer, CustomerSamples.Detail(active: false)));

        // Assert
        sut.Find("header").TextContent.Should().Contain("Inactive");
    }
}

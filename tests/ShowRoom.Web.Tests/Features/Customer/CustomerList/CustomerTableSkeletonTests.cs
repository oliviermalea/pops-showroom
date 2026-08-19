using AwesomeAssertions;
using Bunit;
using ShowRoom.Web.Features.Customer.CustomerList;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Customer.CustomerList;

public sealed class CustomerTableSkeletonTests : BunitContext
{
    [Fact]
    public void The_skeleton_announces_a_loading_table_to_assistive_technology()
    {
        // Arrange & Act
        var sut = Render<CustomerTableSkeleton>(parameters => parameters.Add(p => p.Rows, 5));

        // Assert
        var region = sut.Find("[role='status']");
        region.GetAttribute("aria-busy").Should().Be("true");
        region.GetAttribute("aria-live").Should().Be("polite");
        region.GetAttribute("aria-label").Should().Be("Chargement des clients");
    }
}

using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class DriverTests
    {
        [Fact]
        public void ShouldUseDefaultPortWhenPortNotSet()
        {
            using (var driver = GraphDatabase.Driver("bolt://localhost"))
            {
                driver.Uri.Port.Should().Be(7687);
                driver.Uri.Scheme.Should().Be("bolt");
                driver.Uri.Host.Should().Be("localhost");
            }
        }

        [Fact]
        public void ShouldUseSpecifiedPortWhenPortSet()
        {
            using (var driver = GraphDatabase.Driver("bolt://localhost:8888"))
            {
                driver.Uri.Port.Should().Be(8888);
                driver.Uri.Scheme.Should().Be("bolt");
                driver.Uri.Host.Should().Be("localhost");
            }
        }
    }
}

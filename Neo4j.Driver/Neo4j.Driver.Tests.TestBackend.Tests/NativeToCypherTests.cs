using FluentAssertions;
using Neo4j.Driver.Tests.TestBackend.Types;
using Xunit;

namespace Neo4j.Driver.Tests.TestBackend.Tests;

public class NativeToCypherTests
{
    [Fact]
    public void Convert_Bytes_ProducesSpaceSeparatedHex()
    {
        var bytes = new byte[] { 0x00, 0x33, 0x66, 0x99, 0xcc, 0xff };

        var result = (NativeToCypherObject)NativeToCypher.Convert(bytes);

        result.name.Should().Be("CypherBytes");
        ((NativeToCypherObject.DataType)result.data).value.Should().Be("00 33 66 99 cc ff");
    }
}

using System;
using FluentAssertions;
using Neo4j.Driver.Tests.TestBackend.Types;
using Newtonsoft.Json;
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

    [Theory]
    [InlineData(double.NaN, "NaN")]
    [InlineData(double.PositiveInfinity, "+Infinity")]
    [InlineData(double.NegativeInfinity, "-Infinity")]
    public void Convert_SpecialDouble_ProducesStringValue(double input, string expectedValue)
    {
        var result = (NativeToCypherObject)NativeToCypher.Convert(input);

        result.name.Should().Be("CypherFloat");
        ((NativeToCypherObject.DataType)result.data).value.Should().Be(expectedValue);
        Action serialize = () => JsonConvert.SerializeObject(result);
        serialize.Should().NotThrow();
    }
}

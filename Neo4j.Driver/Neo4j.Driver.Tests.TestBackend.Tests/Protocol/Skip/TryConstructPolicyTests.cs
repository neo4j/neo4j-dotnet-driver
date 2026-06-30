using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Tests.TestBackend.Protocol.Skip;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Neo4j.Driver.Tests.TestBackend.Tests.Protocol.Skip;

public class TryConstructPolicyTests
{
    [Fact]
    public void Overall_IsRunSubtests()
    {
        new TryConstructPolicy().Overall.Should().BeOfType<TestDisposition.RunSubtests>();
    }

    [Fact]
    public void ShouldRunSubtest_RunsConstructibleValue()
    {
        var run = new TryConstructPolicy().ShouldRunSubtest(CypherTimeWithOffset(3661), out var reason);

        run.Should().BeTrue();
        reason.Should().BeEmpty();
    }

    [Fact]
    public void ShouldRunSubtest_SkipsOutOfRangeOffset_WithRealReason()
    {
        var run = new TryConstructPolicy().ShouldRunSubtest(CypherTimeWithOffset(-86400), out var reason);

        run.Should().BeFalse();
        reason.Should().Contain("64800");
    }

    [Fact]
    public void DefaultRegistry_UsesTryConstructForZonedTime()
    {
        TestSkipPolicies.Default.GetPolicy("tests.stub.http_query.datatypes.test_temporal.TestTemporal.test_zoned_time")
            .Overall.Should().BeOfType<TestDisposition.RunSubtests>();
    }

    [Fact]
    public void ShouldRunSubtest_SkipsUnconstructibleValue()
    {
        var parameters = new Dictionary<string, JToken>
        {
            ["x"] = JObject.Parse(@"{""name"":""CypherNotARealType"",""data"":{""value"":1}}")
        };

        var run = new TryConstructPolicy().ShouldRunSubtest(parameters, out var reason);

        run.Should().BeFalse();
        reason.Should().NotBeEmpty();
    }

    private static IReadOnlyDictionary<string, JToken> CypherTimeWithOffset(int utcOffsetSeconds)
    {
        return new Dictionary<string, JToken>
        {
            ["x"] = new JObject
            {
                ["name"] = "CypherTime",
                ["data"] = new JObject
                {
                    ["hour"] = 0,
                    ["minute"] = 0,
                    ["second"] = 0,
                    ["nanosecond"] = 0,
                    ["utc_offset_s"] = utcOffsetSeconds
                }
            }
        };
    }
}

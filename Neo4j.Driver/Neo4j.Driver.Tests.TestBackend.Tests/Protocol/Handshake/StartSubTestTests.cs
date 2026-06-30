using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Tests.TestBackend.Protocol.Handshake;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Neo4j.Driver.Tests.TestBackend.Tests.Protocol.Handshake;

public class StartSubTestTests
{
    [Fact]
    public void Respond_RunsSubtest_ForUnlistedTest()
    {
        var startSubTest = new StartSubTest
        {
            data = new StartSubTest.StartSubTestType
            {
                testName = "some.unlisted.test_name",
                subtestArguments = new Dictionary<string, JToken>()
            }
        };

        JObject.Parse(startSubTest.Respond())["name"].Value<string>().Should().Be("RunTest");
    }

    [Fact]
    public void Respond_SkipsSubtest_ForWholeTestSkip()
    {
        var startSubTest = new StartSubTest
        {
            data = new StartSubTest.StartSubTestType
            {
                testName = "x.test_should_echo_relationship",
                subtestArguments = new Dictionary<string, JToken>()
            }
        };

        var response = JObject.Parse(startSubTest.Respond());

        response["name"].Value<string>().Should().Be("SkipTest");
        response["data"]["reason"].Value<string>().Should().Contain("relationship");
    }

    [Fact]
    public void Respond_SkipsSubtest_ForOutOfRangeZonedTimeOffset()
    {
        var startSubTest = ZonedTimeSubTest(-86400);

        var response = JObject.Parse(startSubTest.Respond());

        response["name"].Value<string>().Should().Be("SkipTest");
        response["data"]["reason"].Value<string>().Should().Contain("64800");
    }

    [Fact]
    public void Respond_RunsSubtest_ForInRangeZonedTimeOffset()
    {
        var startSubTest = ZonedTimeSubTest(3661);

        JObject.Parse(startSubTest.Respond())["name"].Value<string>().Should().Be("RunTest");
    }

    private static StartSubTest ZonedTimeSubTest(int utcOffsetSeconds)
    {
        return new StartSubTest
        {
            data = new StartSubTest.StartSubTestType
            {
                testName = "tests.stub.http_query.datatypes.test_temporal.TestTemporal.test_zoned_time",
                subtestArguments = new Dictionary<string, JToken>
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
                }
            }
        };
    }
}

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
}

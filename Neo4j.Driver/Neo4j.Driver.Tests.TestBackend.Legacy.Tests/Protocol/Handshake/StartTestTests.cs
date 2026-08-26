using FluentAssertions;
using Neo4j.Driver.Tests.TestBackend.Protocol.Handshake;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Neo4j.Driver.Tests.TestBackend.Tests.Protocol.Handshake;

public class StartTestTests
{
    [Fact]
    public void Respond_SkipsBlacklistedTest_WithReason()
    {
        var startTest = new StartTest { data = new StartTest.StartTestType { testName = "x.test_should_echo_relationship" } };

        var response = JObject.Parse(startTest.Respond());

        response["name"].Value<string>().Should().Be("SkipTest");
        response["data"]["reason"].Value<string>().Should().Contain("relationship");
    }

    [Fact]
    public void Respond_RunsUnlistedTest()
    {
        var startTest = new StartTest { data = new StartTest.StartTestType { testName = "some.unlisted.test_name" } };

        var response = JObject.Parse(startTest.Respond());

        response["name"].Value<string>().Should().Be("RunTest");
    }

    [Fact]
    public void Respond_RequestsSubtests_ForZonedTime()
    {
        var startTest = new StartTest
        {
            data = new StartTest.StartTestType
            {
                testName = "tests.stub.http_query.datatypes.test_temporal.TestTemporal.test_zoned_time"
            }
        };

        JObject.Parse(startTest.Respond())["name"].Value<string>().Should().Be("RunSubTests");
    }
}

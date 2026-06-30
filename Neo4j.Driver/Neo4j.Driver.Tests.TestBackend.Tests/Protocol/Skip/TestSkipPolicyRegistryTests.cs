using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Tests.TestBackend.Protocol.Skip;
using Xunit;

namespace Neo4j.Driver.Tests.TestBackend.Tests.Protocol.Skip;

public class TestSkipPolicyRegistryTests
{
    [Fact]
    public void GetPolicy_ReturnsMatchingPolicy_WhenTestNameContainsFragment()
    {
        var policy = new SkipAllPolicy("blacklisted");
        var registry = new TestSkipPolicyRegistry(new (string, ITestSkipPolicy)[]
        {
            ("module.TestThing.test_specific", policy)
        });

        registry.GetPolicy("fully.qualified.module.TestThing.test_specific")
            .Should().BeSameAs(policy);
    }

    [Fact]
    public void GetPolicy_ReturnsFirstMatch_WhenMultipleFragmentsMatch()
    {
        var first = new SkipAllPolicy("first");
        var second = new SkipAllPolicy("second");
        var registry = new TestSkipPolicyRegistry(new (string, ITestSkipPolicy)[]
        {
            ("test_a", first),
            ("test_a", second)
        });

        registry.GetPolicy("module.test_a").Should().BeSameAs(first);
    }

    [Fact]
    public void GetPolicy_ReturnsRunAll_WhenNoFragmentMatches()
    {
        var registry = new TestSkipPolicyRegistry(new (string, ITestSkipPolicy)[]
        {
            ("test_a", new SkipAllPolicy("nope"))
        });

        registry.GetPolicy("module.test_b").Overall.Should().BeOfType<TestDisposition.RunAll>();
    }

    [Fact]
    public void DefaultRegistry_SkipsKnownBlacklistedTest()
    {
        TestSkipPolicies.Default.GetPolicy("anything.test_should_echo_relationship").Overall
            .Should().BeOfType<TestDisposition.SkipAll>();
    }

    [Fact]
    public void DefaultRegistry_RunsUnlistedTest()
    {
        TestSkipPolicies.Default.GetPolicy("some.totally.unlisted.test_name").Overall
            .Should().BeOfType<TestDisposition.RunAll>();
    }
}

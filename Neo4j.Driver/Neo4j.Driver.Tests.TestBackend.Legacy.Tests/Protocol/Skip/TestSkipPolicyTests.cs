using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Tests.TestBackend.Protocol.Skip;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Neo4j.Driver.Tests.TestBackend.Tests.Protocol.Skip;

public class TestDispositionTests
{
    [Fact]
    public void RunAll_IsSingletonRunAllCase()
    {
        TestDispositions.RunAll.Should().BeOfType<TestDisposition.RunAll>();
        TestDispositions.RunAll.Should().BeSameAs(TestDispositions.RunAll);
    }

    [Fact]
    public void RunSubtests_IsSingletonRunSubtestsCase()
    {
        TestDispositions.RunSubtests.Should().BeOfType<TestDisposition.RunSubtests>();
        TestDispositions.RunSubtests.Should().BeSameAs(TestDispositions.RunSubtests);
    }

    [Fact]
    public void SkipAll_CarriesReason()
    {
        var disposition = TestDispositions.SkipAll("because reasons");

        disposition.Should().BeOfType<TestDisposition.SkipAll>()
            .Which.Reason.Should().Be("because reasons");
    }
}

public class RunAllPolicyTests
{
    [Fact]
    public void Overall_IsRunAll()
    {
        new RunAllPolicy().Overall.Should().BeOfType<TestDisposition.RunAll>();
    }

    [Fact]
    public void ShouldRunSubtest_ReturnsTrueWithNoReason()
    {
        var run = new RunAllPolicy().ShouldRunSubtest(NoParameters, out var reason);

        run.Should().BeTrue();
        reason.Should().BeEmpty();
    }

    private static IReadOnlyDictionary<string, JToken> NoParameters => new Dictionary<string, JToken>();
}

public class SkipAllPolicyTests
{
    [Fact]
    public void Overall_IsSkipAllWithReason()
    {
        new SkipAllPolicy("blacklisted").Overall
            .Should().BeOfType<TestDisposition.SkipAll>()
            .Which.Reason.Should().Be("blacklisted");
    }

    [Fact]
    public void ShouldRunSubtest_ReturnsFalseWithReason()
    {
        var run = new SkipAllPolicy("blacklisted").ShouldRunSubtest(NoParameters, out var reason);

        run.Should().BeFalse();
        reason.Should().Contain("blacklisted");
    }

    private static IReadOnlyDictionary<string, JToken> NoParameters => new Dictionary<string, JToken>();
}

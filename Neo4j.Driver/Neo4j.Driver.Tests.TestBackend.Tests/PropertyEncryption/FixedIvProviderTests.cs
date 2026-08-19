using System;
using FluentAssertions;
using Neo4j.Driver.Tests.TestBackend.PropertyEncryption;
using Xunit;

namespace Neo4j.Driver.Tests.TestBackend.Tests.PropertyEncryption;

public class FixedIvProviderTests
{
    [Fact]
    public void GetIv_ReturnsTheFixedIv_WhenSet()
    {
        var provider = new FixedIvProvider();
        var iv = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

        provider.SetNextIv(iv);

        provider.GetIv().Should().Equal(iv);
    }

    [Fact]
    public void GetIv_FallsBackToRandomTwelveBytes_WhenNoFixedIvSet()
    {
        var provider = new FixedIvProvider();

        var first = provider.GetIv();
        var second = provider.GetIv();

        first.Should().HaveCount(12);
        second.Should().HaveCount(12);
        first.Should().NotEqual(second);
    }

    [Fact]
    public void GetIv_FallsBackToRandom_AfterTheFixedIvIsConsumed()
    {
        var provider = new FixedIvProvider();
        var iv = new byte[12];

        provider.SetNextIv(iv);
        provider.GetIv();

        provider.GetIv().Should().NotEqual(iv);
    }

    [Fact]
    public void SetNextIv_Throws_WhenNotTwelveBytes()
    {
        var provider = new FixedIvProvider();

        var act = () => provider.SetNextIv(new byte[11]);

        act.Should().Throw<ArgumentException>().WithMessage("*12*11*");
    }

    [Fact]
    public void SetNextIv_Throws_WhenPreviousFixedIvUnconsumed()
    {
        var provider = new FixedIvProvider();
        provider.SetNextIv(new byte[12]);

        var act = () => provider.SetNextIv(new byte[12]);

        act.Should().Throw<ArgumentException>().WithMessage("*unconsumed*");
    }

    [Fact]
    public void EnsureConsumed_Throws_WhenAFixedIvRemains()
    {
        var provider = new FixedIvProvider();
        provider.SetNextIv(new byte[12]);

        var act = () => provider.EnsureConsumed();

        act.Should().Throw<ArgumentException>().WithMessage("*not consumed*");
    }

    [Fact]
    public void EnsureConsumed_DoesNotThrow_AfterTheFixedIvIsConsumed()
    {
        var provider = new FixedIvProvider();
        provider.SetNextIv(new byte[12]);
        provider.GetIv();

        var act = () => provider.EnsureConsumed();

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureConsumed_DoesNotThrow_WhenNoFixedIvWasEverSet()
    {
        var provider = new FixedIvProvider();
        provider.GetIv();

        var act = () => provider.EnsureConsumed();

        act.Should().NotThrow();
    }
}

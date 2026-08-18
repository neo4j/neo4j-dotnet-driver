using System;
using FluentAssertions;
using Neo4j.Driver.Tests.TestBackend.PropertyEncryption;
using Xunit;

namespace Neo4j.Driver.Tests.TestBackend.Tests.PropertyEncryption;

public class MockCryptoRandomProviderTests
{
    [Fact]
    public void Fill_RepeatsProvidedBytes()
    {
        var provider = new MockCryptoRandomProvider();
        provider.ProvideBytes([1, 2, 3, 4, 5]);

        var buffer = new byte[5];
        provider.Fill(buffer);

        buffer.Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public void Fill_ConsumesBytesSequentiallyAcrossCalls()
    {
        var provider = new MockCryptoRandomProvider();
        provider.ProvideBytes([1, 2, 3, 4, 5]);

        var first = new byte[3];
        var second = new byte[2];
        provider.Fill(first);
        provider.Fill(second);

        first.Should().Equal(1, 2, 3);
        second.Should().Equal(4, 5);
    }

    [Fact]
    public void Fill_Throws_WhenMoreBytesRequestedThanRemain()
    {
        var provider = new MockCryptoRandomProvider();
        provider.ProvideBytes([1, 2, 3, 4]);
        provider.Fill(new byte[3]);

        var act = () => provider.Fill(new byte[2]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*2*requested*1*remain*");
    }

    [Fact]
    public void Fill_Throws_WhenNoBytesProvided()
    {
        var provider = new MockCryptoRandomProvider();

        var act = () => provider.Fill(new byte[1]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnsureAllBytesConsumed_Throws_WhenBytesRemain()
    {
        var provider = new MockCryptoRandomProvider();
        provider.ProvideBytes([1, 2, 3]);
        provider.Fill(new byte[1]);

        var act = () => provider.EnsureAllBytesConsumed();

        act.Should().Throw<InvalidOperationException>().WithMessage("*2*remain*");
    }

    [Fact]
    public void EnsureAllBytesConsumed_DoesNotThrow_WhenAllConsumed()
    {
        var provider = new MockCryptoRandomProvider();
        provider.ProvideBytes([1, 2]);
        provider.Fill(new byte[2]);

        var act = () => provider.EnsureAllBytesConsumed();

        act.Should().NotThrow();
    }

    [Fact]
    public void ProvideBytes_Throws_WhenUnconsumedBytesRemain()
    {
        var provider = new MockCryptoRandomProvider();
        provider.ProvideBytes([1, 2, 3]);
        provider.Fill(new byte[1]);

        var act = () => provider.ProvideBytes([9]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*2*remain*");
    }

    [Fact]
    public void ProvideBytes_StartsAFreshStream_AfterPreviousStreamConsumed()
    {
        var provider = new MockCryptoRandomProvider();
        provider.ProvideBytes([1, 2]);
        provider.Fill(new byte[2]);

        provider.ProvideBytes([9, 8]);
        var buffer = new byte[2];
        provider.Fill(buffer);

        buffer.Should().Equal(9, 8);
    }
}

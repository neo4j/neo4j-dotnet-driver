// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Buffers.Binary;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver;
using Neo4j.Driver.Bolt.Handshake;
using Neo4j.Driver.Bolt.Tests;
using Neo4j.Driver.Bolt.Transport.Abstractions;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.Handshake;

[TestFixture]
internal class BoltHandshakeTests : UnitTestBase<BoltHandshake>
{
    private IByteWriter Writer => AutoMocker.GetMock<IByteWriter>().Object;

    private IByteReader Reader => AutoMocker.GetMock<IByteReader>().Object;

    [Test]
    public void DefaultClientOffers_HasExpectedLengthAndMagic()
    {
        var offers = BoltHandshakeClientOffers.Default;
        offers.Length.Should().Be(20);
        BinaryPrimitives.ReadInt32BigEndian(offers.Span).Should().Be(BoltHandshakeClientOffers.GoGoBolt);
    }

    [Test]
    public async Task NegotiateAsync_LegacyServerResponse_ReturnsVersion()
    {
        var written = Arrange.HandshakeWithWriteCapture(AutoMocker, PackWord((8 << 8) | 5));

        var version = await Subject.NegotiateAsync(Writer, Reader);

        version.Major.Should().Be(5);
        version.Minor.Should().Be(8);
        written.Should().BeEquivalentTo(BoltHandshakeClientOffers.Default.ToArray());
    }

    [Test]
    public void NegotiateAsync_ManifestMarker_ThrowsNotImplementedException()
    {
        Arrange.FirstReadExactly(AutoMocker, PackWord((1 << 8) | BoltHandshakeVersion.ManifestSchemaMajor));

        var act = async () => await Subject.NegotiateAsync(Writer, Reader);

        act.Should().ThrowAsync<NotImplementedException>().WithMessage("*Manifest-style*");
    }

    [Test]
    public void NegotiateAsync_NoAgreement_ThrowsProtocolException()
    {
        Arrange.FirstReadExactly(AutoMocker, PackWord(0));

        var act = async () => await Subject.NegotiateAsync(Writer, Reader);

        act.Should().ThrowAsync<ProtocolException>().WithMessage("*does not support*");
    }

    [Test]
    public void NegotiateAsync_HttpResponse_ThrowsNotSupportedException()
    {
        Arrange.FirstReadExactly(AutoMocker, "HTTP"u8.ToArray());

        var act = async () => await Subject.NegotiateAsync(Writer, Reader);

        act.Should().ThrowAsync<NotSupportedException>().WithMessage("*http endpoint*");
    }

    private static byte[] PackWord(int value)
    {
        var bytes = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        return bytes;
    }

    private static class Arrange
    {
        public static void FirstReadExactly(AutoMocker mocker, byte[] response)
        {
            mocker.GetMock<IByteReader>()
                .Setup(r => r.ReadExactlyAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                .Callback<Memory<byte>, CancellationToken>((dest, _) => response.AsSpan().CopyTo(dest.Span))
                .Returns(ValueTask.CompletedTask);
        }

        public static List<byte> HandshakeWithWriteCapture(AutoMocker mocker, byte[] response)
        {
            var written = new List<byte>();
            mocker.GetMock<IByteWriter>()
                .Setup(w => w.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                .Callback<ReadOnlyMemory<byte>, CancellationToken>((data, _) => written.AddRange(data.ToArray()))
                .Returns(ValueTask.CompletedTask);
            FirstReadExactly(mocker, response);
            return written;
        }
    }
}

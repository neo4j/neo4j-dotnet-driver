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

using System.Buffers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver.Bolt.PackStream;
using Neo4j.Driver.Bolt.PackStream.Abstractions;
using Neo4j.Driver.Bolt.PackStream.Abstractions.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Types.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.PackStream.ValueDecoding;

[TestFixture]
internal class ValueDecoderProviderTests
{
    private static ILogger Logger => Mock.Of<ILogger>();

    [Test]
    public void TryGetDecoderReturnsTrueAndDecoderIsNotNullForRegisteredMarker()
    {
        var decoder = new NullDecoder(Logger);
        var provider = new ValueDecoderProvider([decoder], Logger);
        var stubRecursion = new Mock<IPackStreamDecoder>().Object;

        var found = provider.TryGetDecoder(PackStreamMarker.Null, stubRecursion, out var resolved);

        found.Should().BeTrue();
        resolved.Should().NotBeNull();
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Null]);
        resolved!.Decode(buffer).Value.IsNull.Should().BeTrue();
    }

    [Test]
    public void TryGetDecoderReturnsFalseForUnknownMarker()
    {
        var provider = new ValueDecoderProvider([new NullDecoder(Logger)], Logger);
        var stubRecursion = new Mock<IPackStreamDecoder>().Object;

        var found = provider.TryGetDecoder(0x99, stubRecursion, out var decoder);

        found.Should().BeFalse();
        decoder.Should().BeNull();
    }

    [Test]
    public void TryGetDecoderReturnsTrueForEachRegisteredDecoderMarker()
    {
        var decoders = new IValueDecoder[]
        {
            new NullDecoder(Logger),
            new FloatDecoder(Logger),
        };
        var provider = new ValueDecoderProvider(decoders, Logger);
        var stubRecursion = new Mock<IPackStreamDecoder>().Object;

        var foundNull = provider.TryGetDecoder(PackStreamMarker.Null, stubRecursion, out var nullDecoder);
        var foundFloat = provider.TryGetDecoder(PackStreamMarker.Float64, stubRecursion, out var floatDecoder);

        foundNull.Should().BeTrue();
        nullDecoder.Should().NotBeNull();
        foundFloat.Should().BeTrue();
        floatDecoder.Should().NotBeNull();
    }
}

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

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver.Bolt.DependencyInjection;
using Neo4j.Driver.Bolt.Messages.Implementations;
using Neo4j.Driver.Bolt.Messages.Types;
using Neo4j.Driver.Bolt.Tests.Transport;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.Messages;

/// <summary>
/// Ensures <see cref="ServiceContainer.RegisterTypesFromThisAssembly"/> wires PackStream, chunk assembly,
/// and Bolt message decoders so <see cref="BoltStreamDecoder"/> can read framed Bolt traffic.
/// </summary>
[TestFixture]
internal class BoltStreamDecoderAutoRegistrationTests
{
    [Test]
    public async Task RegisterTypesFromThisAssembly_ResolvesBoltStreamDecoder_DecodesFramedSuccessMessage()
    {
        var container = new ServiceContainer()
            .RegisterInstance<ILogger>(Mock.Of<ILogger>())
            .RegisterTypesFromThisAssembly();

        var decoder = container.Resolve<BoltStreamDecoder>();

        // One Bolt chunk: 2-byte big-endian body length, then PackStream body.
        // SUCCESS struct (tag 0x70) with a single metadata field: empty map.
        byte[] wire =
        [
            0x00, 0x03,
            0xB1,
            0x70,
            0xA0,
        ];

        var byteReader = TestByteReaders.FromSingleReadBuffer(wire);
        var messages = new List<BoltResponseMessage>();
        await foreach (var message in decoder.ReadMessagesAsync(byteReader))
        {
            messages.Add(message);
        }

        messages.Should().ContainSingle();
        messages[0].Kind.Should().Be(MessageKind.Success);
        messages[0].AsSuccess();
    }
}

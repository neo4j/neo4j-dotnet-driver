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

namespace Neo4j.Driver.Bolt.Handshake;

/// <summary>
/// Builds the initial 20-byte Bolt handshake the client sends before chunk framing.
/// <para><b>Must stay aligned with</b> <c>BoltProtocolFactory</c> handshake bytes in Neo4j.Driver
/// (<c>PackSupportedVersions</c>).</para>
/// </summary>
internal static class BoltHandshakeClientOffers
{
    /// <summary>Magic “GO GO BOLT” marker (<c>0x6060B017</c>).</summary>
    public const int GoGoBolt = 0x6060B017;

    public static ReadOnlyMemory<byte> Default { get; } = BuildDefaultOffers();

    private static byte[] BuildDefaultOffers()
    {
        static int Pack(int major, int minor) => (minor << 8) | major;

        static int PackRange(int maxMajor, int maxMinor, int minMajor, int minMinor)
        {
            if (maxMajor != minMajor)
            {
                throw new InvalidOperationException("PackRange requires the same major for min and max.");
            }

            var range = maxMinor - minMinor;
            return (range << 16) | Pack(maxMajor, maxMinor);
        }

        // Keep in sync with Neo4j.Driver Internal/Protocol/BoltProtocolFactory HandshakeBytesLazy.
        var versions = new[]
        {
            GoGoBolt,
            Pack(BoltHandshakeVersion.ManifestSchemaMajor, 1),
            PackRange(5, 8, 5, 0),
            PackRange(4, 4, 4, 2),
            Pack(3, 0),
        };

        var buffer = new byte[versions.Length * sizeof(int)];
        var offset = 0;
        foreach (var v in versions)
        {
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset, sizeof(int)), v);
            offset += sizeof(int);
        }

        return buffer;
    }
}

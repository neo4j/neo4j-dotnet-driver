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
/// Bolt protocol version agreed during the socket handshake (major/minor), using the same packed layout as
/// the driver's <c>BoltProtocolVersion</c> (low byte = major, next byte = minor).
/// </summary>
internal readonly struct BoltHandshakeVersion : IEquatable<BoltHandshakeVersion>
{
    /// <summary>Major = 255 marks manifest-style negotiation (see Bolt spec / <c>BoltHandshaker</c>).</summary>
    public const int ManifestSchemaMajor = 255;

    /// <summary>Reserved “version” when the server speaks HTTP instead of Bolt (ASCII “HTTP” on the wire).</summary>
    public const int HttpMajor = 80;

    public const int HttpMinor = 84;

    public BoltHandshakeVersion(int major, int minor)
    {
        Major = major;
        Minor = minor;
    }

    public int Major { get; }

    public int Minor { get; }
    
    public static BoltHandshakeVersion FromBytes(byte[] bytes)
    {
        var packed = BinaryPrimitives.ReadInt32BigEndian(bytes);
        var major = packed & 0xFF;
        var minor = (packed >> 8) & 0xFF;
        return new BoltHandshakeVersion(major, minor);
    }

    /// <summary>
    /// Unpacks the first server response word (big-endian int32 on the wire, as produced by
    /// <see cref="System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian"/>).
    /// </summary>
    public static BoltHandshakeVersion FromPackedInt32(int packed)
    {
        var major = packed & 0xFF;
        var minor = (packed >> 8) & 0xFF;
        return new BoltHandshakeVersion(major, minor);
    }

    public bool IsManifestMarker => Major == ManifestSchemaMajor;

    public bool IsHttpResponse => Major == HttpMajor && Minor == HttpMinor;

    public bool Equals(BoltHandshakeVersion other) => Major == other.Major && Minor == other.Minor;

    public override bool Equals(object? obj) => obj is BoltHandshakeVersion other && Equals(other);

    public override int GetHashCode() => (Major << 16) | Minor;

    public override string ToString() => $"{Major}.{Minor}";
}

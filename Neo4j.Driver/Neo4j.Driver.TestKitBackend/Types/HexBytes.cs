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

namespace Neo4j.Driver.TestKitBackend.Types;

internal readonly struct HexBytes(byte[] value) : IEquatable<HexBytes>
{
    public byte[] Value { get; } = value;

    public static implicit operator byte[](HexBytes hex) => hex.Value;

    public static implicit operator HexBytes(byte[] value) => new(value);

    public bool Equals(HexBytes other)
    {
        return Value.AsSpan().SequenceEqual(other.Value);
    }

    public override bool Equals(object? obj)
    {
        return obj is HexBytes other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.AddBytes(Value);
        return hash.ToHashCode();
    }
}

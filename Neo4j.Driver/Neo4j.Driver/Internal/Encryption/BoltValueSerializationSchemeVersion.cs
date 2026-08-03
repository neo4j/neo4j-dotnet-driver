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

#nullable enable

namespace Neo4j.Driver.Internal.Encryption;

// The Bolt Value Serialization Scheme (ADR 037) versions plaintext/AAD serialization rules on
// its own MAJOR.MINOR axis, independent of Bolt Protocol - do not reuse BoltProtocolVersion here.
internal readonly record struct BoltValueSerializationSchemeVersion(int Major, int Minor)
{
    // Every Phase-1 type has Type Serialization Baseline 1.0 (ADR 037); this driver implements
    // only BVSS 1.0.
    public static readonly BoltValueSerializationSchemeVersion Latest = new(1, 0);

    public static bool operator >(BoltValueSerializationSchemeVersion left, BoltValueSerializationSchemeVersion right)
    {
        return left.Major != right.Major
            ? left.Major > right.Major
            : left.Minor > right.Minor;
    }

    public static bool operator <(BoltValueSerializationSchemeVersion left, BoltValueSerializationSchemeVersion right)
    {
        return right > left;
    }

    public override string ToString() => $"{Major}.{Minor}";
}

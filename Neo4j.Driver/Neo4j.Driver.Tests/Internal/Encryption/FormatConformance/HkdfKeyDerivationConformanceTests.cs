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

using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption.FormatConformance;

public class HkdfKeyDerivationConformanceTests
{
    [Fact]
    public void Derive_UsesTheAgreedInfoStringAndNoSalt()
    {
        var ikm = Enumerable.Repeat((byte)0x0B, 22).ToArray();

        var result = new HkdfKeyDerivation().Derive(ikm, 32);

        // Expected output computed outside dotnet with a Python impl of RFC 5869 HKDF-SHA256
        // ikm = 0x0B * 22 (as found in RFC 5869, A.1), salt = none,
        // info = "neo4j/property-encryption/v1", length = 32.
        result.Should().Equal(
            0x4E, 0xC2, 0x82, 0x01, 0xEB, 0xBF, 0xC7, 0x16, 0xA1, 0xF4, 0xB6, 0x56, 0x49, 0xB8, 0x3D, 0xA1,
            0xED, 0xBC, 0xCC, 0x9D, 0x39, 0x2E, 0x59, 0xA3, 0xE9, 0x5C, 0x02, 0x79, 0xEE, 0x01, 0x0B, 0xBA);
    }
}

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

using System;
using FluentAssertions;
using Neo4j.Driver.Internal.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class KeyDerivationTests
{
    private readonly HkdfKeyDerivation _subject = new();

    // Same IKM produces identical output (deterministic, fixed info label).
    [Fact]
    public void Derive_WithFixedInfoLabel_ProducesStableOutput()
    {
        var ikm = new byte[32];
        Random.Shared.NextBytes(ikm);

        var first  = _subject.Derive(ikm, outputLength: 32);
        var second = _subject.Derive(ikm, outputLength: 32);

        first.Should().HaveCount(32);
        first.Should().NotEqual(ikm);
        first.Should().Equal(second);
    }

    // The production overload (no salt, fixed info) should be equivalent to calling
    // the test overload with null salt and the "neo4j/property-encryption/v1" label.
    [Fact]
    public void Derive_ProductionOverload_MatchesRawWithFixedLabel()
    {
        var ikm = new byte[32];
        Random.Shared.NextBytes(ikm);

        var viaProduction = _subject.Derive(ikm, outputLength: 32);
        var viaRaw = _subject.Derive(ikm, salt: null, "neo4j/property-encryption/v1"u8.ToArray(), outputLength: 32);
        viaRaw.Should().Equal(viaProduction);
    }

    // Different IKMs should produce different derived keys.
    [Fact]
    public void Derive_DifferentIkm_ProducesDifferentOutput()
    {
        var ikm1 = new byte[32];
        var ikm2 = new byte[32];
        Random.Shared.NextBytes(ikm1);
        Random.Shared.NextBytes(ikm2);

        _subject.Derive(ikm1, outputLength: 32)
            .Should().NotEqual(_subject.Derive(ikm2, outputLength: 32));
    }
}

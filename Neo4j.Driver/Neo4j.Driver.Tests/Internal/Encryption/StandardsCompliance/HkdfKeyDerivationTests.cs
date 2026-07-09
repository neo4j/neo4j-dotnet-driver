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

namespace Neo4j.Driver.Tests.Internal.Encryption.StandardsCompliance;

// Standards-conformance tests for HKDF-SHA256 (RFC 5869 Appendix A).
// Authoritative source: https://www.rfc-editor.org/rfc/rfc5869#appendix-A
// Tests A.4 – A.7 use SHA-1 and are not included: HkdfKeyDerivation is fixed to SHA-256.
public class HkdfKeyDerivationTests
{
    private readonly HkdfKeyDerivation _subject = new();

    // https://www.rfc-editor.org/rfc/rfc5869#appendix-A.1
    [Fact]
    public void Rfc5869_A1_Sha256Basic()
    {
        var ikm  = Convert.FromHexString("0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b");
        var salt = Convert.FromHexString("000102030405060708090a0b0c");
        var info = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9");
        var expected = Convert.FromHexString(
            "3cb25f25faacd57a90434f64d0362f2a" +
            "2d2d0a90cf1a5a4c5db02d56ecc4c5bf" +
            "34007208d5b887185865");

        _subject.Derive(ikm, salt, info, outputLength: 42).Should().Equal(expected);
    }

    // https://www.rfc-editor.org/rfc/rfc5869#appendix-A.2
    [Fact]
    public void Rfc5869_A2_Sha256LongerInputs()
    {
        var ikm  = Convert.FromHexString(
            "000102030405060708090a0b0c0d0e0f" +
            "101112131415161718191a1b1c1d1e1f" +
            "202122232425262728292a2b2c2d2e2f" +
            "303132333435363738393a3b3c3d3e3f" +
            "404142434445464748494a4b4c4d4e4f");
        var salt = Convert.FromHexString(
            "606162636465666768696a6b6c6d6e6f" +
            "707172737475767778797a7b7c7d7e7f" +
            "808182838485868788898a8b8c8d8e8f" +
            "909192939495969798999a9b9c9d9e9f" +
            "a0a1a2a3a4a5a6a7a8a9aaabacadaeaf");
        var info = Convert.FromHexString(
            "b0b1b2b3b4b5b6b7b8b9babbbcbdbebf" +
            "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf" +
            "d0d1d2d3d4d5d6d7d8d9dadbdcdddedf" +
            "e0e1e2e3e4e5e6e7e8e9eaebecedeeef" +
            "f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff");
        var expected = Convert.FromHexString(
            "b11e398dc80327a1c8e7f78c596a4934" +
            "4f012eda2d4efad8a050cc4c19afa97c" +
            "59045a99cac7827271cb41c65e590e09" +
            "da3275600c2f09b8367793a9aca3db71" +
            "cc30c58179ec3e87c14c01d5c1f3434f" +
            "1d87");

        _subject.Derive(ikm, salt, info, outputLength: 82).Should().Equal(expected);
    }

    // https://www.rfc-editor.org/rfc/rfc5869#appendix-A.3
    [Fact]
    public void Rfc5869_A3_Sha256ZeroLengthSaltAndInfo()
    {
        var ikm  = Convert.FromHexString("0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b");
        var expected = Convert.FromHexString(
            "8da4e775a563c18f715f802a063c5a31" +
            "b8a11f5c5ee1879ec3454e5f3c738d2d" +
            "9d201395faa4b61a96c8");

        _subject.Derive(ikm, salt: [], info: [], outputLength: 42).Should().Equal(expected);
    }
}

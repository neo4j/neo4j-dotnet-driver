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

using System;
using System.Security.Cryptography;
using FluentAssertions;
using Neo4j.Driver.Internal.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption.StandardsCompliance;

// Standards-conformance tests for AES-256-GCM (96-bit IV, 128-bit tag).
// Authoritative source: https://github.com/C2SP/wycheproof/blob/master/testvectors_v1/aes_gcm_test.json
public class AesGcmCipherTests
{
    private readonly AesGcmCipher _subject = new();

    // https://github.com/C2SP/wycheproof/blob/master/testvectors_v1/aes_gcm_test.json tcId=91 (Ktv)
    [Fact]
    public void Wycheproof_91_WithAadAndPlaintext()
    {
        var key = Convert.FromHexString("92ace3e348cd821092cd921aa3546374299ab46209691bc28b8752d17f123c20");
        var iv  = Convert.FromHexString("00112233445566778899aabb");
        var aad = Convert.FromHexString("00000000ffffffff");
        var msg = Convert.FromHexString("00010203040506070809");
        var expectedCt  = Convert.FromHexString("e27abdd2d2a53d2f136b");
        var expectedTag = Convert.FromHexString("9a4a2579529301bcfb71c78d4060f52c");

        var cipherResult = _subject.Encrypt(key, iv, msg, aad);

        cipherResult.CipherText.Should().Equal(expectedCt);
        cipherResult.Tag.Should().Equal(expectedTag);

        _subject.Decrypt(key, iv, cipherResult.Combined, aad).Should().Equal(msg);
    }

    // https://github.com/C2SP/wycheproof/blob/master/testvectors_v1/aes_gcm_test.json tcId=92 (Ktv)
    [Fact]
    public void Wycheproof_92_EmptyPlaintext()
    {
        var key = Convert.FromHexString("29d3a44f8723dc640239100c365423a312934ac80239212ac3df3421a2098123");
        var iv  = Convert.FromHexString("00112233445566778899aabb");
        var aad = Convert.FromHexString("aabbccddeeff");
        var expectedTag = Convert.FromHexString("2a7d77fa526b8250cb296078926b5020");

        var cipherResult = _subject.Encrypt(key, iv, msg: [], aad);

        cipherResult.Tag.Should().Equal(expectedTag);
        _subject.Decrypt(key, iv, cipherResult.Combined, aad).Should().BeEmpty();
    }

    // https://github.com/C2SP/wycheproof/blob/master/testvectors_v1/aes_gcm_test.json tcId=93
    [Fact]
    public void Wycheproof_93_EmptyPlaintextAndAad()
    {
        var key = Convert.FromHexString("80ba3192c803ce965ea371d5ff073cf0f43b6a2ab576b208426e11409c09b9b0");
        var iv  = Convert.FromHexString("4da5bf8dfd5852c1ea12379d");
        var expectedTag = Convert.FromHexString("4771a7c404a472966cea8f73c8bfe17a");

        var cipherResult = _subject.Encrypt(key, iv, msg: [], aad: []);

        cipherResult.Tag.Should().Equal(expectedTag);
        _subject.Decrypt(key, iv, cipherResult.Combined, aad: []).Should().BeEmpty();
    }

    // https://github.com/C2SP/wycheproof/blob/master/testvectors_v1/aes_gcm_test.json tcId=94
    [Fact]
    public void Wycheproof_94_SingleBytePlaintext()
    {
        var key = Convert.FromHexString("cc56b680552eb75008f5484b4cb803fa5063ebd6eab91f6ab6aef4916a766273");
        var iv  = Convert.FromHexString("99e23ec48985bccdeeab60f1");
        var msg = Convert.FromHexString("2a");
        var expectedCt  = Convert.FromHexString("06");
        var expectedTag = Convert.FromHexString("633c1e9703ef744ffffb40edf9d14355");

        var cipherResult = _subject.Encrypt(key, iv, msg, aad: []);

        cipherResult.CipherText.Should().Equal(expectedCt);
        cipherResult.Tag.Should().Equal(expectedTag);

        _subject.Decrypt(key, iv, cipherResult.Combined, aad: []).Should().Equal(msg);
    }

    [Fact]
    public void Decrypt_CorruptedTag_Throws()
    {
        var key = Convert.FromHexString("92ace3e348cd821092cd921aa3546374299ab46209691bc28b8752d17f123c20");
        var iv  = Convert.FromHexString("00112233445566778899aabb");
        var aad = Convert.FromHexString("00000000ffffffff");
        var msg = Convert.FromHexString("00010203040506070809");

        var cipherResult = _subject.Encrypt(key, iv, msg, aad);
        cipherResult.Tag[^1] ^= 0xff;

        var act = () => _subject.Decrypt(key, iv, cipherResult.Combined, aad);
        act.Should().Throw<AuthenticationTagMismatchException>();
    }
}

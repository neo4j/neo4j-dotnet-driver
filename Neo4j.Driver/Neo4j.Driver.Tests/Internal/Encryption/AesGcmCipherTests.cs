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

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class AesGcmCipherTests
{
    private readonly AesGcmCipher _subject = new();

    private static readonly byte[] Key = new byte[32];
    private static readonly byte[] Iv  = new byte[12];

    static AesGcmCipherTests()
    {
        Random.Shared.NextBytes(Key);
        Random.Shared.NextBytes(Iv);
    }

    // Same key, IV, plaintext, and AAD always produce the same ciphertext and tag.
    [Fact]
    public void Encrypt_SameInputs_ProducesStableOutput()
    {
        var plaintext = new byte[32];
        Random.Shared.NextBytes(plaintext);

        var first  = _subject.Encrypt(Key, Iv, plaintext, aad: []);
        var second = _subject.Encrypt(Key, Iv, plaintext, aad: []);

        first.CipherText.Should().Equal(second.CipherText);
        first.Tag.Should().Equal(second.Tag);
    }

    // Different IVs produce different ciphertext even for the same key and plaintext.
    [Fact]
    public void Encrypt_DifferentIvs_ProduceDifferentOutput()
    {
        var iv2 = new byte[12];
        Random.Shared.NextBytes(iv2);
        var plaintext = new byte[32];
        Random.Shared.NextBytes(plaintext);

        var result1 = _subject.Encrypt(Key, Iv, plaintext, aad: []);
        var result2 = _subject.Encrypt(Key, iv2, plaintext, aad: []);

        result1.CipherText.Should().NotEqual(result2.CipherText);
    }

    // Encrypt then Decrypt recovers the original plaintext.
    [Fact]
    public void EncryptThenDecrypt_RecoverPlaintext()
    {
        var plaintext = new byte[64];
        Random.Shared.NextBytes(plaintext);
        var aad = new byte[8];
        Random.Shared.NextBytes(aad);

        var result = _subject.Encrypt(Key, Iv, plaintext, aad);
        byte[] cipherOutput = [..result.CipherText, ..result.Tag];

        _subject.Decrypt(Key, Iv, cipherOutput, aad).Should().Equal(plaintext);
    }

    // A corrupted tag must cause decryption to throw.
    [Fact]
    public void Decrypt_CorruptedTag_Throws()
    {
        var plaintext = new byte[32];
        Random.Shared.NextBytes(plaintext);

        var result = _subject.Encrypt(Key, Iv, plaintext, aad: []);
        byte[] cipherOutput = [..result.CipherText, ..result.Tag];
        cipherOutput[^1] ^= 0xff;

        var act = () => _subject.Decrypt(Key, Iv, cipherOutput, aad: []);

        act.Should().Throw<AuthenticationTagMismatchException>();
    }
}

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

    [Fact]
    public void Encrypt_SameInputs_ProducesStableOutput()
    {
        var plaintext = new byte[32];
        Random.Shared.NextBytes(plaintext);

        var first  = _subject.Encrypt(Key, Iv, plaintext, aad: []);
        var second = _subject.Encrypt(Key, Iv, plaintext, aad: []);

        first.CipherText.ToArray().Should().Equal(second.CipherText.ToArray());
        first.Tag.ToArray().Should().Equal(second.Tag.ToArray());
    }

    [Fact]
    public void Encrypt_DifferentIvs_ProduceDifferentOutput()
    {
        var iv2 = new byte[12];
        Random.Shared.NextBytes(iv2);
        var plaintext = new byte[32];
        Random.Shared.NextBytes(plaintext);

        var result1 = _subject.Encrypt(Key, Iv, plaintext, aad: []);
        var result2 = _subject.Encrypt(Key, iv2, plaintext, aad: []);

        result1.CipherText.ToArray().Should().NotEqual(result2.CipherText.ToArray());
    }

    [Fact]
    public void EncryptThenDecrypt_RecoverPlaintext()
    {
        var plaintext = new byte[64];
        Random.Shared.NextBytes(plaintext);
        var aad = new byte[8];
        Random.Shared.NextBytes(aad);

        var result = _subject.Encrypt(Key, Iv, plaintext, aad);
        var cipherOutput = result.CipherOutput;

        _subject.Decrypt(Key, Iv, cipherOutput, aad).Should().Equal(plaintext);
    }

    [Fact]
    public void Decrypt_CorruptedTag_Throws()
    {
        var plaintext = new byte[32];
        Random.Shared.NextBytes(plaintext);

        var result = _subject.Encrypt(Key, Iv, plaintext, aad: []);
        var cipherOutput = result.CipherOutput;
        cipherOutput[^1] ^= 0xff;

        var act = () => _subject.Decrypt(Key, Iv, cipherOutput, aad: []);

        act.Should().Throw<AuthenticationTagMismatchException>();
    }

    [Fact]
    public void Decrypt_CipherOutputShorterThanTag_ThrowsProtocolException()
    {
        var cipherOutput = new byte[8];

        var act = () => _subject.Decrypt(Key, Iv, cipherOutput, aad: []);

        act.Should().Throw<ProtocolException>();
    }
}

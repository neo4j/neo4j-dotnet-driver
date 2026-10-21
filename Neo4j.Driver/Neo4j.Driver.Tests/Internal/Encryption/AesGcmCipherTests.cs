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

using FluentAssertions;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Xunit;
using static Neo4j.Driver.Tests.Internal.Encryption.EncryptionTestHelpers;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class AesGcmCipherTests
{
    private static readonly byte[] Key = Sequence(32, seed: 0x10);
    private static readonly byte[] Iv = Sequence(12, seed: 0x40);
    private static readonly byte[] Plaintext = Sequence(32, seed: 0x80);

    private readonly AesGcmCipher _subject = new();

    [Fact]
    public void Encrypt_DifferentIvs_ProduceDifferentOutput()
    {
        var otherIv = Sequence(12, seed: 0x60);

        var first = _subject.Encrypt(Key, Iv, Plaintext, aad: []);
        var second = _subject.Encrypt(Key, otherIv, Plaintext, aad: []);

        first.CipherText.ToArray().Should().NotEqual(second.CipherText.ToArray());
    }

    [Fact]
    public void Decrypt_CipherOutputShorterThanTag_ThrowsPropertyEncryptionException()
    {
        var cipherOutput = new byte[8];

        var act = () => _subject.Decrypt(Key, Iv, cipherOutput, aad: []);

        act.Should().Throw<PropertyEncryptionException>();
    }
}

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

using System.Security.Cryptography;

namespace Neo4j.Driver.Internal.Encryption;

[DriverAutoRegister(singleton: true)]
internal class AesGcmCipher : IAeadCipher
{
    private const int TagSizeInBytes = 16;

    public byte[] Decrypt(byte[] key, byte[] iv, byte[] cipherOutput, byte[] aad)
    {
        using var aesGcm = new AesGcm(key, TagSizeInBytes);
        var ciphertext = cipherOutput[..^TagSizeInBytes];
        var tag = cipherOutput[^TagSizeInBytes..];
        var plaintext = new byte[ciphertext.Length];
        aesGcm.Decrypt(iv, ciphertext, tag, plaintext, aad);
        return plaintext;
    }

    public CipherResult Encrypt(byte[] key, byte[] iv, byte[] msg, byte[] aad)
    {
        using var aesGcm = new AesGcm(key, TagSizeInBytes);
        var cipherText = new byte[msg.Length];
        var tag = new byte[TagSizeInBytes]; 
        aesGcm.Encrypt(iv, msg, cipherText, tag, aad);
        return new CipherResult(cipherText, tag);
    }
}

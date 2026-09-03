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

namespace Neo4j.Driver.Internal.Encryption;

internal class AesGcmCipher : IAeadCipher
{
    private const int TagSizeInBytes = 16;

    public byte[] Decrypt(byte[] key, byte[] iv, byte[] cipherOutput, byte[] aad)
    {
        if (cipherOutput.Length < TagSizeInBytes)
        {
            throw new ProtocolException(
                $"Cipher output must be at least {TagSizeInBytes} bytes to contain an authentication tag, " +
                $"but was {cipherOutput.Length} bytes.");
        }

        using var aesGcm = new AesGcm(key, TagSizeInBytes);
        var cipherTextLength = cipherOutput.Length - TagSizeInBytes;
        var plaintext = new byte[cipherTextLength];
        var cipherText = cipherOutput.AsSpan(0, cipherTextLength);
        var tag = cipherOutput.AsSpan(cipherTextLength);
        aesGcm.Decrypt(iv, cipherText, tag, plaintext, aad);

        return plaintext;
    }

    public CipherResult Encrypt(byte[] key, byte[] iv, byte[] msg, byte[] aad)
    {
        using var aesGcm = new AesGcm(key, TagSizeInBytes);
        var cipherOutputBuffer = new byte[msg.Length + TagSizeInBytes];
        var cipherTextBuffer = cipherOutputBuffer.AsSpan(0, msg.Length);
        var tagBuffer = cipherOutputBuffer.AsSpan(msg.Length);
        aesGcm.Encrypt(iv, msg, cipherTextBuffer, tagBuffer, aad);
        return new CipherResult(cipherOutputBuffer, TagSizeInBytes);
    }
}

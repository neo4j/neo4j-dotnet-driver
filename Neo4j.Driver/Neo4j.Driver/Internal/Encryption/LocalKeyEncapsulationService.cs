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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

internal class LocalKeyEncapsulationService : IKeyEncapsulationService
{
    private const string IvOption = "iv";

    private readonly byte[] _kek;
    private readonly IAeadCipher _aeadCipher;
    private readonly ICryptoRandomProvider _randomProvider;
    private readonly IBase64Codec _base64Codec;

    internal LocalKeyEncapsulationService(
        byte[] kek,
        IAeadCipher aeadCipher,
        ICryptoRandomProvider randomProvider,
        IBase64Codec base64Codec)
    {
        _kek = kek;
        _aeadCipher = aeadCipher;
        _randomProvider = randomProvider;
        _base64Codec = base64Codec;
    }

    public Task<KeyEncapsulationResult> EncapsulateAsync(
        IKeyEncapsulationOptions options,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dek = new byte[AesGcmConstants.KeyLengthInBytes];
        _randomProvider.Fill(dek);

        var iv = new byte[AesGcmConstants.IvLengthInBytes];
        _randomProvider.Fill(iv);

        var wrapped = _aeadCipher.Encrypt(_kek, iv, dek, aad: []).CipherOutput;

        var metadata = new Dictionary<string, string> { [IvOption] = _base64Codec.Encode(iv) };

        return Task.FromResult(new KeyEncapsulationResult(wrapped, metadata, dek));
    }

    public Task<byte[]> DecapsulateAsync(
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var iv = _base64Codec.Decode(metadata[IvOption]);
        var decapsulatedKey = _aeadCipher.Decrypt(_kek, iv, encapsulation, aad: []);
        return Task.FromResult(decapsulatedKey);
    }
}

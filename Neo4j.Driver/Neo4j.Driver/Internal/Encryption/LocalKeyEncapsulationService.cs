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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Encryption;

internal class LocalKeyEncapsulationService : IKeyEncapsulationService
{
    private const int DekSizeInBytes = 32;
    private const int IvSizeInBytes = 12;
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

    public Task<EncapsulationResult> EncapsulateAsync(
        IKeyEncapsulationOptions options,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dek = new byte[DekSizeInBytes];
        _randomProvider.Fill(dek);

        var iv = new byte[IvSizeInBytes];
        _randomProvider.Fill(iv);

        var wrapped = _aeadCipher.Encrypt(_kek, iv, dek, aad: []).Combined;

        var resultOptions = new MapKeyEncapsulationOptions(
            new Dictionary<string, string> { [IvOption] = _base64Codec.Encode(iv) });

        return Task.FromResult(new EncapsulationResult(wrapped, resultOptions, dek));
    }

    public Task<byte[]> DecapsulateAsync(
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> options,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var iv = _base64Codec.Decode(options[IvOption]);
        var decapsulatedKey = _aeadCipher.Decrypt(_kek, iv, encapsulation, aad: []);
        return Task.FromResult(decapsulatedKey);
    }
}

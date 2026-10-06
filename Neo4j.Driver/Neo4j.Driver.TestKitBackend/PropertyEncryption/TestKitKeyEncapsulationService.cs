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

using System.Security.Cryptography;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.TestKitBackend.PropertyEncryption;

internal class TestKitKeyEncapsulationService : IKeyEncapsulationService
{
    private const string IvMetadataKey = "iv";
    private const int KeyLength = 32;
    private const int IvLength = 12;
    private const int TagLength = 16;

    private readonly byte[] _kek;

    public TestKitKeyEncapsulationService(byte[]? kek)
    {
        _kek = kek ?? RandomNumberGenerator.GetBytes(KeyLength);
    }

    public Task<KeyEncapsulationResult> EncapsulateAsync(
        IKeyEncapsulationOptions options,
        CancellationToken cancellationToken = default)
    {
        var dataKey = RandomNumberGenerator.GetBytes(KeyLength);
        var encapsulation = new byte[dataKey.Length + TagLength];

        Span<byte> iv = stackalloc byte[IvLength];
        RandomNumberGenerator.Fill(iv);

        using var aesGcm = new AesGcm(_kek, TagLength);
        aesGcm.Encrypt(
            iv,
            dataKey,
            encapsulation.AsSpan(0, dataKey.Length),
            encapsulation.AsSpan(dataKey.Length));

        var metadata = new Dictionary<string, string> { [IvMetadataKey] = Convert.ToBase64String(iv) };

        return Task.FromResult(new KeyEncapsulationResult(encapsulation, metadata, dataKey));
    }

    public Task<byte[]> DecapsulateAsync(
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        Span<byte> iv = stackalloc byte[IvLength];
        if (!Convert.TryFromBase64String(metadata[IvMetadataKey], iv, out var ivLength) || ivLength != IvLength)
        {
            throw new ArgumentException($"The '{IvMetadataKey}' option is not a {IvLength}-byte base64 value.");
        }

        var dataKey = new byte[encapsulation.Length - TagLength];

        using var aesGcm = new AesGcm(_kek, TagLength);
        aesGcm.Decrypt(
            iv,
            encapsulation.AsSpan(0, dataKey.Length),
            encapsulation.AsSpan(dataKey.Length),
            dataKey);

        return Task.FromResult(dataKey);
    }
}

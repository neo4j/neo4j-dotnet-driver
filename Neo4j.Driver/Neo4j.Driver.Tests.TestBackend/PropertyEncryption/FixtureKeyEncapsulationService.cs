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
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Tests.TestBackend.PropertyEncryption;

internal class FixtureKeyEncapsulationService : IKeyEncapsulationService
{
    private const int KeyLength = 32;
    private const int IvLength = 12;
    private const int TagLength = 16;

    private readonly byte[] _kek = RandomNumberGenerator.GetBytes(KeyLength);

    public Task<EncapsulationResult> EncapsulateAsync(
        IKeyEncapsulationOptions options,
        CancellationToken cancellationToken = default)
    {
        var dataKey = RandomNumberGenerator.GetBytes(KeyLength);
        var iv = RandomNumberGenerator.GetBytes(IvLength);
        var ciphertext = new byte[dataKey.Length];
        var tag = new byte[TagLength];

        using var aesGcm = new AesGcm(_kek, TagLength);
        aesGcm.Encrypt(iv, dataKey, ciphertext, tag);

        var encapsulation = new byte[ciphertext.Length + tag.Length];
        ciphertext.CopyTo(encapsulation, 0);
        tag.CopyTo(encapsulation, ciphertext.Length);

        var wrapOptions = new FixtureKeyEncapsulationOptions(
            new Dictionary<string, string> { ["iv"] = Convert.ToBase64String(iv) });

        return Task.FromResult(new EncapsulationResult(encapsulation, wrapOptions, dataKey));
    }

    public Task<byte[]> DecapsulateAsync(
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> options,
        CancellationToken cancellationToken = default)
    {
        var iv = Convert.FromBase64String(options["iv"]);
        var ciphertext = encapsulation[..^TagLength];
        var tag = encapsulation[^TagLength..];
        var dataKey = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(_kek, TagLength);
        aesGcm.Decrypt(iv, ciphertext, tag, dataKey);

        return Task.FromResult(dataKey);
    }

    private class FixtureKeyEncapsulationOptions : IKeyEncapsulationOptions
    {
        private readonly IReadOnlyDictionary<string, string> _map;

        public FixtureKeyEncapsulationOptions(IReadOnlyDictionary<string, string> map)
        {
            _map = map;
        }

        public IReadOnlyDictionary<string, string> ToMap()
        {
            return _map;
        }
    }
}

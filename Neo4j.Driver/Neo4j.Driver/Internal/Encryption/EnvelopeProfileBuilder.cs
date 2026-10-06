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
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

internal class EnvelopeProfileBuilder : IEnvelopeProfileBuilder
{
    public static readonly CacheConfig DefaultKeyCache = new(100, TimeSpan.FromMinutes(15));
    public static readonly CacheConfig DefaultKeyAliasIndex = new(100, TimeSpan.FromSeconds(15));

    private readonly string _name;
    private readonly IKeyEncapsulationService _keyEncapsulationService;
    private readonly IEncapsulatedKeyRecordRepository _keyRepository;

    public EnvelopeProfileBuilder(
        string name,
        IKeyEncapsulationService keyEncapsulationService,
        IEncapsulatedKeyRecordRepository keyRepository)
    {
        _name = name;
        _keyEncapsulationService = keyEncapsulationService;
        _keyRepository = keyRepository;
    }

    private CacheConfig? _keyCache = DefaultKeyCache;
    private CacheConfig? _keyAliasIndex = DefaultKeyAliasIndex;

    public IEnvelopeProfileBuilder WithKeyCache(int maxSize, TimeSpan ttl)
    {
        _keyCache = NewCacheConfig(maxSize, ttl);
        return this;
    }

    public IEnvelopeProfileBuilder WithoutKeyCache()
    {
        _keyCache = null;
        _keyAliasIndex = null;
        return this;
    }

    public IEnvelopeProfileBuilder WithKeyAliasIndex(int maxSize, TimeSpan ttl)
    {
        _keyAliasIndex = NewCacheConfig(maxSize, ttl);
        return this;
    }

    public IEnvelopeProfileBuilder WithoutKeyAliasIndex()
    {
        _keyAliasIndex = null;
        return this;
    }

    public IPropertyEncryptionProfile Build()
    {
        if (_keyCache is null && _keyAliasIndex is not null)
        {
            throw new InvalidOperationException(
                "The key alias index cannot be enabled while the key cache is disabled: an alias " +
                "resolves to a key id, and the key itself then comes from the key cache.");
        }

        return new EnvelopeEncryptionProfile(
            _name,
            _keyEncapsulationService,
            _keyRepository,
            _keyCache,
            _keyAliasIndex);
    }

    private static CacheConfig NewCacheConfig(int maxSize, TimeSpan ttl)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttl, TimeSpan.Zero);

        return new CacheConfig(maxSize, ttl);
    }
}

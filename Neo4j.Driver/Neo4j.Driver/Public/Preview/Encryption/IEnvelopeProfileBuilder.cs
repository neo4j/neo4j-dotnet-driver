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

namespace Neo4j.Driver.Preview.Encryption;

/// <summary>
/// Builds an Envelope <see cref="IPropertyEncryptionProfile"/>. Obtain one from
/// <see cref="PropertyEncryptionProfile.EnvelopeBuilder"/>. This interface is part of the Encryption
/// Preview feature, and is subject to change or removal.
/// </summary>
public interface IEnvelopeProfileBuilder
{
    /// <summary>
    /// Bounds the cache of decapsulated data encryption keys, which spares the configured
    /// <see cref="IEncapsulatedKeyRecordRepository"/> and <see cref="IKeyEncapsulationService"/> a
    /// round trip per operation. Enabled by default, holding 100 entries for 15 minutes. This method is
    /// part of the Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="maxSize">The greatest number of keys to retain; at least 1.</param>
    /// <param name="ttl">How long a decapsulated key may be used for; greater than zero.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxSize"/> is less than 1, or <paramref name="ttl"/> is not greater than zero.
    /// </exception>
    IEnvelopeProfileBuilder WithKeyCache(int maxSize, TimeSpan ttl);

    /// <summary>
    /// Stops caching decapsulated data encryption keys, so every operation resolves its key afresh.
    /// This also disables the key alias index, which cannot resolve a key without the key cache. This
    /// method is part of the Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <returns>This builder.</returns>
    IEnvelopeProfileBuilder WithoutKeyCache();

    /// <summary>
    /// Bounds the index of key aliases to key ids. It holds no key material, so it expires
    /// independently of the key cache; the default is a shorter 15 seconds for 100 entries, because an
    /// alias may be reassigned to another key. This method is part of the Encryption Preview feature,
    /// and is subject to change or removal.
    /// </summary>
    /// <param name="maxSize">The greatest number of aliases to retain; at least 1.</param>
    /// <param name="ttl">How long an alias mapping may be used for; greater than zero.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxSize"/> is less than 1, or <paramref name="ttl"/> is not greater than zero.
    /// </exception>
    IEnvelopeProfileBuilder WithKeyAliasIndex(int maxSize, TimeSpan ttl);

    /// <summary>
    /// Stops indexing key aliases, so resolving a key by alias consults the configured
    /// <see cref="IEncapsulatedKeyRecordRepository"/> every time. This method is part of the Encryption
    /// Preview feature, and is subject to change or removal.
    /// </summary>
    /// <returns>This builder.</returns>
    IEnvelopeProfileBuilder WithoutKeyAliasIndex();

    /// <summary>
    /// Returns the configured profile. This method is part of the Encryption Preview feature, and is
    /// subject to change or removal.
    /// </summary>
    /// <returns>The configured profile.</returns>
    /// <exception cref="InvalidOperationException">
    /// The key alias index is enabled while the key cache is disabled, which it cannot be: an alias
    /// resolves to a key id, and the key itself then comes from the key cache.
    /// </exception>
    IPropertyEncryptionProfile Build();
}

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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Neo4j.Driver.Internal.Services;

namespace Neo4j.Driver.Internal.Caching;

// Buckets a BoundedLruCache per profile name, so one profile's churn can't evict another
// profile's entries. Not itself DI-registered - composed by named, per-use-case caches.
internal class PerProfileBoundedCache<TValue>
{
    private readonly int _capacityPerProfile;
    private readonly TimeSpan? _ttl;
    private readonly IDateTimeProvider _clock;
    private readonly object _lock = new();
    private readonly Dictionary<string, IBoundedCache<string, TValue>> _perProfile = new();

    public PerProfileBoundedCache(int capacityPerProfile, TimeSpan? ttl, IDateTimeProvider clock)
    {
        _capacityPerProfile = capacityPerProfile;
        _ttl = ttl;
        _clock = clock;
    }

    public bool TryGet(string profileName, string key, [NotNullWhen(true)] out TValue? value)
    {
        return GetOrAddProfileCache(profileName).TryGet(key, out value);
    }

    public void Set(string profileName, string key, TValue value)
    {
        GetOrAddProfileCache(profileName).Set(key, value);
    }

    private IBoundedCache<string, TValue> GetOrAddProfileCache(string profileName)
    {
        lock (_lock)
        {
            if (_perProfile.TryGetValue(profileName, out var cache))
            {
                return cache;
            }

            cache = new BoundedLruCache<string, TValue>(_capacityPerProfile, _ttl, _clock);
            _perProfile[profileName] = cache;

            return cache;
        }
    }
}

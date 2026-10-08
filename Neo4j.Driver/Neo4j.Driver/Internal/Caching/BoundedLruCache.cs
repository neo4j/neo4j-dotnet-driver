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

internal class BoundedLruCache<TKey, TValue> : IBoundedCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly TimeSpan? _ttl;
    private readonly IDateTimeProvider _clock;
    private readonly object _lock = new();
    private readonly LinkedList<CacheEntry> _entries = new();
    private readonly Dictionary<TKey, LinkedListNode<CacheEntry>> _index = new();

    public BoundedLruCache(int capacity, TimeSpan? ttl, IDateTimeProvider clock)
    {
        _capacity = capacity;
        _ttl = ttl;
        _clock = clock;
    }

    public bool TryGet(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        lock (_lock)
        {
            if (!_index.TryGetValue(key, out var node))
            {
                value = default;
                return false;
            }

            if (_ttl.HasValue && node.Value.ExpiresAt <= _clock.Now())
            {
                _entries.Remove(node);
                _index.Remove(key);
                value = default;
                return false;
            }

            _entries.Remove(node);
            _entries.AddFirst(node);

            value = node.Value.Value!;
            return true;
        }
    }

    public void Set(TKey key, TValue value)
    {
        lock (_lock)
        {
            PurgeExpiredEntries();

            if (_index.TryGetValue(key, out var existing))
            {
                _entries.Remove(existing);
                _index.Remove(key);
            }

            var node = _entries.AddFirst(new CacheEntry(key, value, ExpiryFromNow()));
            _index[key] = node;

            if (_index.Count > _capacity)
            {
                var lru = _entries.Last!;
                _entries.RemoveLast();
                _index.Remove(lru.Value.Key);
            }
        }
    }

    public void Remove(TKey key)
    {
        lock (_lock)
        {
            if (_index.TryGetValue(key, out var node))
            {
                _entries.Remove(node);
                _index.Remove(key);
            }
        }
    }

    private DateTime? ExpiryFromNow()
    {
        if (!_ttl.HasValue)
        {
            return null;
        }

        var now = _clock.Now();
        var latestReachableTtl = DateTime.MaxValue - now;
        return _ttl.Value >= latestReachableTtl ? DateTime.MaxValue : now + _ttl.Value;
    }

    private void PurgeExpiredEntries()
    {
        var now = _clock.Now();
        var node = _entries.First;
        while (node is not null)
        {
            var next = node.Next;
            if (node.Value.ExpiresAt is { } expiresAt && expiresAt <= now)
            {
                _entries.Remove(node);
                _index.Remove(node.Value.Key);
            }

            node = next;
        }
    }

    private readonly record struct CacheEntry(TKey Key, TValue Value, DateTime? ExpiresAt);
}

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

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Connector.Resolvers;

internal class CachingHostResolver : IHostResolver
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IHostResolver _resolver;
    private readonly int _ttl;

    public CachingHostResolver(IHostResolver resolver, int ttl)
    {
        _resolver = resolver;
        _ttl = ttl;
    }

    public IPAddress[] Resolve(string hostname)
    {
        if (!TryGetCached(hostname, out var entry))
        {
            _lock.Wait();
            try
            {
                if (!TryGetCached(hostname, out entry))
                {
                    var resolved = _resolver.Resolve(hostname);

                    entry = new CacheEntry
                    {
                        Timer = Stopwatch.StartNew(),
                        Addresses = resolved
                    };

                    var captured = entry;
                    entry = _cache.AddOrUpdate(hostname, entry, (_, _) => captured);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        return entry.Addresses;
    }

    public async Task<IPAddress[]> ResolveAsync(string hostname)
    {
        if (!TryGetCached(hostname, out var entry))
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!TryGetCached(hostname, out entry))
                {
                    var resolved = await _resolver.ResolveAsync(hostname).ConfigureAwait(false);

                    entry = new CacheEntry
                    {
                        Timer = Stopwatch.StartNew(),
                        Addresses = resolved
                    };

                    var captured = entry;
                    entry = _cache.AddOrUpdate(hostname, entry, (_, _) => captured);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        return entry.Addresses;
    }

    private bool TryGetCached(string hostname, out CacheEntry entry)
    {
        if (_cache.TryGetValue(hostname, out entry))
        {
            if (entry.Timer == null || entry.Timer.ElapsedMilliseconds <= _ttl)
            {
                return true;
            }
        }

        entry = null;

        return false;
    }

    private class CacheEntry
    {
        public Stopwatch Timer { get; set; }
        public IPAddress[] Addresses { get; set; }
    }
}

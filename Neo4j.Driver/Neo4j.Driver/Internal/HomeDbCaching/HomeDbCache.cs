using System.Collections.Generic;

namespace Neo4j.Driver.Internal.HomeDbCaching;

internal class HomeDbCache : IHomeDbCache
{
    private readonly Dictionary<HomeDbCacheKey, string> _cache = new();

    public bool TryGetCached(HomeDbCacheKey key, out string value)
    {
        return _cache.TryGetValue(key, out value);
    }

    public void AddOrUpdate(HomeDbCacheKey key, string value)
    {
        _cache[key] = value;
    }
}

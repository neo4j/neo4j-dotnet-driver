using System.Collections.Generic;

namespace Neo4j.Driver.Internal.HomeDbCaching;

internal class HomeDbCache : IHomeDbCache
{
    private const int PurgeThreshold = 10_000;
    private const int PurgeAmount = PurgeThreshold / 10;

    private class CacheItem
    {
        public HomeDbCacheKey Key { get; }
        public string DatabaseName { get; set; }

        public CacheItem(HomeDbCacheKey key, string databaseName)
        {
            Key = key;
            DatabaseName = databaseName;
        }
    }

    private readonly object _lock = new();
    private readonly LinkedList<CacheItem> _cachedItems = new();
    private readonly Dictionary<HomeDbCacheKey, LinkedListNode<CacheItem>> _cacheLookup = new();

    public bool TryGetCached(HomeDbCacheKey key, out string value)
    {
        lock (_lock)
        {
            value = null;
            var found = _cacheLookup.TryGetValue(key, out var node);
            if (!found)
            {
                return false;
            }

            _cachedItems.Remove(node);
            _cachedItems.AddFirst(node);
            value = node.Value.DatabaseName;
            return true;
        }
    }

    public void AddOrUpdate(HomeDbCacheKey key, string value)
    {
        lock (_lock)
        {
            // if we already have an entry
            if (_cacheLookup.TryGetValue(key, out var node))
            {
                _cachedItems.Remove(node);
            }
            else
            {
                node = new LinkedListNode<CacheItem>(new CacheItem(key, value));
                _cacheLookup[key] = node;
            }

            node.Value.DatabaseName = value;
            _cachedItems.AddFirst(node);
            PurgeOldItems();
        }
    }

    private void PurgeOldItems()
    {
        lock (_lock)
        {
            if (_cachedItems.Count < PurgeThreshold)
            {
                return;
            }

            for (var i = 0; i < PurgeAmount; i++)
            {
                RemoveLastItem();
            }
        }
    }

    private void RemoveLastItem()
    {
        var last = _cachedItems.Last;

        if(last == null)
        {
            return;
        }

        _cacheLookup.Remove(last!.Value.Key);
        _cachedItems.RemoveLast();
    }
}

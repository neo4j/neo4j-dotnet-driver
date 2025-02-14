using System.Collections.Generic;

namespace Neo4j.Driver.Internal.HomeDbCaching;

internal class HomeDbCache : IHomeDbCache
{
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

    private readonly LinkedList<CacheItem> _cachedItems = new();
    private readonly Dictionary<HomeDbCacheKey, LinkedListNode<CacheItem>> _cacheLookup = new();

    public bool TryGetCached(HomeDbCacheKey key, out string value)
    {
        value = null;
        var found = _cacheLookup.TryGetValue(key, out var node);
        if (found)
        {
            value = node.Value.DatabaseName;
            return true;
        }

        return false;
    }

    public void AddOrUpdate(HomeDbCacheKey key, string value)
    {
        LinkedListNode<CacheItem> node;
        // if we already have an entry
        if (_cacheLookup.TryGetValue(key, out node))
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
    }

    public void RemoveItems(int itemsToRemove)
    {
        for (var i = 0; i < itemsToRemove; i++)
        {
            if (_cachedItems.Count == 0)
            {
                return;
            }

            var last = _cachedItems.Last;
            _cacheLookup.Remove(last!.Value.Key);
            _cachedItems.RemoveLast();
        }
    }
}

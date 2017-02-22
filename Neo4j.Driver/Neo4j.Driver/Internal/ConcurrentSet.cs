using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Neo4j.Driver.Internal
{
    internal class ConcurrentSet <T> : IEnumerable<T>
    {
        private readonly ConcurrentDictionary<T, bool> _dictionary;

        public ConcurrentSet()
        {
            _dictionary = new ConcurrentDictionary<T, bool>();
        }

        /// <summary>
        /// true if the item was added to the set successfully; false if the item already exists.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryAdd(T item)
        {
            return _dictionary.TryAdd(item, true);
        }

        /// <summary>
        /// true if the item was removed from the set successfully; false if the item does not exists.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryRemove(T item)
        {
            bool value;
            return _dictionary.TryRemove(item, out value);
        }

        public int Count => _dictionary.Count;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _dictionary.Keys.GetEnumerator();
        }
    }
}
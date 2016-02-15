using System.Collections.Generic;

namespace Neo4j.Driver
{
    /// <summary>
    ///  A record contains ordered key and value pairs
    /// </summary>
    public interface IRecord
    {
        /// <summary>
        /// Gets the value at the given index.
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The value specified with the given index</returns>
        dynamic this[int index] { get; }

        /// <summary>
        /// Gets the value specified by the given key
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>the value spcified with the given key</returns>
        dynamic this[string key] { get; }

        /// <summary>
        /// Gets the key and value pairs in a <see cref="IReadOnlyDictionary{TKey,TValue}"/>.
        /// </summary>
        IReadOnlyDictionary<string, object> Values { get; }

        /// <summary>
        /// Gets the keys in a <see cref="IReadOnlyList{T}"/>.
        /// </summary>
        IReadOnlyList<string> Keys { get; }
    }
}
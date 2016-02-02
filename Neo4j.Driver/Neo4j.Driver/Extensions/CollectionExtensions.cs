using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal
{
    public static class CollectionExtensions
    {
        public static bool ContentEqual<T>(this IReadOnlyCollection<T> collection, IReadOnlyCollection<T> other)
        {
            if (collection == null && other == null)
                return true;

            if (collection == null || other == null || collection.Count != other.Count)
                return false;

            if (collection.Any(item => !other.Contains(item)))
            {
                return false;
            }
            return true;
        }

        public static bool ContentEqual<T, V>(this IReadOnlyDictionary<T, V> dict, IReadOnlyDictionary<T, V> other)
        {
            if (dict == null && other == null)
                return true;

            if (dict == null || other == null || dict.Count != other.Count)
                return false;

            foreach (var item in dict)
            {
                if (!other.ContainsKey(item.Key))
                    return false;

                if (!other[item.Key].Equals(item.Value))
                    return false;
            }
            return true;
        }


        public static string ValueToString(this object o)
        {
            if (o == null)
            {
                return "NULL";
            }
            if (o is string)
            {
                return o.ToString();
            }
            if (o is IDictionary)
            {
                var dict = (IDictionary) o;
                var dictStrings = (from object key in dict.Keys select $"{{{key.ValueToString()} : {dict[key].ValueToString()}}}").ToList();
                return $"[{string.Join(", ", dictStrings)}]";
            }
            else if (o is IEnumerable)
            {
                var listStrings = (from object item in ((IEnumerable) o) select item.ValueToString());
                return $"[{string.Join(", ", listStrings)}]";
            }

            return o.ToString();
        }
    }
}
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Extensions
{
    public static class Extensions
    {
        public static T[] DequeueToArray<T>(this Queue<T> queue, int length)
        {
            var output = new T[length];
            for (var i = 0; i < length; i++)
            {
                output[i] = queue.Dequeue();
            }
            return output;
        }

        public static T GetValue<T>(this IDictionary<string, object> dict, string key, T defaultValue)
        {
            return dict.ContainsKey(key) ? (T) dict[key] : defaultValue;
        }

        public static string ToContentString<K, V>(this IDictionary<K, V> dict)
        {
            var output = dict.Select(item => $"{{{item.Key}, {item.Value}}}");
            return $"[{string.Join(", ", output)}]";
        }

        public static string ToContentString<K>(this IEnumerable<K> enumerable)
        {
            var output = enumerable.Select(item => $"{item}");
            return $"[{string.Join(", ", output)}]";
        }
    }
}
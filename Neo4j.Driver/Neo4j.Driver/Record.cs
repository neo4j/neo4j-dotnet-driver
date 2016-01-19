using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Exceptions;

namespace Neo4j.Driver
{
    public class Record
    {
        public dynamic this[int index] => Values[Values.Keys.ToList()[index]]; // TODO
        public dynamic this[string key] => Values[key];

        public IReadOnlyDictionary<string, dynamic> Values { get; }
        public IReadOnlyList<string> Keys { get; }

        public Record(string[] keys, dynamic[] values )
        {
            Throw.ArgumentException.IfNotEqual(keys.Length, values.Length, nameof(keys), nameof(values));

            var valueKeys = new Dictionary<string, dynamic>();

            for (int i =0; i < keys.Length; i ++)
            {
                valueKeys.Add( keys[i], values[i]);
            }
            Values = valueKeys;
            Keys = new List<string>(keys);
        }
    }
}
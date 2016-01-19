using System.Collections.Generic;

namespace Neo4j.Driver
{
    public interface IResultRecordAccessor
    {
        dynamic Value(int index);
        dynamic Value(string key);
        bool ContainsKey(string key);
        IReadOnlyList<string> Keys { get; }
        int Index(string key);
        int Size();
        IReadOnlyDictionary<string, dynamic> Values();
        IReadOnlyList<KeyValuePair<string, dynamic>> OrderedValues();
        bool HasRecord();
        Record Record();
    }
}
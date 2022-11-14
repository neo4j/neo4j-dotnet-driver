using System.Collections.Generic;

namespace Neo4j.Driver.Tests.TestBackend;

internal class ProtocolObjectManager
{
    private static int ObjectCounter { get; set; }
    private Dictionary<string, IProtocolObject> ProtocolObjects { get; } = new();
    public int ObjectCount => ProtocolObjects.Count;

    public static string GenerateUniqueIdString()
    {
        return (ObjectCounter++).ToString();
    }

    public static int GenerateUniqueIdInt()
    {
        return ObjectCounter++;
    }

    public void AddProtocolObject(IProtocolObject obj)
    {
        obj.SetUniqueId(GenerateUniqueIdString());
        ProtocolObjects[obj.uniqueId] = obj;
    }

    public IProtocolObject GetObject(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return ProtocolObjects[id];
    }

    public T GetObject<T>(string id) where T : IProtocolObject
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return (T)ProtocolObjects[id];
    }
}

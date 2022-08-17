using System.Collections.Generic;

namespace Neo4j.Driver.Tests.TestBackend
{    
    internal class ProtocolObjectManager
    {
        private static int ObjectCounter { get; set; } = 0;
        private Dictionary<string, ProtocolObject> ProtocolObjects { get; set; } = new Dictionary<string, ProtocolObject>();
        public int ObjectCount => ProtocolObjects.Count;

        public static string GenerateUniqueIdString()
        {
            return (ObjectCounter++).ToString();
        }

        public static int GenerateUniqueIdInt()
		{
            return (ObjectCounter++);
		}

        public void AddProtocolObject(ProtocolObject obj)
        {
            obj.UniqueId = GenerateUniqueIdString();
            ProtocolObjects[obj.UniqueId] = obj;
        }

        public ProtocolObject GetObject(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return ProtocolObjects[id];
        }


        public T GetObject<T>(string id) where T: ProtocolObject
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return (T)ProtocolObjects[id];
        }
    }
}

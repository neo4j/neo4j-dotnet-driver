using System.Collections.Generic;

namespace Neo4j.Driver.Tests.TestBackend
{    
    internal class ProtocolObjectManager
    {
        private int ObjectCounter { get; set; } = 0;
        private Dictionary<string, IProtocolObject> ProtocolObjects { get; set; } = new Dictionary<string, IProtocolObject>();
        public int ObjectCount { get { return ProtocolObjects.Count; } }

        public string GenerateUniqueId()
        {
            return (ObjectCounter++).ToString();
        }

        public void AddProtocolObject(IProtocolObject obj)
        {
            obj.SetUniqueId(GenerateUniqueId());
            ProtocolObjects[obj.uniqueId] = obj;
        }

        public IProtocolObject GetObject(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return ProtocolObjects[id];
        }
    }
}

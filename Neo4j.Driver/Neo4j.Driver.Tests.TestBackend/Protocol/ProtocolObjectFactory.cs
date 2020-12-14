using System;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal static class ProtocolObjectFactory
    {
        
        public static ProtocolObjectManager ObjManager { get; set; }
       

        public static IProtocolObject CreateObject(Protocol.Types type, string jsonString = null)
        {
            var newObject = (IProtocolObject)CreateNewObjectOfType(Protocol.GetObjectType(type), jsonString, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            if (newObject is null)
                throw new Exception($"Trying to create a none supported object in the ProtocolObjectFactory of type {type}");

            newObject.SetObjectManager(ObjManager);
            ObjManager.AddProtocolObject(newObject);
            return newObject;
        }

        static object CreateNewObjectOfType(Type newType, string jsonString, JsonSerializerSettings jsonSettings = null)
		{
            var settings = jsonSettings ?? new JsonSerializerSettings();
            return string.IsNullOrEmpty(jsonString) ? Activator.CreateInstance(newType) : JsonConvert.DeserializeObject(jsonString, newType, jsonSettings);
        }

        static T CreateNewObjectOfType<T>(string jsonString, JsonSerializerSettings jsonSettings = null) where T : new()
		{
            var settings = jsonSettings ?? new JsonSerializerSettings();  
            return string.IsNullOrEmpty(jsonString) ? new T() : JsonConvert.DeserializeObject<T>(jsonString, settings);
		}
    }
}

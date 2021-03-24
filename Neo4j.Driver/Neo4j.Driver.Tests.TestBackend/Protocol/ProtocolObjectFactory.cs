using System;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal static class ProtocolObjectFactory
    {
        
        public static ProtocolObjectManager ObjManager { get; set; }
       
		public static IProtocolObject CreateObject(Type type, string jsonString = null)
        {
			Protocol.ValidateType(type);

            var newObject = (IProtocolObject)CreateNewObjectOfType(type, jsonString, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore,
																												  MissingMemberHandling = MissingMemberHandling.Error });
			ProcessNewObject(newObject);

			return newObject;
        }
		
		public static T CreateObject<T>(string jsonString = null) where T : IProtocolObject, new()
		{
			return (T)CreateObject(typeof(T), jsonString);			
		}

        private static object CreateNewObjectOfType(Type newType, string jsonString, JsonSerializerSettings jsonSettings = null)
		{
            var settings = jsonSettings ?? new JsonSerializerSettings();
            return string.IsNullOrEmpty(jsonString) ? Activator.CreateInstance(newType) : JsonConvert.DeserializeObject(jsonString, newType, jsonSettings);
        }

        private static T CreateNewObjectOfType<T>(string jsonString, JsonSerializerSettings jsonSettings = null) where T : new()
		{
            var settings = jsonSettings ?? new JsonSerializerSettings();  
            return string.IsNullOrEmpty(jsonString) ? new T() : JsonConvert.DeserializeObject<T>(jsonString, settings);
		}

		private static void ProcessNewObject(IProtocolObject newObject)
		{
			newObject.SetObjectManager(ObjManager);
			ObjManager.AddProtocolObject(newObject);
		}
    }
}

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal static class ProtocolObjectFactory
    {
        
        public static ProtocolObjectManager ObjManager { get; set; }

		public static ProtocolObject CreateObject(string jsonString)
		{
			Type type = GetObjectType(jsonString);
			Protocol.ValidateType(type);
			return CreateObject(type, jsonString);
		}

		public static T CreateObject<T>() where T : ProtocolObject
		{
			Protocol.ValidateType(typeof(T));
			return (T)CreateObject(typeof(T));
		}

		private static ProtocolObject CreateObject(Type type, string jsonString = null)
		{
			try
			{
				var newObject = (ProtocolObject)CreateNewObjectOfType(type, jsonString, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore,
					MissingMemberHandling = MissingMemberHandling.Error
				});
				ProcessNewObject(newObject);

				return newObject;
			}
			catch(JsonException ex)
			{
				throw new Exception($"Json protocol Error: {ex.Message}");
			}
		}

		public static Type GetObjectType(string jsonString)
		{
			var objectTypeName = GetObjectTypeName(jsonString) ;
			Protocol.ValidateType(objectTypeName);
			return Type.GetType(typeof(ProtocolObjectFactory).Namespace + "." + objectTypeName, true);
		}

		private static string GetObjectTypeName(string jsonString)
		{
			JObject jsonObject = JObject.Parse(jsonString);
			return (string)jsonObject["name"];
		}


		public static T CreateObject<T>(string jsonString = null) where T : ProtocolObject, new()
		{
			return (T)CreateObject(jsonString);			
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

		private static void ProcessNewObject(ProtocolObject newObject)
		{
			newObject.SetObjectManager(ObjManager);
			ObjManager.AddProtocolObject(newObject);
		}
    }
}

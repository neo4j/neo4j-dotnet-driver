using Newtonsoft.Json;
    
namespace Neo4j.Driver.Tests.TestBackend
{
    internal class ProtocolResponse
    {
        public string name { get; }
        public object data { get; set; }
       
        public class ResponseType
        {
            public string id { get; set; }
            public string[] keys { get; set; }

        }

        public ProtocolResponse(string newName, string newId): this(newName, newId, null)
        {
        }

        public ProtocolResponse(string newName, string newId, string[] keys)
        {
            data = new ResponseType
            {
                id = newId,
                keys = keys
            };
            name = newName;
        }

        public ProtocolResponse(string newName, object dataType)
        {
            name = newName;
            data = dataType;
        }

        public ProtocolResponse(string newName):this(newName, null, null)
		{
		}


        public string Encode()
        {
            return JsonConvert.SerializeObject(this, 
                new JsonSerializerSettings{ NullValueHandling = NullValueHandling.Ignore });
        }
    }
}

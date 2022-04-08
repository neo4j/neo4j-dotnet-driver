using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class ProtocolResponse
    {
        public string Name { get; }
        public object Data { get; set; }
       
        public class ResponseType
        {
            public string Id { get; set; }
        }

        public ProtocolResponse(string newName, string newId)
        {
            Data = new ResponseType();
            Name = newName;
            ((ResponseType)Data).Id = newId;
        }

        public ProtocolResponse(string newName, object dataType)
        {
            Name = newName;
            Data = dataType;
        }

        public ProtocolResponse(string newName)
        {
            Name = newName;
            Data = null;
        }

        public string Encode()
        {
            return JsonConvert.SerializeObject(this, 
                Formatting.None, 
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
        }
    }
}

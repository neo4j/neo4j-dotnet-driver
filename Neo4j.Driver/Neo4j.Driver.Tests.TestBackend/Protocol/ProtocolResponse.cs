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
        }

        public ProtocolResponse(string newName, string newId)
        {
            data = new ResponseType();
            name = newName;
            ((ResponseType)data).id = newId;
        }

        public ProtocolResponse(string newName, object dataType)
        {
            name = newName;
            data = dataType;
        }

		public ProtocolResponse(string newName)
		{
			name = newName;
			data = null;
		}

        public string Encode()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

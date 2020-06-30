using System.Text.Json;
    
namespace Neo4j.Driver.Tests.TestBackend
{
    internal class Response
    {
        public string name { get; }
        public object data { get; set; }
       
        public class ResponseType
        {
            public string id { get; set; }
        }

        public Response(string newName, string newId)
        {
            data = new ResponseType();
            name = newName;
            ((ResponseType)data).id = newId;
        }

        public Response(string newName, object dataType)
        {
            name = newName;
            data = dataType;
        }

        public string Encode()
        {  
            return JsonSerializer.Serialize<object>(this);
        }
    }
}

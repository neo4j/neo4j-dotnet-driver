using System;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Neo4j.Driver;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionReadTransaction : IProtocolObject
    {   
        public SessionReadTransactionType data { get; set; } = new SessionReadTransactionType();
        [JsonIgnore]
        private string ResultId { get; set; }
        //[JsonIgnore]
        //public IAsyncTransaction Transaction { get; set; }    //TODO: Remove as just here for notes as I figure out the retry system.

        public class SessionReadTransactionType
        {
            public string sessionId { get; set; }  
            public string cypher { get; set; }
        }

        public override async Task Process()
        {
            var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
            await sessionContainer.Session.ReadTransactionAsync(async tx =>
            {   
                IResultCursor cursor = await tx.RunAsync(data.cypher);

                var result = new Result() { Results = cursor };
                ObjManager.AddProtocolObject(result);
                ResultId = result.uniqueId;
            });        
        }

        public override string Respond()
        {
            //return new ProtocolResponse("RetryableTry", uniqueId).Encode(); //TODO: remove as just here for notes as I figure out the retry system.
            return ((Result)ObjManager.GetObject(ResultId)).Respond();
        }
    }
}

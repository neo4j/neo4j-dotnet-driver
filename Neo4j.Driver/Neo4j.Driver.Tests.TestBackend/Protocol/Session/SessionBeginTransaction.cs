using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionBeginTransaction : IProtocolObject
    {
        public SessionBeginTransactionType data { get; set; } = new SessionBeginTransactionType();
        [JsonIgnore]
        public string TransactionId { get; set; }


        [JsonConverter(typeof(BaseSessionTypeJsonConverter<SessionBeginTransactionType>))]
        public class SessionBeginTransactionType : BaseSessionType
        {
        }

		public override async Task Process(Controller controller)
		{
			var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
			var transaction = await sessionContainer.Session.BeginTransactionAsync(data.TransactionConfig);
			TransactionId = controller.TransactionManager.AddTransaction(new TransactionWrapper(transaction, async cursor =>
			{
				var result = ProtocolObjectFactory.CreateObject<Result>();
				result.ResultCursor = cursor;

				return await Task.FromResult<string>(result.uniqueId);
			}));
        }

        public override string Respond()
        {
            return new ProtocolResponse("Transaction", TransactionId).Encode();
        }
    }
}

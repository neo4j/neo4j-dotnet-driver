﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
	class TransactionClose : ProtocolObject
	{
		public TransactionCloseDataType data { get; set; } = new TransactionCloseDataType();

		public class TransactionCloseDataType
		{
			public string txId { get; set; }
		}

		public override async Task ProcessAsync(Controller controller)
		{
			var transactionWrapper = controller.TransactionManager.FindTransaction(data.txId);
			await transactionWrapper.Transaction.DisposeAsync();
		}

		public override string Respond()
		{
			return new ProtocolResponse("Transaction", UniqueId).Encode();
		}
	}
}

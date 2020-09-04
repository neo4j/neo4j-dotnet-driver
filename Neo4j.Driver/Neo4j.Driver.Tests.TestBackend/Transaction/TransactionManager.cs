using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo4j.Driver.Tests.TestBackend.Transaction
{
	class TransactionManager
	{
		private Dictionary<string, IAsyncTransaction> Transactions { get; set; } = new Dictionary<string, IAsyncTransaction>();

		public string AddTransaction(IAsyncTransaction transation)
		{
			var key = ProtocolObjectManager.GenerateUniqueIdString();
			Transactions.Add(key, transation);
			return key;
		}

		public void RemoveTransaction(string key)
		{
			Transactions.Remove(key);
		}

		public IAsyncTransaction FindTransaction(string key)
		{
			return Transactions[key];
		}

	}
}

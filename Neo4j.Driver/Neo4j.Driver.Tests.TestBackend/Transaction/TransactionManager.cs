using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class TransactionWrapper
	{
		public IAsyncTransaction Transaction { get; private set; }
		private Func<IResultCursor, Task<string>> ResultHandler;

		public TransactionWrapper(IAsyncTransaction transaction, Func<IResultCursor, Task<string>>resultHandler)
		{
			Transaction = transaction;
			ResultHandler = resultHandler;
		}

		public async Task<string> ProcessResults(IResultCursor cursor)
		{
			return await ResultHandler(cursor);
		}

	}


	internal class TransactionManager
	{
		private Dictionary<string, TransactionWrapper> Transactions { get; set; } = new Dictionary<string, TransactionWrapper>();

		public string AddTransaction(TransactionWrapper transation)
		{
			var key = ProtocolObjectManager.GenerateUniqueIdString();
			Transactions.Add(key, transation);
			return key;
		}

		public void RemoveTransaction(string key)
		{
			Transactions.Remove(key);
		}

		public TransactionWrapper FindTransaction(string key)
		{
			return Transactions[key];
		}

	}
}

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Neo4j.Driver;
using System.Collections.Generic;
using System.Diagnostics;


namespace Neo4j.Driver.Tests.TestBackend
{
	internal class Result : IProtocolObject
	{
		public ResultType data { get; set; } = new ResultType();
		
        public class ResultType
        {
            public string id { get; set; }
        }

        public override async Task Process()
        {
            //Currently does nothing
            await Task.CompletedTask;
        }

        public override string Respond()
        {
            return new ProtocolResponse("Result", uniqueId).Encode();
        }

		public async virtual Task<IRecord> GetNextRecord()
		{	
			return await Task.FromResult<IRecord>(null);
		}
    }

	internal class TransactionResult : Result 
	{
		[JsonIgnore]
		private List<IRecord> Records { get; set; } = new List<IRecord>();
		[JsonIgnore]
		private int CurrentRecordIndex { get; set; } = 0;

		public async override Task<IRecord> GetNextRecord()
		{
			if (CurrentRecordIndex >= Records.Count)
				return null;

			return await Task.FromResult<IRecord>(Records[CurrentRecordIndex++]);
		}

		public async Task PopulateRecords(IResultCursor cursor)
		{
			await cursor.ForEachAsync(record => Records.Add(record)).ConfigureAwait(false);
		}
	}

	internal class SessionResult : Result
	{
		[JsonIgnore]
		public IResultCursor Results { private get; set; }

		public async override Task<IRecord> GetNextRecord()
		{
			if (await Results.FetchAsync().ConfigureAwait(false))
			{
				return await Task.FromResult<IRecord>(Results.Current);
			}

			return await Task.FromResult<IRecord>(null);
		}

		public async Task ConsumeResults()
		{
			await Results.ConsumeAsync().ConfigureAwait(false);
		}
	}
}

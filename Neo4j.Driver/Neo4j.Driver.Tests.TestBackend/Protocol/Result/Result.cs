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
		[JsonIgnore]
		public IResultCursor ResultCursor { get; set; }

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

		public async Task<IRecord> GetNextRecord()
		{
			if(await ResultCursor.FetchAsync())
			{
				return await Task.FromResult<IRecord>(ResultCursor.Current);
			}

			return await Task.FromResult<IRecord>(null);
		}

        public Task<IRecord> PeekRecord()
        {
            return ResultCursor.PeekAsync();
        }

        public Task<IRecord> SingleAsync() => ResultCursor.SingleAsync();

        public async Task<IResultSummary> ConsumeResults()
		{
			return await ResultCursor.ConsumeAsync().ConfigureAwait(false);
		}

        public Task<List<IRecord>> ToListAsync() => ResultCursor.ToListAsync();

		public async Task PopulateRecords(IResultCursor cursor)
		{
			ResultCursor = cursor;
			await Task.CompletedTask;
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Neo4j.Driver.Tests.TestBackend
{
    internal class TransactionRun : IProtocolObject
    {
        public TransactionRunType data { get; set; } = new TransactionRunType();
        [JsonIgnore]
        private string ResultId { get; set; }

        public class TransactionRunType
        {
            public string txId { get; set; }
            public string cypher { get; set; }
            [JsonProperty("params")]
            [JsonConverter(typeof(QueryParameterConverter))]
            public Dictionary<string, CypherToNativeObject> parameters { get; set; } = new Dictionary<string, CypherToNativeObject>();
        }

        public override async Task Process(Controller controller)
        {
            try
            {
                var transactionWrapper = controller.TransactionManager.FindTransaction(data.txId);

                IResultCursor cursor = await transactionWrapper.Transaction
                    .RunAsync(data.cypher, CypherToNativeObject.ConvertDitctionaryToNative(data.parameters)).ConfigureAwait(false);

                ResultId = await transactionWrapper.ProcessResults(cursor);

            }
            catch (TimeZoneNotFoundException tz)
            {
                throw new DriverExceptionWrapper(tz);
            }
        }

        public override string Respond()
        {
            try
            {
                return ((Result)ObjManager.GetObject(ResultId)).Respond();
            }
            catch (TimeZoneNotFoundException tz)
            {
                throw new DriverExceptionWrapper(tz);
            }
        }
    }
}

﻿using System;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Neo4j.Driver;
using System.Linq;
using System.Diagnostics;


namespace Neo4j.Driver.Tests.TestBackend
{
    internal class ResultNext : IProtocolObject
    {
        public ResultNextType data { get; set; } = new ResultNextType();
        [JsonIgnore]
        public IRecord Records { get; set; }

        public class ResultNextType
        {
            public string resultId { get; set; }
        }

		public override async Task Process()
		{
			var result = (Result)ObjManager.GetObject(data.resultId);
			Records = await result.GetNextRecord();
        }

        public override string Respond()
        {
            if (!(Records is null))
            {
				//Generate list of ordered records
				var valuesList = Records.Keys.Select(v => NativeToCypher.Convert(Records[v]));
                return new ProtocolResponse("Record", new { values = valuesList }).Encode();
            }
            else
            {
				return new ProtocolResponse("NullRecord", (object)null).Encode();
            }
        }
    }
}

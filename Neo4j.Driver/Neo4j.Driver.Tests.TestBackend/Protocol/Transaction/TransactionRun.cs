// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class TransactionRun : ProtocolObject
{
    public TransactionRunType data { get; set; } = new();
    [JsonIgnore] private string ResultId { get; set; }

    private Dictionary<string, object> ConvertParameters(Dictionary<string, CypherToNativeObject> source)
    {
        if (data.parameters == null)
            return null;

        var newParams = new Dictionary<string, object>();

        foreach (var element in source) 
            newParams.Add(element.Key, CypherToNative.Convert(element.Value));

        return newParams;
    }

    public override async Task ProcessAsync(Controller controller)
    {
        try
        {
            var transactionWrapper = controller.TransactionManager.FindTransaction(data.txId);

            var cursor = await transactionWrapper.Transaction
                .RunAsync(data.cypher, ConvertParameters(data.parameters));

            ResultId = await transactionWrapper.ProcessResults(cursor);
        }
        catch (TimeZoneNotFoundException tz)
        {
            throw new DriverExceptionWrapper(tz);
        }
    }

    public override async Task ReactiveProcessAsync(Controller controller)
    {
        try
        {
            var transactionWrapper = controller.ReactiveTransactionManager.FindTransaction(data.txId);

            var cursor = transactionWrapper.Transaction
                .Run(data.cypher, ConvertParameters(data.parameters));

            ResultId = await transactionWrapper.ProcessResults(new RxCursorWrapper(cursor));
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
            return ObjManager.GetObject<Result>(ResultId).Respond();
        }
        catch (TimeZoneNotFoundException tz)
        {
            throw new DriverExceptionWrapper(tz);
        }
    }

    public class TransactionRunType
    {
        public string txId { get; set; }
        public string cypher { get; set; }

        [JsonProperty("params")]
        [JsonConverter(typeof(QueryParameterConverter))]
        public Dictionary<string, CypherToNativeObject> parameters { get; set; } = new();
    }
}
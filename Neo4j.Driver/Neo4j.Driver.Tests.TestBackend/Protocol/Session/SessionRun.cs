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

internal class SessionRun : ProtocolObject
{
    public SessionRunType data { get; set; } = new();

    [JsonIgnore] private string ResultId { get; set; }

    private Dictionary<string, object> ConvertParameters(Dictionary<string, CypherToNativeObject> source)
    {
        if (data.parameters == null)
            return null;

        var newParams = new Dictionary<string, object>();

        foreach (var element in source) newParams.Add(element.Key, CypherToNative.Convert(element.Value));

        return newParams;
    }

    private void TransactionConfig(TransactionConfigBuilder configBuilder)
    {
        try
        {
            if (data.TimeoutSet)
            {
                var timeout = data.timeout.HasValue
                    ? TimeSpan.FromMilliseconds(data.timeout.Value)
                    : default(TimeSpan?);
                configBuilder.WithTimeout(timeout);
            }
        }
        catch (ArgumentOutOfRangeException e) when ((data.timeout ?? 0) < 0 && e.ParamName == "value")
        {
            throw new DriverExceptionWrapper(e);
        }

        if (data.txMeta.Count > 0)
            configBuilder.WithMetadata(data.txMeta);
    }

    public override async Task ProcessAsync()
    {
        var newSession = ObjManager.GetObject<NewSession>(data.sessionId);
        var cursor = await newSession.Session
            .RunAsync(data.cypher, ConvertParameters(data.parameters), TransactionConfig);

        var result = ProtocolObjectFactory.CreateObject<Result>();
        result.ResultCursor = cursor;

        ResultId = result.UniqueId;
    }

    public override Task ReactiveProcessAsync()
    {
        var newSession = ObjManager.GetObject<NewSession>(data.sessionId);
        var cursor = newSession.RxSession
            .Run(data.cypher, ConvertParameters(data.parameters), TransactionConfig);
        
        var result = ProtocolObjectFactory.CreateObject<Result>();
        result.ResultCursor = new RxCursorWrapper(cursor);

        ResultId = result.UniqueId;
        return Task.CompletedTask;
    }

    public override string Respond()
    {
        return ObjManager.GetObject<Result>(ResultId).Respond();
    }

    [JsonConverter(typeof(SessionTypeJsonConverter))]
    public class SessionRunType : BaseSessionType
    {
        public string cypher { get; set; }

        [JsonProperty("params")]
        [JsonConverter(typeof(QueryParameterConverter))]
        public Dictionary<string, CypherToNativeObject> parameters { get; set; } = new();
    }
}
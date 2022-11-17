// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend;

internal class BaseSessionTypeJsonConverter<T> : JsonConverter<T> where T : BaseSessionType, new()
{
    public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override T ReadJson(
        JsonReader reader,
        Type objectType,
        T existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var json = JObject.Load(reader);
        var sessionSettings = new T();
        SetBaseValues(json, sessionSettings);
        return sessionSettings;
    }

    protected void SetBaseValues(JObject jsonObject, T baseSession)
    {
        baseSession.sessionId = jsonObject["sessionId"]?.Value<string>();
        baseSession.txMeta = jsonObject["txMeta"]?.ToObject<Dictionary<string, object>>() ??
            new Dictionary<string, object>();

        baseSession.timeout = jsonObject["timeout"]?.Value<int?>();
        baseSession.TimeoutSet = jsonObject.ContainsKey("timeout");
    }
}

// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

internal class QueryParameterConverter : JsonConverter<Dictionary<string, CypherToNativeObject>>
{
    public override void WriteJson(
        JsonWriter writer,
        Dictionary<string, CypherToNativeObject> value,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override Dictionary<string, CypherToNativeObject> ReadJson(
        JsonReader reader,
        Type objectType,
        Dictionary<string, CypherToNativeObject> existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var token = JObject.Load(reader);
        return JsonCypherParameterParser.ParseParameters(token);
    }

    internal class FullQueryParameterConverter : JsonConverter<Dictionary<string, object>>
    {
        public override void WriteJson(JsonWriter writer, Dictionary<string, object> value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<string, object> ReadJson(
            JsonReader reader,
            Type objectType,
            Dictionary<string, object> existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var token = JObject.Load(reader);
            var parameters = JsonCypherParameterParser.ParseParameters(token);
            return ConvertParameters(parameters);
        }

        public static Dictionary<string, object> ConvertParameters(Dictionary<string, CypherToNativeObject> source)
        {
            if (source == null)
            {
                return null;
            }

            var newParams = new Dictionary<string, object>();

            foreach (var element in source)
            {
                newParams.Add(element.Key, CypherToNative.Convert(element.Value));
            }

            return newParams;
        }
    }
}

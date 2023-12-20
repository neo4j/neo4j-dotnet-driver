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
using Neo4j.Driver.Tests.TestBackend.Types;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend.Protocol.Session;

internal class JsonCypherParameterParser
{
    private static readonly Dictionary<string, Type> dateTimeTypes = new()
    {
        ["CypherDate"] = typeof(DateTimeParameterValue),
        ["CypherTime"] = typeof(DateTimeParameterValue),
        ["CypherLocalTime"] = typeof(DateTimeParameterValue),
        ["CypherDateTime"] = typeof(DateTimeParameterValue),
        ["CypherLocalDateTime"] = typeof(DateTimeParameterValue)
    };

    public static Dictionary<string, CypherToNativeObject> ParseParameters(JToken token)
    {
        if (!(token is JObject parameters))
        {
            return null;
        }

        var result = new Dictionary<string, CypherToNativeObject>();

        foreach (var parameter in parameters.Properties())
        {
            result[parameter.Name] = ExtractParameterFromProperty(parameter.Value as JObject);
        }

        return result;
    }

    public static CypherToNativeObject ExtractParameterFromProperty(JObject parameter)
    {
        if (dateTimeTypes.ContainsKey(parameter["name"].Value<string>()))
        {
            return new CypherToNativeObject
            {
                name = parameter["name"].Value<string>(),
                data = parameter["data"].ToObject<DateTimeParameterValue>()
            };
        }

        if (parameter["name"].Value<string>() == "CypherDuration")
        {
            return new CypherToNativeObject
            {
                name = parameter["name"].Value<string>(),
                data = parameter["data"].ToObject<DurationParameterValue>()
            };
        }

        return new CypherToNativeObject
        {
            name = parameter["name"].Value<string>(),
            data = parameter["data"].ToObject<SimpleValue>()
        };
    }
}

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
using Neo4j.Driver.Tests.TestBackend.Protocol.Session;
using Neo4j.Driver.Tests.TestBackend.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend.Protocol.JsonConverters;

internal class SingleCypherValueConverter : JsonConverter<CypherToNativeObject>
{
    public override void WriteJson(JsonWriter writer, CypherToNativeObject value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override CypherToNativeObject ReadJson(
        JsonReader reader,
        Type objectType,
        CypherToNativeObject existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var token = JObject.Load(reader);
        return JsonCypherParameterParser.ExtractParameterFromProperty(token);
    }
}

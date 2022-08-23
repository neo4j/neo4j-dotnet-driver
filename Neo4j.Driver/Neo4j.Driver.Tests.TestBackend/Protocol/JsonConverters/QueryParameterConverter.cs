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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class QueryParameterConverter : JsonConverter<Dictionary<string, CypherToNativeObject>>
    {
        public override void WriteJson(JsonWriter writer, Dictionary<string, CypherToNativeObject> value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<string, CypherToNativeObject> ReadJson(JsonReader reader, Type objectType, Dictionary<string, CypherToNativeObject> existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var token = JObject.Load(reader);
            return JsonCypherParameterParser.ParseParameters(token);
        }
    }
}
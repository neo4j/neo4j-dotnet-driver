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
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Neo4j.Driver.Internal.QueryApi;

internal class QueryApiListCodec : IQueryApiTypeCodec
{
    public bool CanRead(string typeName)
    {
        throw new NotImplementedException();
    }

    public object Read(JsonElement element, IJsonValueDecoder recurse)
    {
        throw new NotImplementedException();
    }

    public bool CanWrite(object value)
    {
        throw new NotImplementedException();
    }

    public JsonNode Write(object value, IJsonValueEncoder recurse)
    {
        throw new NotImplementedException();
    }
}

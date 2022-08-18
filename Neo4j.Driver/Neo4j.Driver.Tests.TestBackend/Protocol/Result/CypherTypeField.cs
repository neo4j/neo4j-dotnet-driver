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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Types;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class CypherTypeField : ProtocolObject
{
    [JsonProperty("data")] public CypherTypeFieldRequest RequestData { get; set; } = new();

    [JsonIgnore] public object Field { get; set; }

    public override async Task ProcessAsync()
    {
        var result = ObjManager.GetObject<Result>(RequestData.ResultId);
        var record = await result.GetNextRecordAsync();
        var data = record[RequestData.RecordKey];

        Field = data switch
        {
            Node node => ReadProperty(typeof(Node), node, RequestData.Field),
            Relationship rel => ReadProperty(typeof(Relationship), rel, RequestData.Field),
            Path path => ReadPath(path, RequestData.Field),
            _ => throw new Exception("not a cypher type")
        };
    }

    public override string Respond()
    {
        return new ProtocolResponse("Field", new {value = NativeToCypher.Convert(Field)}).Encode();
    }

    private object ReadPath(Path path, string field)
    {
        var request = field.Split(".");

        return request[0] switch
        {
            "nodes" => ReadProperty(typeof(Node), path.Nodes[int.Parse(request[1])], request[2]),
            "relationships" => ReadProperty(typeof(Relationship), path.Relationships[int.Parse(request[1])],
                request[2]),
            _ => throw new Exception("found invalid field path")
        };
    }

    private object ReadProperty(Type type, object obj, string field)
    {
        try
        {
            var property = type
                .GetProperties()
                .Single(x =>
                    string.Equals(x.Name.ToLower(), field.ToLower(), StringComparison.CurrentCultureIgnoreCase));
            return property.GetValue(obj);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException invalidOp)
        {
            throw new DriverExceptionWrapper(invalidOp);
        }
    }

    public class CypherTypeFieldRequest
    {
        public string ResultId { get; set; }
        public string RecordKey { get; set; }
        public string Field { get; set; }
        public string Type { get; set; }
    }
}
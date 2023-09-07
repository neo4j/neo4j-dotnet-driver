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

namespace Neo4j.Driver.Preview.Mapping;

public static class RecordNodeExtensions
{
    public static T AsObject<T>(this IRecord record) where T : new()
    {
        return RecordObjectMapping.Map<T>(record);
    }

    public static T GetValue<T>(this IRecord record, string key)
    {
        return record[key].As<T>();
    }

    public static INode GetNode(this IRecord record, string key)
    {
        return record.GetValue<INode>(key);
    }

    public static T GetValue<T>(this INode node, string key)
    {
        return node[key].As<T>();
    }
}

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

namespace Neo4j.Driver.Preview.Mapping;

internal interface IRecordPathFinder
{
    bool TryGetValueByPath(IRecord record, string path, out object value);
}

internal class RecordPathFinder : IRecordPathFinder
{
    private bool PathCompare(string path, string field)
    {
        return string.Equals(path, field, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <inheritdoc />
    public bool TryGetValueByPath(IRecord record, string path, out object value)
    {
        value = null;

        foreach (var field in record.Keys)
        {
            if (PathCompare(path, field))
            {
                // we can return the value directly if the field name matches the path
                value = record[field];
                return true;
            }

            // if the field contains an entity or dictionary we can drill down and
            // check if the path matches any of the properties
            var properties = record[field] switch
            {
                IEntity entity => entity.Properties,
                IReadOnlyDictionary<string, object> dict => dict,
                _ => null
            };

            if (properties is null)
            {
                // if the field is not an entity or dictionary we can't drill down further
                continue;
            }

            foreach (var property in properties)
            {
                // if there is a property that matches the path in the dictionary, or if the path
                // matches the field name + property name, we can return the value
                if (
                    PathCompare(path, property.Key) ||
                    PathCompare(path, $"{field}.{property.Key}"))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        return false;
    }
}

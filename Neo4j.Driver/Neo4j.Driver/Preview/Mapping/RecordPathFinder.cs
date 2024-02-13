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

namespace Neo4j.Driver.Preview.Mapping;

internal interface IRecordPathFinder
{
    bool TryGetValueByPath(IRecord record, string path, out object value);
}

internal class RecordPathFinder : IRecordPathFinder
{
    /// <inheritdoc />
    public bool TryGetValueByPath(IRecord record, string path, out object value)
    {
        value = null;

        // if the path matches a field name, we can return the value directly
        if (record.TryGetValueByCaseInsensitiveKey(path, out value))
        {
            return true;
        }

        // if there's a dot in the path, we can try to split it and check if the first part
        // matches a field name and the second part matches a property name
        var dotIndex = path.IndexOf('.');
        if (dotIndex > 0)
        {
            var field = path.Substring(0, dotIndex);
            var property = path.Substring(dotIndex + 1);

            if (!record.TryGetValueByCaseInsensitiveKey(field, out var fieldValue))
            {
                return false;
            }

            var dictAsRecord = fieldValue switch
            {
                IEntity entity => new DictAsRecord(entity.Properties, record),
                IReadOnlyDictionary<string, object> dict => new DictAsRecord(dict, record),
                _ => null
            };

            if (dictAsRecord is not null)
            {
                return dictAsRecord.TryGetValueByCaseInsensitiveKey(property, out value);
            }
        }

        // check any values on the record that are entities or dictionaries, in case
        // the path is a property on one of those
        foreach (var key in record.Keys)
        {
            var dictAsRecord = record[key] switch
            {
                IEntity entity => new DictAsRecord(entity.Properties, record),
                IReadOnlyDictionary<string, object> dict => new DictAsRecord(dict, record),
                _ => null
            };

            if(dictAsRecord is not null && TryGetValueByPath(dictAsRecord, path, out value))
            {
                return true;
            }
        }

        return false;
    }
}

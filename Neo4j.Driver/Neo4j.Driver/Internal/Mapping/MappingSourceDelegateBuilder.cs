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

using Neo4j.Driver.Mapping;

namespace Neo4j.Driver.Internal.Mapping;

internal delegate bool MappingValueDelegate(IRecord record, out object value);

internal interface IMappingSourceDelegateBuilder
{
    MappingValueDelegate GetMappingDelegate(EntityMappingInfo entityMappingInfo);
}

internal class MappingSourceDelegateBuilder : IMappingSourceDelegateBuilder
{
    private readonly IRecordPathFinder _pathFinder = new RecordPathFinder();

    public MappingValueDelegate GetMappingDelegate(EntityMappingInfo entityMappingInfo)
    {
        return (record, out value) =>
        {
            var translate = !entityMappingInfo.Explicit;

            if (!_pathFinder.TryGetValueByPath(record, entityMappingInfo.Path, translate, out value))
            {
                value = entityMappingInfo.DefaultValue;
                return entityMappingInfo.Optional;
            }

            var (result, returnValue) = (entityMappingInfo.EntityMappingSource, value) switch
            {
                (EntityMappingSource.NodeLabel, INode node) => (true, node.Labels),
                (EntityMappingSource.RelationshipType, IRelationship relationship) => (true, relationship.Type),
                _ => (true, value)
            };

            value = returnValue;
            return result;
        };
    }
}



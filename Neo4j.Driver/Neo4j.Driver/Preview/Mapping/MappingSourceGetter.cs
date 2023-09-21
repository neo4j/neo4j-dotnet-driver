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

namespace Neo4j.Driver.Preview.Mapping
{
    internal delegate bool TryGetMapSourceDelegate(
        IRecord record,
        out object value);

    internal interface IMappingSourceGetter
    {
        TryGetMapSourceDelegate GetMappingDelegate(InternalMappingSource mappingSource);
    }

    internal class MappingSourceGetter : IMappingSourceGetter
    {
        private IRecordPathFinder _pathFinder = new RecordPathFinder();

        /// <inheritdoc />
        public TryGetMapSourceDelegate GetMappingDelegate(InternalMappingSource mappingSource)
        {
            bool TryGetValue(IRecord record, out object value)
            {
                if (!_pathFinder.TryGetPath(record, mappingSource.Path, out var foundValue))
                {
                    value = null;
                    return false;
                }

                switch (mappingSource)
                {
                    case { EntityMappingSource: EntityMappingSource.NodeLabel }
                        when foundValue is INode node:
                    {
                        value = node.Labels;
                        return true;
                    }

                    case { EntityMappingSource: EntityMappingSource.RelationshipType }
                        when foundValue is IRelationship relationship:
                    {
                        value = relationship.Type;
                        return true;
                    }

                    default:
                        value = foundValue;
                        return true;
                }
            }

            return TryGetValue;
        }
    }
}

// Copyright (c) "Neo4j"
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

namespace Neo4j.Driver.Internal.Types
{
    internal class Relationship : IRelationship
    {
        [Obsolete("Replaced by ElementId, Will be removed in 6.0")]
        public long Id { get; set; }

        [Obsolete("Replaced by StartNodeElementId, Will be removed in 6.0")]
        public long StartNodeId { get; set; }
        [Obsolete("Replaced by EndNodeElementId, Will be removed in 6.0")]
        public long EndNodeId { get; set; }
        public string Type { get; }
        
        public string ElementId { get; }
        public string StartNodeElementId { get; internal set; }
        public string EndNodeElementId { get; internal set; }
        public IReadOnlyDictionary<string, object> Properties { get; }
        public object this[string key] => Properties[key];
       
        public Relationship(long id, long startId, long endId, string relType,
            IReadOnlyDictionary<string, object> props)
        {
            Id = id;
            StartNodeId = startId;
            EndNodeId = endId;

            ElementId = id.ToString();
            StartNodeElementId = startId.ToString();
            EndNodeElementId = endId.ToString();
            Type = relType;
            Properties = props;
        }
        
        public Relationship(long id, string elementId, long startId, long endId, string startElementId, string endElementId, 
            string relType,
            IReadOnlyDictionary<string, object> props)
        {
            Id = id;
            StartNodeId = startId;
            EndNodeId = endId;

            ElementId = elementId;
            StartNodeElementId = startElementId;
            EndNodeElementId = endElementId;

            Type = relType;
            Properties = props;
        }


        public bool Equals(IRelationship other)
        {
            if (other == null)
                return false;
            
            if (ReferenceEquals(this, other))
                return true;
            
            return Equals(ElementId, other.ElementId);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IRelationship);
        }

        public override int GetHashCode()
        {
            return ElementId.GetHashCode();
        }

        internal void SetStartAndEnd(INode start, INode end)
        {
            StartNodeId = start.Id;
            EndNodeId = end.Id;
            StartNodeElementId = start.ElementId;
            EndNodeElementId = end.ElementId;
        }
    }
}

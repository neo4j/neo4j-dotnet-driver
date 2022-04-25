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
using Neo4j.Driver.Internal.Serialization;

namespace Neo4j.Driver.Internal.Types
{
    internal class Relationship : IRelationship
    {
        private long _id = -1;
        private long _startNodeId = -1;
        private long _endNodeId = -1;
        private readonly bool _throwOnIdRead = false;


        [Obsolete("Replaced by ElementId, Will be removed in 6.0")]
        public long Id
        {
            get
            {
                if (_throwOnIdRead)
                    throw new InvalidOperationException("Id is not compatible with server. use ElementId");
                return _id;
            }
            set => _id = value;
        }

        [Obsolete("Replaced by StartNodeElementId, Will be removed in 6.0")]
        public long StartNodeId
        {
            get
            {
                if (_throwOnIdRead)
                    throw new InvalidOperationException("StartNodeId is not compatible with server. use StartNodeElementId");
                return _startNodeId;
            }
            internal set => _startNodeId = value;
        }

        [Obsolete("Replaced by EndNodeElementId, Will be removed in 6.0")]
        public long EndNodeId
        {
            get
            {
                if (_throwOnIdRead)
                    throw new InvalidOperationException("EndNodeId is not compatible with server. use EndNodeElementId");
                return _endNodeId;
            }
            internal set => _endNodeId = value;
        }

        public string Type { get; }
        
        public string ElementId { get; }

        public T ConvertProperties<T>() where T : new()
        {
            return Neo4jSerialization.Convert<T>(Properties);
        }

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
        
        public Relationship(string id, string startId, string endId, string relType,
            IReadOnlyDictionary<string, object> props)
        {
            _throwOnIdRead = true;

            ElementId = id;
            StartNodeElementId = startId;
            EndNodeElementId = endId; 

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
            if (!_throwOnIdRead)
            {
                StartNodeId = start.Id;
                EndNodeId = end.Id;
            }
            StartNodeElementId = start.ElementId;
            EndNodeElementId = end.ElementId;
        }
    }

    internal class Relationship<T> : Relationship, IRelationship<T> where T: new()
    {
        public Relationship(long id, long startId, long endId, string relType, IReadOnlyDictionary<string, object> props) : base(id, startId, endId, relType, props)
        {
        }

        public Relationship(string id, string startId, string endId, string relType, IReadOnlyDictionary<string, object> props) : base(id, startId, endId, relType, props)
        {
        }

        public Relationship(long id, string elementId, long startId, long endId, string startElementId, string endElementId, string relType, IReadOnlyDictionary<string, object> props) : base(id, elementId, startId, endId, startElementId, endElementId, relType, props)
        {
        }

        public T Data { get; }
    }
}

// Copyright (c) 2002-2020 "Neo4j,"
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace Neo4j.Driver.Internal.Types
{
    internal class Node : INode
    {
        public long Id { get; }
        public IReadOnlyList<string> Labels { get; }
        public IReadOnlyDictionary<string, object> Properties { get; }
        public object this[string key] => Properties[key];

        public Node(long id, IReadOnlyList<string> lables, IReadOnlyDictionary<string, object> prop)
        {
            Id = id;
            Labels = lables;
            Properties = prop;
        }

        public bool Equals(INode other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as INode);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}

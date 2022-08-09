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

using Neo4j.Driver.Internal.IO.ValueSerializers;
using FailureMessageSerializer = Neo4j.Driver.Internal.IO.MessageSerializers.V5.FailureMessageSerializer;
using V4_4 = Neo4j.Driver.Internal.IO.MessageSerializers.V4_4;
using V5_0 = Neo4j.Driver.Internal.IO.MessageSerializers.V5_0;

namespace Neo4j.Driver.Internal.Protocol
{
    class BoltProtocolV5_0MessageFormat : BoltProtocolV4_4MessageFormat
    {
        public BoltProtocolV5_0MessageFormat() : base(true)
        {
            RemoveHandler<NodeSerializer>();
            AddHandler<ElementNodeSerializer>();

            RemoveHandler<RelationshipSerializer>();
            AddHandler<ElementRelationshipSerializer>();

            RemoveHandler<UnboundRelationshipSerializer>();
            AddHandler<ElementUnboundRelationshipSerializer>();

            RemoveHandler<IO.MessageSerializers.V3.FailureMessageSerializer>();
            AddHandler<FailureMessageSerializer>();

            RemoveHandler<V4_4.HelloMessageSerializer>();
            AddHandler<V5_0.HelloMessageSerializer>();
        }
    }

}
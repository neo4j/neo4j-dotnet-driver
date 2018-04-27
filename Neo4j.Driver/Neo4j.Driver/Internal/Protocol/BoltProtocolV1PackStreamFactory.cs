// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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

using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.StructHandlers;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV1PackStreamFactory: PackStreamFactory
    {

        internal BoltProtocolV1PackStreamFactory(bool supportBytes)
            : base(supportBytes)
        {
            // Request Message Types
            AddHandler<InitMessageHandler>();
            AddHandler<RunMessageHandler>();
            AddHandler<PullAllMessageHandler>();
            AddHandler<DiscardAllMessageHandler>();
            AddHandler<ResetMessageHandler>();
            AddHandler<AckFailureMessageHandler>();

            // Response Message Types
            AddHandler<FailureMessageHandler>();
            AddHandler<IgnoredMessageHandler>();
            AddHandler<RecordMessageHandler>();
            AddHandler<SuccessMessageHandler>();

            // Struct Data Types
            AddHandler<NodeHandler>();
            AddHandler<RelationshipHandler>();
            AddHandler<UnboundRelationshipHandler>();
            AddHandler<PathHandler>();
        }

    }
}
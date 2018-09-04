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
using Neo4j.Driver.Internal.IO.MessageHandlers;
using Neo4j.Driver.Internal.IO.MessageHandlers.V3;
using Neo4j.Driver.Internal.IO.ValueHandlers;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV3MessageFormat: MessageFormat
    {
        #region Message Constants

        public const byte MsgHello = 0x01;
        public const byte MsgGoodbye = 0x02;
        public const byte MsgBegin = 0x11;
        public const byte MsgCommit = 0x12;
        public const byte MsgRollback = 0x13;

        #endregion Consts
        
        internal BoltProtocolV3MessageFormat()
            : base(true)
        {
            // BoltV3 Request Message Types
            AddHandler<HelloMessageHandler>();
            AddHandler<RunWithMetadataMessageHandler>();
            AddHandler<BeginMessageHandler>();
            AddHandler<CommitMessageHandler>();
            AddHandler<RollbackMessageHandler>();
            
            AddHandler<PullAllMessageHandler>();
            AddHandler<DiscardAllMessageHandler>();
            AddHandler<ResetMessageHandler>();

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
            
            // Add V2 Spatial Types
            AddHandler<PointHandler>();

            // Add V2 Temporal Types
            AddHandler<LocalDateHandler>();
            AddHandler<LocalTimeHandler>();
            AddHandler<LocalDateTimeHandler>();
            AddHandler<OffsetTimeHandler>();
            AddHandler<ZonedDateTimeHandler>();
            AddHandler<DurationHandler>();

            // Add BCL Handlers
            AddHandler<SystemDateTimeHandler>();
            AddHandler<SystemDateTimeOffsetHandler>();
            AddHandler<SystemTimeSpanHandler>();
        }
    }
}
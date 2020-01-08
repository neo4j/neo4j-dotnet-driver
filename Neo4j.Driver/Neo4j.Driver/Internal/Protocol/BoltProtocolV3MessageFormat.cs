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

using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.IO.MessageSerializers.V3;
using Neo4j.Driver.Internal.IO.ValueSerializers;
using Neo4j.Driver.Internal.IO.ValueSerializers.Temporal;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV3MessageFormat : MessageFormat
    {
        #region Message Constants Inherited Over Older Versions

        public const byte MsgReset = 0x0F;
        public const byte MsgRun = 0x10;
        public const byte MsgDiscardAll = 0x2F;
        public const byte MsgPullAll = 0x3F;

        public const byte MsgRecord = 0x71;
        public const byte MsgSuccess = 0x70;
        public const byte MsgIgnored = 0x7E;
        public const byte MsgFailure = 0x7F;

        #endregion

        #region Message Constants

        public const byte MsgHello = 0x01;
        public const byte MsgGoodbye = 0x02;
        public const byte MsgBegin = 0x11;
        public const byte MsgCommit = 0x12;
        public const byte MsgRollback = 0x13;

        #endregion

        internal BoltProtocolV3MessageFormat()
        {
            // BoltV3 Request Message Types
            AddHandler<HelloMessageSerializer>();
            AddHandler<RunWithMetadataMessageSerializer>();
            AddHandler<BeginMessageSerializer>();
            AddHandler<CommitMessageSerializer>();
            AddHandler<RollbackMessageSerializer>();

            // BoltV3 optional Goodbye
            AddHandler<GoodbyeMessageSerializer>();

            AddHandler<PullAllMessageSerializer>();
            AddHandler<DiscardAllMessageSerializer>();
            AddHandler<ResetMessageSerializer>();

            // Response Message Types
            AddHandler<FailureMessageSerializer>();
            AddHandler<IgnoredMessageSerializer>();
            AddHandler<RecordMessageSerializer>();
            AddHandler<SuccessMessageSerializer>();

            // Struct Data Types
            AddHandler<NodeSerializer>();
            AddHandler<RelationshipSerializer>();
            AddHandler<UnboundRelationshipSerializer>();
            AddHandler<PathSerializer>();

            // Add V2 Spatial Types
            AddHandler<PointSerializer>();

            // Add V2 Temporal Types
            AddHandler<LocalDateSerializer>();
            AddHandler<LocalTimeSerializer>();
            AddHandler<LocalDateTimeSerializer>();
            AddHandler<OffsetTimeSerializer>();
            AddHandler<ZonedDateTimeSerializer>();
            AddHandler<DurationSerializer>();

            // Add BCL Handlers
            AddHandler<SystemDateTimeSerializer>();
            AddHandler<SystemDateTimeOffsetHandler>();
            AddHandler<SystemTimeSpanSerializer>();
        }
    }
}
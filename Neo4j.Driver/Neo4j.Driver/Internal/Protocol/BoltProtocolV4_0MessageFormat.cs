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
using Neo4j.Driver.Internal.IO.MessageSerializers.V4;
using Neo4j.Driver.Internal.IO.ValueSerializers;


namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV4_0MessageFormat : BoltProtocolV3MessageFormat
    {
        #region Message Constants

        public const byte MsgDiscardN = 0x2F;
        public const byte MsgPullN = 0x3F;

        #endregion

        internal BoltProtocolV4_0MessageFormat()
        {
            RemoveHandler<PullAllMessageSerializer>();
            AddHandler<PullMessageSerializer>();

            RemoveHandler<DiscardAllMessageSerializer>();
            AddHandler<DiscardMessageSerializer>();
        }
    }
}
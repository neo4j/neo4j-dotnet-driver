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

using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Internal.Protocol
{
    internal static class BoltProtocolMessageFormat
    {
        public static readonly IMessageFormat V3 = new BoltProtocolV3MessageFormat();

        public static readonly IMessageFormat V4 = new BoltProtocolV4_0MessageFormat();

        public static readonly IMessageFormat V4_1 = new BoltProtocolV4_1MessageFormat();

        public static readonly IMessageFormat V4_2 = new BoltProtocolV4_2MessageFormat();

        public static readonly IMessageFormat V4_3 = new BoltProtocolV4_3MessageFormat(false);

        public static readonly IMessageFormat V4_3Utc = new BoltProtocolV4_3MessageFormat(true);

		public static readonly IMessageFormat V4_4 = new BoltProtocolV4_4MessageFormat(false);
        public static readonly IMessageFormat V4_4Utc = new BoltProtocolV4_4MessageFormat(true);

        public static readonly IMessageFormat V5_0 = new BoltProtocolV5_0MessageFormat();
    }
}

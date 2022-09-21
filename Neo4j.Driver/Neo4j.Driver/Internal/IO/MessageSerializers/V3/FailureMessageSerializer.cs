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

using System.Collections.Generic;
using Neo4j.Driver.Internal.Messaging;
using static Neo4j.Driver.Internal.Protocol.MessageFormat;

namespace Neo4j.Driver.Internal.IO.MessageSerializers.V3
{
    internal class FailureMessageSerializer : ReadOnlySerializer
    {
        public override IEnumerable<byte> ReadableStructs => new[] {MsgFailure};

        public override object Deserialize(PackStreamReader reader, byte signature, long size)
        {
            var values = reader.ReadMap();
            var code = values["code"]?.ToString();
            var message = values["message"]?.ToString();

            if (code == "Neo.TransientError.Transaction.Terminated")
                code = "Neo.ClientError.Transaction.Terminated";
            if (code == "Neo.TransientError.Transaction.LockClientStopped")
                code = "Neo.ClientError.Transaction.LockClientStopped";

            return new FailureMessage(code, message);
        }
    }
}

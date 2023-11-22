// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO.MessageSerializers;

internal sealed class FailureMessageSerializer : ReadOnlySerializer
{
    internal static FailureMessageSerializer Instance = new();

    private static readonly byte[] StructTags = { MessageFormat.MsgFailure };
    public override IEnumerable<byte> ReadableStructs => StructTags;

    public override object Deserialize(
        BoltProtocolVersion boltProtocolVersion,
        PackStreamReader reader,
        byte _,
        long __)
    {
        var values = reader.ReadMap();
        var code = values["code"]?.ToString();
        var message = values["message"]?.ToString();

        // codes were fixed in bolt 5, so we need to interpret these codes.
        if (boltProtocolVersion.MajorVersion < 5)
        {
            if (code == "Neo.TransientError.Transaction.Terminated")
            {
                code = "Neo.ClientError.Transaction.Terminated";
            }

            if (code == "Neo.TransientError.Transaction.LockClientStopped")
            {
                code = "Neo.ClientError.Transaction.LockClientStopped";
            }
        }

        return new FailureMessage(code, message);
    }

    public override object Deserialize(PackStreamReader reader)
    {
        // overload not required for this serializer.
        throw new NotImplementedException();
    }
}

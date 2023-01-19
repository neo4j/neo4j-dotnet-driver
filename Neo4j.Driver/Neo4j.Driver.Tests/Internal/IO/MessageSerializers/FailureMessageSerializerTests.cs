// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

namespace Neo4j.Driver.Internal.IO.MessageSerializers
{
    public class FailureMessageSerializerTests
    {
        [Fact]
        public void StructTagsAreSuccess()
        {
            FailureMessageSerializer.Instance.ReadableStructs.Should().ContainEquivalentOf(MessageFormat.MsgFailure);
        }

        [Theory]
        [InlineData(3, 0, "Neo.TransientError.Transaction.Terminated", "Neo.TransientError.Transaction.Terminated")]
        [InlineData(4, 0, "Neo.TransientError.Transaction.Terminated", "Neo.TransientError.Transaction.Terminated")]
        [InlineData(4, 1, "Neo.TransientError.Transaction.Terminated", "Neo.TransientError.Transaction.Terminated")]
        [InlineData(4, 2, "Neo.TransientError.Transaction.Terminated", "Neo.TransientError.Transaction.Terminated")]
        [InlineData(4, 3, "Neo.TransientError.Transaction.Terminated", "Neo.TransientError.Transaction.Terminated")]
        [InlineData(4, 4, "Neo.TransientError.Transaction.Terminated", "Neo.TransientError.Transaction.Terminated")]
        [InlineData(5, 0, "Neo.TransientError.Transaction.Terminated", "Neo.ClientError.Transaction.Terminated")]
        [InlineData(
            5,
            0,
            "Neo.TransientError.Transaction.LockClientStopped",
            "Neo.ClientError.Transaction.LockClientStopped")]
        [InlineData(5, 0, "Neo.TransientError.Transaction.Unrelated", "Neo.ClientError.Transaction.Unrelated")]
        [InlineData(6, 0, "Neo.TransientError.Transaction.Terminated", "Neo.ClientError.Transaction.Terminated")]
        [InlineData(
            6,
            0,
            "Neo.TransientError.Transaction.LockClientStopped",
            "Neo.ClientError.Transaction.LockClientStopped")]
        [InlineData(6, 0, "Neo.TransientError.Transaction.Unrelated", "Neo.ClientError.Transaction.Unrelated")]
        public void ShouldDeserialize(int major, int minor, string inCode, string outCode)
        {
            using var memory = new MemoryStream();

            var boltProtocolVersion = new BoltProtocolVersion(major, minor);
            var format = new MessageFormat(boltProtocolVersion);

            var psw = new PackStreamWriter(format, memory);

            var value = new Dictionary<string, object>() as IDictionary<string, object>;
            value.Add("code", inCode);
            value.Add("message", "message text.");
            psw.WriteDictionary(value);
            memory.Position = 0;

            var reader = new PackStreamReader(format, memory, new ByteBuffers());

            var message = FailureMessageSerializer.Instance.Deserialize(boltProtocolVersion, reader, 0, 0);

            message.Should().BeOfType<FailureMessage>().Which.Message.Should().Be("message text.");
            message.Should().BeOfType<FailureMessage>().Which.Code.Should().Be(outCode);
        }
    }
}

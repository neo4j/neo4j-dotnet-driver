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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Messaging.V4_4;
using Neo4j.Driver.Internal.Protocol;
using Xunit;


namespace Neo4j.Driver.Internal.IO.MessageSerializers.V4_4
{
	public class HelloMessageSerializerTests : PackStreamSerializerTests
	{
		internal override IPackStreamSerializer SerializerUnderTest => new HelloMessageSerializer();

		[Fact]
		public void ShouldThrowOnDeserialize()
		{
			var handler = SerializerUnderTest;

			var ex = Record.Exception(() =>
				handler.Deserialize(Mock.Of<IPackStreamReader>(), BoltProtocolV4_4MessageFormat.MsgBegin, 2));

			ex.Should().NotBeNull();
			ex.Should().BeOfType<ProtocolException>();
		}

		[Fact]
		public void ShouldSerialize()
		{
			var writerMachine = CreateWriterMachine();
			var writer = writerMachine.Writer();
			var authToken = new Dictionary<string, object> { {"scheme", "basic"},
															 {"principal", "username"},
															 {"credentials", "password"} };
			var routingContext = new Dictionary<string, string> { { "contextKey", "contextValue" } };


			writer.Write(new HelloMessage("Client-Version/1.0", authToken, routingContext));

			var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
			var reader = readerMachine.Reader();

			reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
			reader.ReadStructHeader().Should().Be(1);
			reader.ReadStructSignature().Should().Be(BoltProtocolV4_4MessageFormat.MsgHello);

			var readMap = reader.ReadMap();
            readMap.Should().HaveCount(6).And.Contain(
                new[]
                {
                    new KeyValuePair<string, object>("user_agent", "Client-Version/1.0"),
                    new KeyValuePair<string, object>("scheme", "basic"),
                    new KeyValuePair<string, object>("principal", "username"),
                    new KeyValuePair<string, object>("credentials", "password")
                });

            readMap.Should().ContainKey("patch_bolt")
                .WhichValue.Should().BeOfType<List<object>>()
                .Which.Should().Contain("utc")
                .And.HaveCount(1);

            readMap.Should().ContainKey("routing")
                .WhichValue.Should().BeOfType<Dictionary<string, object>>()
                .Which.Should().Contain(new KeyValuePair<string, object>("contextKey", "contextValue"))
                .And.HaveCount(1);
        }

		[Fact]
		public void ShouldSerializeEmptyMapWhenAuthTokenIsNull()
		{
			var writerMachine = CreateWriterMachine();
			var writer = writerMachine.Writer();
			var routingContext = new Dictionary<string, string> { { "contextKey", "contextValue" } };

			writer.Write(new HelloMessage("Client-Version/1.0", null, routingContext));

			var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
			var reader = readerMachine.Reader();

			reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
			reader.ReadStructHeader().Should().Be(1);
			reader.ReadStructSignature().Should().Be(BoltProtocolV4_4MessageFormat.MsgHello);

			var readMap = reader.ReadMap();
			readMap.Should().NotBeNull().And.HaveCount(3).And.Contain(
				new[]
				{
					new KeyValuePair<string, object>("user_agent", "Client-Version/1.0"),
				});

            readMap.Should().ContainKey("patch_bolt")
                .WhichValue.Should().BeOfType<List<object>>()
                .Which.Should().Contain("utc")
                .And.HaveCount(1);

            readMap.Should().ContainKey("routing")
                .WhichValue.Should().BeOfType<Dictionary<string, object>>()
                .Which.Should().Contain(new KeyValuePair<string, object>("contextKey", "contextValue"))
                .And.HaveCount(1);
        }

		[Fact]
		public void ShouldSerializeWithNullRoutingContext()
		{
			var writerMachine = CreateWriterMachine();
			var writer = writerMachine.Writer();

			writer.Write(new HelloMessage("Client-Version/1.0", null, null));

			var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
			var reader = readerMachine.Reader();

			reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
			reader.ReadStructHeader().Should().Be(1);
			reader.ReadStructSignature().Should().Be(BoltProtocolV4_4MessageFormat.MsgHello);

            var readMap = reader.ReadMap();
            readMap.Should().NotBeNull().And.HaveCount(3).And.Contain(
                new[]
                {
                    new KeyValuePair<string, object>("user_agent", "Client-Version/1.0"),
                });

            readMap.Should().ContainKey("patch_bolt")
                .WhichValue.Should().BeOfType<List<object>>()
                .Which.Should().Contain("utc")
                .And.HaveCount(1);

            readMap.Should().ContainKey("routing")
                .WhichValue.Should().BeNull();
        }

		[Fact]
		public void ShouldSerializeWithEmptyRoutingContext()
		{
			var writerMachine = CreateWriterMachine();
			var writer = writerMachine.Writer();

			writer.Write(new HelloMessage("Client-Version/1.0", null, new Dictionary<string, string>()));

			var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
			var reader = readerMachine.Reader();

			reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
			reader.ReadStructHeader().Should().Be(1);
			reader.ReadStructSignature().Should().Be(BoltProtocolV4_4MessageFormat.MsgHello);

            var readMap = reader.ReadMap();
            readMap.Should().NotBeNull().And.HaveCount(3).And.Contain(
                new[]
                {
                    new KeyValuePair<string, object>("user_agent", "Client-Version/1.0"),
                });

            readMap.Should().ContainKey("patch_bolt")
                .WhichValue.Should().BeOfType<List<object>>()
                .Which.Should().Contain("utc")
                .And.HaveCount(1);

            readMap.Should().ContainKey("routing")
                .WhichValue.Should().BeOfType<Dictionary<string, object>>()
                .Which.Should().HaveCount(0);
        }
	}
}

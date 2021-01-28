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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Moq;
using FluentAssertions;
using Neo4j.Driver.Internal.Messaging.V4_3;
using Neo4j.Driver.Internal.Protocol;
using System.Collections.Immutable;

namespace Neo4j.Driver.Internal.IO.MessageSerializers.V4_3
{
	public class RouteMessageSerializerTests : PackStreamSerializerTests
	{
		internal override IPackStreamSerializer SerializerUnderTest => new RouteMessageSerializer();

		[Fact]
		public void ShouldThrowOnDeserialize()
		{
			var handler = SerializerUnderTest;

			var ex = Record.Exception(() => handler.Deserialize(Mock.Of<IPackStreamReader>(), BoltProtocolV4_3MessageFormat.MsgBegin, 2));

			ex.Should().NotBeNull();
			ex.Should().BeOfType<ProtocolException>();
		}

		[Fact]
		public void ShouldSerialize()
		{
			const string db = "adb";
			var writerMachine = CreateWriterMachine();
			var writer = writerMachine.Writer();
			var routingContext = new Dictionary<string, string> { {"ContextKey1", "ContextValue1"},
																  {"ContextKey2", "ContextValue2"},
																  {"ContextKey3", "ContextValue3"} };

			writer.Write(new RouteMessage(routingContext, db));

			var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
			var reader = readerMachine.Reader();

			reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
			reader.ReadStructHeader().Should().Be(2);
			reader.ReadStructSignature().Should().Be(BoltProtocolV4_3MessageFormat.MsgRoute);

			var readMap = reader.ReadMap();
			readMap.Should().HaveCount(3).And.Contain(new[] { new KeyValuePair<string, object>( "ContextKey1", "ContextValue1" ),
															  new KeyValuePair<string, object>( "ContextKey2", "ContextValue2" ),
															  new KeyValuePair<string, object>( "ContextKey3", "ContextValue3" ) });
		}

        [Fact]
        public void ShouldSerializeWithNullRoutingContext()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(new RouteMessage(null, "adb"));

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(2);
            reader.ReadStructSignature().Should().Be(BoltProtocolV4_3MessageFormat.MsgRoute);

            var readMap = reader.ReadMap();
            readMap.Should().NotBeNull().And.HaveCount(0);
        }

        [Fact]
        public void ShouldSerializeWithEmptyRoutingContext()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(new RouteMessage(new Dictionary<string, string>(), "adb"));

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(2);
            reader.ReadStructSignature().Should().Be(BoltProtocolV4_3MessageFormat.MsgRoute);

            var readMap = reader.ReadMap();
			readMap.Should().NotBeNull().And.HaveCount(0);
        }
    }
}

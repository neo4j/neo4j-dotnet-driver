// Copyright (c) 2002-2018 "Neo4j,"
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO
{
    public class BoltReaderTests
    {

        public class ReadMethod
        {

            [Fact]
            public void ShouldReadSuccessMessage()
            {
                var dict = (IDictionary<string, object>) null;
                var mockInput =
                    IOExtensions.CreateMockStream("00 14 B1 70 A1 86  66 69 65 6C  64 73 92 84  6E 61 6D 65 83 61 67 65 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleSuccessMessage(It.IsAny<IDictionary<string, object>>()))
                    .Callback<IDictionary<string, object>>(x => dict = x);
                var reader = new BoltReader(mockInput.Object);

                reader.Read(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleSuccessMessage(It.IsAny<IDictionary<string, object>>()), Times.Once);
                dict.Should().NotBeNull();
                dict.Should().HaveCount(1);
                dict.Should().ContainKey("fields");

                var fieldsValue = dict["fields"];
                fieldsValue.Should().NotBeNull();
                fieldsValue.Should().BeAssignableTo<IList<object>>();

                var fields = (IList<object>) fieldsValue;
                fields.Should().HaveCount(2);
                fields.Should().Contain("name");
                fields.Should().Contain("age");
            }

            [Fact]
            public void ShouldReadFailureMessage()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("00 47 B1 7F A2 84  63 6F 64 65  D0 25 4E 65  6F 2E 43 6C 69 65 6E 74  45 72 72 6F  72 2E 53 74  61 74 65 6D 65 6E 74 2E  53 79 6E 74  61 78 45 72  72 6F 72 87 6D 65 73 73  61 67 65 8F  49 6E 76 61  6C 69 64 20 73 79 6E 74  61 78 2E 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var reader = new BoltReader(mockInput.Object);

                reader.Read(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleFailureMessage("Neo.ClientError.Statement.SyntaxError", "Invalid syntax."), Times.Once);
            }

            [Fact]
            public void ShouldReadIgnoredMessage()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("00 02 B0 7E 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var reader = new BoltReader(mockInput.Object);

                reader.Read(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleIgnoredMessage(), Times.Once);
            }

            [Fact]
            public void ShouldReadRecordMessageReturningNumber()
            {
                var fields = new object[0];
                var mockInput =
                    IOExtensions.CreateMockStream("00 04 b1 71  91 01 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleRecordMessage(It.IsAny<object[]>()))
                    .Callback<object[]>(x => fields = x);
                var reader = new BoltReader(mockInput.Object);

                reader.Read(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleRecordMessage(It.IsAny<object[]>()), Times.Once);

                fields.Should().NotBeNull();
                fields.Should().HaveCount(1);
                fields.Should().Contain(1L);
            }

            [Fact]
            public void ShouldReadRecordMessageReturningRelationship()
            {
                var fields = new object[0];
                var mockInput =
                    IOExtensions.CreateMockStream("00 0A b1 71 91 B5 52 01 02 03 80 a0 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleRecordMessage(It.IsAny<object[]>()))
                    .Callback<object[]>(x => fields = x);
                var reader = new BoltReader(mockInput.Object);

                reader.Read(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleRecordMessage(It.IsAny<object[]>()), Times.Once);

                fields.Should().NotBeNull();
                fields.Should().HaveCount(1);

                var rel = fields[0] as IRelationship;
                rel.Should().NotBeNull();

                rel.Id.Should().Be(1);
                rel.StartNodeId.Should().Be(2);
                rel.EndNodeId.Should().Be(3);
                rel.Type.Should().BeEmpty();
                rel.Properties.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReadRecordMessageReturningNode()
            {
                var fields = new object[0];
                var mockInput =
                    IOExtensions.CreateMockStream("00 08 b1 71 91 B3 4E 01 90 A0 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleRecordMessage(It.IsAny<object[]>()))
                    .Callback<object[]>(x => fields = x);
                var reader = new BoltReader(mockInput.Object);

                reader.Read(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleRecordMessage(It.IsAny<object[]>()), Times.Once);

                fields.Should().NotBeNull();
                fields.Should().HaveCount(1);

                var n = fields[0] as INode;
                n.Should().NotBeNull();

                n.Id.Should().Be(1);
                n.Properties.Should().BeEmpty();
                n.Labels.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReadRecordMessageReturningPath()
            {
                var fields = new object[0];
                var mockInput =
                    IOExtensions.CreateMockStream("00 0d b1 71 91 B3 50 91 B3 4E 01 90 A0 90 90 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleRecordMessage(It.IsAny<object[]>()))
                    .Callback<object[]>(x => fields = x);
                var reader = new BoltReader(mockInput.Object);

                reader.Read(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleRecordMessage(It.IsAny<object[]>()), Times.Once);

                fields.Should().NotBeNull();
                fields.Should().HaveCount(1);

                var p = fields[0] as IPath;
                p.Should().NotBeNull();

                p.Start.Should().NotBeNull();
                p.End.Should().NotBeNull();
                p.Start.Id.Should().Be(1);
                p.Start.Properties.Should().BeEmpty();
                p.Start.Labels.Should().BeEmpty();
                p.Nodes.Should().HaveCount(1);
                p.Relationships.Should().HaveCount(0);
            }


            //00 79 B1 71 92 B3 4E 02 91 86 50 65 72 73 6F 6E A1 84 6E 61 6D 65 81 63 92 A2 83 72 65 6C B5 52 03 02 05 84 4F 57 4E 53 A0 87 70 72 6F 64 75 63 74 B3 4E 05 91 87 50 72 6F 64 75 63 74 A1 84 6E 61 6D 65 84 69 70 6F 64 A2 83 72 65 6C B5 52 02 02 03 84 4F 57 4E 53 A0 87 70 72 6F 64 75 63 74 B3 4E 03 91 87 50 72 6F 64 75 63 74 A1 84 6E 61 6D 65 88 63 6F 6D 70 75 74 65 72
            //[RECORD [Neo4j.Driver.Internal.Node, [[{rel : Neo4j.Driver.Internal.Relationship}, {product : Neo4j.Driver.Internal.Node}], [{rel : Neo4j.Driver.Internal.Relationship}, {product : Neo4j.Driver.Internal.Node}]]]]
            [Fact]
            public void ShouldReadRecordMessageWithInnerStructs()
            {
                var fields = new object[0];
                var mockInput =
                    IOExtensions.CreateMockStream(
                        "00 79 B1 71 92 B3 4E 02 91 86 50 65 72 73 6F 6E A1 84 6E 61 6D 65 81 63 92 A2 83 72 65 6C B5 52 03 02 05 84 4F 57 4E 53 A0 87 70 72 6F 64 75 63 74 B3 4E 05 91 87 50 72 6F 64 75 63 74 A1 84 6E 61 6D 65 84 69 70 6F 64 A2 83 72 65 6C B5 52 02 02 03 84 4F 57 4E 53 A0 87 70 72 6F 64 75 63 74 B3 4E 03 91 87 50 72 6F 64 75 63 74 A1 84 6E 61 6D 65 88 63 6F 6D 70 75 74 65 72 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleRecordMessage(It.IsAny<object[]>()))
                    .Callback<object[]>(x => fields = x);
                var reader = new BoltReader(mockInput.Object);

                reader.Read(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleRecordMessage(It.IsAny<object[]>()), Times.Once);

                fields.Should().NotBeNull();
                fields.Should().HaveCount(2);

                fields[0].Should().BeAssignableTo<INode>();
                fields[0].ValueAs<INode>().Labels.Should().Contain("Person");
                fields[0].ValueAs<INode>().Properties.Keys.Should().Contain("name");
                fields[0].ValueAs<INode>().Properties.Values.Should().Contain("c");

                fields[1].Should().BeAssignableTo<IList>();

                var el0 = ((IList) fields[1])[0];
                el0.Should().BeAssignableTo<IDictionary<string, object>>();
                el0.ValueAs<IDictionary<string, object>>().Should().ContainKey("rel");
                var rel0 = el0.ValueAs<IDictionary<string, object>>()["rel"];
                rel0.Should().BeAssignableTo<IRelationship>();
                rel0.ValueAs<IRelationship>().Type.Should().Be("OWNS");
                rel0.ValueAs<IRelationship>().StartNodeId.Should().Be(2L);
                rel0.ValueAs<IRelationship>().EndNodeId.Should().Be(5L);
                rel0.ValueAs<IRelationship>().Properties.Should().BeEmpty();

                el0.ValueAs<IDictionary<string, object>>().Should().ContainKey("product");
                var prod0 = el0.ValueAs<IDictionary<string, object>>()["product"];
                prod0.Should().BeAssignableTo<INode>();
                prod0.ValueAs<INode>().Labels.Should().Contain("Product");
                prod0.ValueAs<INode>().Properties.Keys.Should().Contain("name");
                prod0.ValueAs<INode>().Properties.Values.Should().Contain("ipod");

                var el1 = ((IList)fields[1])[1];
                el1.Should().BeAssignableTo<IDictionary<string, object>>();
                el1.ValueAs<IDictionary<string, object>>().Should().ContainKey("rel");
                var rel1 = el1.ValueAs<IDictionary<string, object>>()["rel"];
                rel1.Should().BeAssignableTo<IRelationship>();
                rel1.ValueAs<IRelationship>().Type.Should().Be("OWNS");
                rel1.ValueAs<IRelationship>().StartNodeId.Should().Be(2L);
                rel1.ValueAs<IRelationship>().EndNodeId.Should().Be(3L);
                rel1.ValueAs<IRelationship>().Properties.Should().BeEmpty();

                el1.ValueAs<IDictionary<string, object>>().Should().ContainKey("product");
                var prod1 = el1.ValueAs<IDictionary<string, object>>()["product"];
                prod1.Should().BeAssignableTo<INode>();
                prod1.ValueAs<INode>().Labels.Should().Contain("Product");
                prod1.ValueAs<INode>().Properties.Keys.Should().Contain("name");
                prod1.ValueAs<INode>().Properties.Values.Should().Contain("computer");
            }


            [Fact]
            public void ShouldReadRecordMessageReturningNumberNodeRelationShipPath()
            {
                var fields = new object[0];
                var mockInput =
                    IOExtensions.CreateMockStream("00 1a b1 71 94 01 B3 4E 01 90 A0 B5 52 01 02 03 80 a0 B3 50 91 B3 4E 01 90 A0 90 90 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleRecordMessage(It.IsAny<object[]>()))
                    .Callback<object[]>(x => fields = x);
                var reader = new BoltReader(mockInput.Object);

                reader.Read(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleRecordMessage(It.IsAny<object[]>()), Times.Once);

                fields.Should().NotBeNull();
                fields.Should().HaveCount(4);

                var first = fields[0];
                first.Should().NotBeNull();
                first.Should().BeOfType<long>();
                first.Should().Be(1L);

                var second = fields[1] as INode;
                second.Should().NotBeNull();
                second.Id.Should().Be(1);
                second.Properties.Should().BeEmpty();
                second.Labels.Should().BeEmpty();

                var third = fields[2] as IRelationship;
                third.Should().NotBeNull();
                third.Id.Should().Be(1);
                third.StartNodeId.Should().Be(2);
                third.EndNodeId.Should().Be(3);
                third.Type.Should().BeEmpty();
                third.Properties.Should().BeEmpty();

                var fourth = fields[3] as IPath;
                fourth.Should().NotBeNull();
                fourth.Start.Should().NotBeNull();
                fourth.End.Should().NotBeNull();
                fourth.Start.Id.Should().Be(1);
                fourth.Start.Properties.Should().BeEmpty();
                fourth.Start.Labels.Should().BeEmpty();
                fourth.Nodes.Should().HaveCount(1);
                fourth.Relationships.Should().HaveCount(0);
            }

            [Fact]
            public void ShouldThrowExceptionWhenMessageHasInvalidSignature()
            {
                var fields = new object[0];
                var mockInput =
                    IOExtensions.CreateMockStream("00 04 b1 95 91 01 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleRecordMessage(It.IsAny<object[]>()))
                    .Callback<object[]>(x => fields = x);
                var reader = new BoltReader(mockInput.Object);

                var ex = Record.Exception(() => reader.Read(mockResponseHandler.Object));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<ProtocolException>();
            }

        }

        public class ReadAsyncMethod
        {

            [Fact]
            public async void ShouldReadSuccessMessage()
            {
                var dict = (IDictionary<string, object>)null;
                var mockInput =
                    IOExtensions.CreateMockStream("00 14 B1 70 A1 86  66 69 65 6C  64 73 92 84  6E 61 6D 65 83 61 67 65 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleSuccessMessage(It.IsAny<IDictionary<string, object>>()))
                    .Callback<IDictionary<string, object>>(x => dict = x);
                var reader = new BoltReader(mockInput.Object);

                await reader.ReadAsync(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleSuccessMessage(It.IsAny<IDictionary<string, object>>()), Times.Once);
                dict.Should().NotBeNull();
                dict.Should().HaveCount(1);
                dict.Should().ContainKey("fields");

                var fieldsValue = dict["fields"];
                fieldsValue.Should().NotBeNull();
                fieldsValue.Should().BeAssignableTo<IList<object>>();

                var fields = (IList<object>)fieldsValue;
                fields.Should().HaveCount(2);
                fields.Should().Contain("name");
                fields.Should().Contain("age");
            }

            [Fact]
            public async void ShouldReadFailureMessage()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("00 47 B1 7F A2 84  63 6F 64 65  D0 25 4E 65  6F 2E 43 6C 69 65 6E 74  45 72 72 6F  72 2E 53 74  61 74 65 6D 65 6E 74 2E  53 79 6E 74  61 78 45 72  72 6F 72 87 6D 65 73 73  61 67 65 8F  49 6E 76 61  6C 69 64 20 73 79 6E 74  61 78 2E 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var reader = new BoltReader(mockInput.Object);

                await reader.ReadAsync(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleFailureMessage("Neo.ClientError.Statement.SyntaxError", "Invalid syntax."), Times.Once);
            }

            [Fact]
            public async void ShouldReadIgnoredMessage()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("00 02 B0 7E 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var reader = new BoltReader(mockInput.Object);

                await reader.ReadAsync(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleIgnoredMessage(), Times.Once);
            }

            [Fact]
            public async void ShouldReadRecordMessageReturningNumber()
            {
                var fields = new object[0];
                var mockInput =
                    IOExtensions.CreateMockStream("00 04 b1 71  91 01 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleRecordMessage(It.IsAny<object[]>()))
                    .Callback<object[]>(x => fields = x);
                var reader = new BoltReader(mockInput.Object);

                await reader.ReadAsync(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleRecordMessage(It.IsAny<object[]>()), Times.Once);

                fields.Should().NotBeNull();
                fields.Should().HaveCount(1);
                fields.Should().Contain(1L);
            }

            [Fact]
            public async void ShouldReadRecordMessageReturningRelationship()
            {
                var fields = new object[0];
                var mockInput =
                    IOExtensions.CreateMockStream("00 0A b1 71 91 B5 52 01 02 03 80 a0 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleRecordMessage(It.IsAny<object[]>()))
                    .Callback<object[]>(x => fields = x);
                var reader = new BoltReader(mockInput.Object);

                await reader.ReadAsync(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleRecordMessage(It.IsAny<object[]>()), Times.Once);

                fields.Should().NotBeNull();
                fields.Should().HaveCount(1);

                var rel = fields[0] as IRelationship;
                rel.Should().NotBeNull();

                rel.Id.Should().Be(1);
                rel.StartNodeId.Should().Be(2);
                rel.EndNodeId.Should().Be(3);
                rel.Type.Should().BeEmpty();
                rel.Properties.Should().BeEmpty();
            }

            [Fact]
            public async void ShouldReadRecordMessageReturningNode()
            {
                var fields = new object[0];
                var mockInput =
                    IOExtensions.CreateMockStream("00 08 b1 71 91 B3 4E 01 90 A0 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleRecordMessage(It.IsAny<object[]>()))
                    .Callback<object[]>(x => fields = x);
                var reader = new BoltReader(mockInput.Object);

                await reader.ReadAsync(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleRecordMessage(It.IsAny<object[]>()), Times.Once);

                fields.Should().NotBeNull();
                fields.Should().HaveCount(1);

                var n = fields[0] as INode;
                n.Should().NotBeNull();

                n.Id.Should().Be(1);
                n.Properties.Should().BeEmpty();
                n.Labels.Should().BeEmpty();
            }

            [Fact]
            public async void ShouldReadRecordMessageReturningPath()
            {
                var fields = new object[0];
                var mockInput =
                    IOExtensions.CreateMockStream("00 0d b1 71 91 B3 50 91 B3 4E 01 90 A0 90 90 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleRecordMessage(It.IsAny<object[]>()))
                    .Callback<object[]>(x => fields = x);
                var reader = new BoltReader(mockInput.Object);

                await reader.ReadAsync(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleRecordMessage(It.IsAny<object[]>()), Times.Once);

                fields.Should().NotBeNull();
                fields.Should().HaveCount(1);

                var p = fields[0] as IPath;
                p.Should().NotBeNull();

                p.Start.Should().NotBeNull();
                p.End.Should().NotBeNull();
                p.Start.Id.Should().Be(1);
                p.Start.Properties.Should().BeEmpty();
                p.Start.Labels.Should().BeEmpty();
                p.Nodes.Should().HaveCount(1);
                p.Relationships.Should().HaveCount(0);
            }


            [Fact]
            public async void ShouldReadRecordMessageReturningNumberNodeRelationShipPath()
            {
                var fields = new object[0];
                var mockInput =
                    IOExtensions.CreateMockStream("00 1a b1 71 94 01 B3 4E 01 90 A0 B5 52 01 02 03 80 a0 B3 50 91 B3 4E 01 90 A0 90 90 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleRecordMessage(It.IsAny<object[]>()))
                    .Callback<object[]>(x => fields = x);
                var reader = new BoltReader(mockInput.Object);

                await reader.ReadAsync(mockResponseHandler.Object);

                mockResponseHandler.Verify(x => x.HandleRecordMessage(It.IsAny<object[]>()), Times.Once);

                fields.Should().NotBeNull();
                fields.Should().HaveCount(4);

                var first = fields[0];
                first.Should().NotBeNull();
                first.Should().BeOfType<long>();
                first.Should().Be(1L);

                var second = fields[1] as INode;
                second.Should().NotBeNull();
                second.Id.Should().Be(1);
                second.Properties.Should().BeEmpty();
                second.Labels.Should().BeEmpty();

                var third = fields[2] as IRelationship;
                third.Should().NotBeNull();
                third.Id.Should().Be(1);
                third.StartNodeId.Should().Be(2);
                third.EndNodeId.Should().Be(3);
                third.Type.Should().BeEmpty();
                third.Properties.Should().BeEmpty();

                var fourth = fields[3] as IPath;
                fourth.Should().NotBeNull();
                fourth.Start.Should().NotBeNull();
                fourth.End.Should().NotBeNull();
                fourth.Start.Id.Should().Be(1);
                fourth.Start.Properties.Should().BeEmpty();
                fourth.Start.Labels.Should().BeEmpty();
                fourth.Nodes.Should().HaveCount(1);
                fourth.Relationships.Should().HaveCount(0);
            }

            [Fact]
            public async void ShouldThrowExceptionWhenMessageHasInvalidSignature()
            {
                var fields = new object[0];
                var mockInput =
                    IOExtensions.CreateMockStream("00 04 b1 95 91 01 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleRecordMessage(It.IsAny<object[]>()))
                    .Callback<object[]>(x => fields = x);
                var reader = new BoltReader(mockInput.Object);

                var ex = await Record.ExceptionAsync(() => reader.ReadAsync(mockResponseHandler.Object));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<ProtocolException>();
            }

        }

        public class BufferCleanUp
        {

            [Fact]
            public void ShouldNotResetCapacityWhenCapacityDoesNotExceedMaxBufferSize()
            {
                var dict = (IDictionary<string, object>)null;
                var mockInput =
                    IOExtensions.CreateMockStream("00 14 B1 70 A1 86  66 69 65 6C  64 73 92 84  6E 61 6D 65 83 61 67 65 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleSuccessMessage(It.IsAny<IDictionary<string, object>>()))
                    .Callback<IDictionary<string, object>>(x => dict = x);
                var mockLogger = new Mock<ILogger>();
                var reader = new BoltReader(mockInput.Object, 256, 512, mockLogger.Object, true);

                reader.Read(mockResponseHandler.Object);

                mockLogger.Verify(x => x.Info(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
            }

            [Fact]
            public void ShouldResetCapacityWhenCapacityExceedsMaxBufferSize()
            {
                var dict = (IDictionary<string, object>)null;
                var mockInput =
                    IOExtensions.CreateMockStream("00 14 B1 70 A1 86  66 69 65 6C  64 73 92 84  6E 61 6D 65 83 61 67 65 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleSuccessMessage(It.IsAny<IDictionary<string, object>>()))
                    .Callback<IDictionary<string, object>>(x => dict = x);
                var mockLogger = new Mock<ILogger>();
                var reader = new BoltReader(mockInput.Object, 10, 15, mockLogger.Object, true);

                reader.Read(mockResponseHandler.Object);

                mockLogger.Verify(x => x.Info(It.Is<string>(s => s.StartsWith("Shrinking read buffers to the default read buffer size"))), Times.Once);
            }

            [Fact]
            public async void ShouldResetCapacityWhenCapacityExceedsMaxBufferSizeAsync()
            {
                var dict = (IDictionary<string, object>)null;
                var mockInput =
                    IOExtensions.CreateMockStream("00 14 B1 70 A1 86  66 69 65 6C  64 73 92 84  6E 61 6D 65 83 61 67 65 00 00");
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.HandleSuccessMessage(It.IsAny<IDictionary<string, object>>()))
                    .Callback<IDictionary<string, object>>(x => dict = x);
                var mockLogger = new Mock<ILogger>();
                var reader = new BoltReader(mockInput.Object, 10, 15, mockLogger.Object, true);

                await reader.ReadAsync(mockResponseHandler.Object);

                mockLogger.Verify(x => x.Info(It.Is<string>(s => s.StartsWith("Shrinking read buffers to the default read buffer size"))), Times.Once);
            }

        }

        public class UnpackStructureMethod
        {
            private static object ReadFromByteArrayChunked(byte[] bytes)
            {
                var reader = IOExtensions.CreateChunkedPackStreamReaderFromBytes(bytes);
                var result = reader.Read();

                return result;
            }

            [Fact]
            public void ShouldThrowExceptionWhenStructSignatureNotRecognized()
            {
                var bytes = "00 07 B5 43 01 02 03 80 a0 00 00".ToByteArray();

                var ex = Record.Exception(() => ReadFromByteArrayChunked(bytes));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<ProtocolException>();
            }

            [Fact]
            public void ShouldUnpackRelationshipCorrectly()
            {
                var bytes = "00 07 B5 52 01 02 03 80 a0 00 00".ToByteArray();

                var rel = ReadFromByteArrayChunked(bytes) as IRelationship;
                rel.Should().NotBeNull();

                rel.Id.Should().Be(1);
                rel.StartNodeId.Should().Be(2);
                rel.EndNodeId.Should().Be(3);
                rel.Type.Should().BeEmpty();
                rel.Properties.Should().BeEmpty();
            }

            [Fact]
            public void ShouldUnpackNodeCorrectly()
            {
                var bytes = "00 06 B3 4E 01 90 A0 00 00 00".ToByteArray();

                var n = ReadFromByteArrayChunked(bytes) as INode;
                n.Should().NotBeNull();

                n.Id.Should().Be(1);
                n.Properties.Should().BeEmpty();
                n.Labels.Should().BeEmpty();
            }

            [Fact]
            public void ShouldUnpackPathCorrectly()
            {
                var bytes = "00 0A B3 50 91 B3 4E 01 90 A0 90 90 00 00".ToByteArray();

                var p = ReadFromByteArrayChunked(bytes) as IPath;
                p.Should().NotBeNull();

                p.Start.Should().NotBeNull();
                p.End.Should().NotBeNull();
                p.Start.Id.Should().Be(1);
                p.Start.Properties.Should().BeEmpty();
                p.Start.Labels.Should().BeEmpty();
                p.Nodes.Should().HaveCount(1);
                p.Relationships.Should().HaveCount(0);
            }

            [Fact]
            public void ShouldUnpackZeroLenghPathCorrectly()
            {
                // A
                var bytes =
                    "00 2C B3 50 91 B3 4E C9 03 E9    92 86 50 65 72 73 6F 6E    88 45 6D 70 6C 6F 79 65    65 A2 84 6E 61 6D 65 85 41 6C 69 63 65 83 61 67    65 21 90 90 00 00"
                        .ToByteArray();

                var p = ReadFromByteArrayChunked(bytes) as IPath;
                p.Should().NotBeNull();

                p.Start.Should().NotBeNull();
                p.End.Should().NotBeNull();
                p.Start.Equals(TestNodes.Alice).Should().BeTrue();
                p.End.Equals(TestNodes.Alice).Should().BeTrue();
                p.Nodes.Should().HaveCount(1);
                p.Relationships.Should().HaveCount(0);
            }

            [Fact]
            public void ShouldUnpackPathWithLenghOneCorrectly()
            {
                // A->B
                var bytes =
                    "00 66 B3 50 92 B3 4E C9 03 E9    92 86 50 65 72 73 6F 6E    88 45 6D 70 6C 6F 79 65    65 A2 84 6E 61 6D 65 85 41 6C 69 63 65 83 61 67    65 21 B3 4E C9 03 EA 92    86 50 65 72 73 6F 6E 88    45 6D 70 6C 6F 79 65 65 A2 84 6E 61 6D 65 83 42    6F 62 83 61 67 65 2C 91    B3 72 0C 85 4B 4E 4F 57    53 A1 85 73 69 6E 63 65 C9 07 CF 92 01 01 00 00"
                        .ToByteArray();

                var p = ReadFromByteArrayChunked(bytes) as IPath;
                p.Should().NotBeNull();

                p.Nodes.Should().HaveCount(2);
                p.Relationships.Should().HaveCount(1);
                p.Start.Should().NotBeNull();
                p.End.Should().NotBeNull();
                p.Start.Equals(TestNodes.Alice).Should().BeTrue();
                p.End.Equals(TestNodes.Bob).Should().BeTrue();
                p.Relationships[0].Equals(TestRelationships.AliceKnowsBob).Should().BeTrue();
            }

            [Fact]
            public void ShouldUnpackPathWithRelationshipTraversedAgainstItsDirectionCorrectly()
            {
                // A->B<-C->D
                var bytes =
                    "00 b0 B35094B34EC903E99286506572736F6E88456D706C6F796565A2846E616D6585416C6963658361676521B34EC903EA9286506572736F6E88456D706C6F796565A2846E616D6583426F62836167652CB34EC903EB9186506572736F6EA1846E616D65854361726F6CB34EC903EC90A1846E616D65844461766593B3720C854B4E4F5753A18573696E6365C907CFB37220884449534C494B4553A0B372228A4D4152524945445F544FA0960101FE020303 00 00"
                        .ToByteArray();

                var p = ReadFromByteArrayChunked(bytes) as IPath;
                p.Should().NotBeNull();

                p.Nodes.Should().HaveCount(4);
                p.Relationships.Should().HaveCount(3);
                p.Start.Should().NotBeNull();
                p.End.Should().NotBeNull();
                p.Start.Equals(TestNodes.Alice).Should().BeTrue();
                p.End.Equals(TestNodes.Dave).Should().BeTrue($"Got {p.End.Id}");

                List<INode> correctOrder = new List<INode>
                {
                    TestNodes.Alice,
                    TestNodes.Bob,
                    TestNodes.Carol,
                    TestNodes.Dave
                };
                p.Nodes.Should().ContainInOrder(correctOrder);

                p.Relationships[0].Equals(TestRelationships.AliceKnowsBob).Should().BeTrue();
                List<IRelationship> expectedRelOrder = new List<IRelationship>
                {
                    TestRelationships.AliceKnowsBob,
                    TestRelationships.CarolDislikesBob,
                    TestRelationships.CarolMarriedToDave
                };
                p.Relationships.Should().ContainInOrder(expectedRelOrder);
            }

            [Fact]
            public void ShouldUnpackPathWithNodeVisitedMulTimesCorrectly()
            {
                // A->B<-A->C->B<-C
                var bytes =
                    "00 9E B35093B34EC903E99286506572736F6E88456D706C6F796565A2846E616D6585416C6963658361676521B34EC903EA9286506572736F6E88456D706C6F796565A2846E616D6583426F62836167652CB34EC903EB9186506572736F6EA1846E616D65854361726F6C93B3720C854B4E4F5753A18573696E6365C907CFB3720D854C494B4553A0B37220884449534C494B4553A09A0101FF0002020301FD02 00 00"
                        .ToByteArray();

                var p = ReadFromByteArrayChunked(bytes) as IPath;
                p.Should().NotBeNull();

                p.Nodes.Should().HaveCount(6);
                p.Relationships.Should().HaveCount(5);
                p.Start.Should().NotBeNull();
                p.End.Should().NotBeNull();
                p.Start.Equals(TestNodes.Alice).Should().BeTrue();
                p.End.Equals(TestNodes.Carol).Should().BeTrue($"Got {p.End.Id}");

                List<INode> correctOrder = new List<INode>
                {
                    TestNodes.Alice,
                    TestNodes.Bob,
                    TestNodes.Alice,
                    TestNodes.Carol,
                    TestNodes.Bob,
                    TestNodes.Carol
                };
                p.Nodes.Should().ContainInOrder(correctOrder);

                List<IRelationship> expectedRelOrder = new List<IRelationship>
                {
                    TestRelationships.AliceKnowsBob,
                    TestRelationships.AliceKnowsBob,
                    TestRelationships.AliceLikesCarol,
                    TestRelationships.CarolDislikesBob,
                    TestRelationships.CarolDislikesBob
                };
                p.Relationships.Should().ContainInOrder(expectedRelOrder);
                p.Relationships[0].Equals(TestRelationships.AliceKnowsBob).Should().BeTrue();
            }

            [Fact]
            public void ShouldUnpackPathWithRelTraversedMulTimesInSameDirectionCorrectly()
            {
                // A->C->B<-A->C->D
                var bytes =
                    "00 BE B35094B34EC903E99286506572736F6E88456D706C6F796565A2846E616D6585416C6963658361676521B34EC903EB9186506572736F6EA1846E616D65854361726F6CB34EC903EA9286506572736F6E88456D706C6F796565A2846E616D6583426F62836167652CB34EC903EC90A1846E616D65844461766594B3720D854C494B4553A0B37220884449534C494B4553A0B3720C854B4E4F5753A18573696E6365C907CFB372228A4D4152524945445F544FA09A01010202FD0001010403 00 00"
                        .ToByteArray();

                var p = ReadFromByteArrayChunked(bytes) as IPath;
                p.Should().NotBeNull();

                p.Nodes.Should().HaveCount(6);
                p.Relationships.Should().HaveCount(5);
                p.Start.Should().NotBeNull();
                p.End.Should().NotBeNull();
                p.Start.Equals(TestNodes.Alice).Should().BeTrue();
                p.End.Equals(TestNodes.Dave).Should().BeTrue($"Got {p.End.Id}");

                List<INode> correctOrder = new List<INode>
                {
                    TestNodes.Alice,
                    TestNodes.Carol,
                    TestNodes.Bob,
                    TestNodes.Alice,
                    TestNodes.Carol,
                    TestNodes.Dave
                };
                p.Nodes.Should().ContainInOrder(correctOrder);

                List<IRelationship> expectedRelOrder = new List<IRelationship>
                {
                    TestRelationships.AliceLikesCarol,
                    TestRelationships.CarolDislikesBob,
                    TestRelationships.AliceKnowsBob,
                    TestRelationships.AliceLikesCarol,
                    TestRelationships.CarolMarriedToDave
                };
                p.Relationships.Should().ContainInOrder(expectedRelOrder);
                p.Relationships[0].Equals(TestRelationships.AliceLikesCarol).Should().BeTrue();
            }

            [Fact]
            public void ShouldUnpackPathWithLoopCorrectly()
            {
                // C->D->D
                var bytes =
                    "00 50 B35092B34EC903EB9186506572736F6EA1846E616D65854361726F6CB34EC903EC90A1846E616D65844461766592B372228A4D4152524945445F544FA0B3722C89574F524B535F464F52A09401010201 00 00"
                        .ToByteArray();

                var p = ReadFromByteArrayChunked(bytes) as IPath;
                p.Should().NotBeNull();

                p.Nodes.Should().HaveCount(3);
                p.Relationships.Should().HaveCount(2);
                p.Start.Should().NotBeNull();
                p.End.Should().NotBeNull();
                p.Start.Equals(TestNodes.Carol).Should().BeTrue();
                p.End.Equals(TestNodes.Dave).Should().BeTrue($"Got {p.End.Id}");

                List<INode> correctOrder = new List<INode>
                {
                    TestNodes.Carol,
                    TestNodes.Dave,
                    TestNodes.Dave
                };
                p.Nodes.Should().ContainInOrder(correctOrder);

                List<IRelationship> expectedRelOrder = new List<IRelationship>
                {
                    TestRelationships.CarolMarriedToDave,
                    TestRelationships.DaveWorksForDave,
                };
                p.Relationships.Should().ContainInOrder(expectedRelOrder);
                p.Relationships[0].Equals(TestRelationships.CarolMarriedToDave).Should().BeTrue();
            }

            private static class TestNodes
            {
                public static INode Alice = new Node(1001L,
                    new List<string> { "Person", "Employee" },
                    new Dictionary<string, object> { { "name", "Alice" }, { "age", 33L } });

                public static INode Bob = new Node(1002L,
                    new List<string> { "Person", "Employee" },
                    new Dictionary<string, object> { { "name", "Bob" }, { "age", 44L } });

                public static INode Carol = new Node(
                    1003L,
                    new List<string> { "Person" },
                    new Dictionary<string, object> { { "name", "Carol" } });

                public static INode Dave = new Node(
                    1004L,
                    new List<string>(),
                    new Dictionary<string, object> { { "name", "Dave" } });
            }

            private static class TestRelationships
            {
                // IRelationship types
                private static string KNOWS = "KNOWS";

                private static string LIKES = "LIKES";
                private static string DISLIKES = "DISLIKES";

                private static string MARRIED_TO =
                    "MARRIED_TO";

                private static string WORKS_FOR =
                    "WORKS_FOR";

                // IRelationships
                public static IRelationship AliceKnowsBob =
                    new Relationship(12L, TestNodes.Alice.Id,
                        TestNodes.Bob.Id, KNOWS,
                        new Dictionary<string, object> { { "since", 1999L } });

                public static IRelationship AliceLikesCarol =
                    new Relationship(13L, TestNodes.Alice.Id,
                        TestNodes.Carol.Id, LIKES,
                        new Dictionary<string, object>());

                public static IRelationship CarolDislikesBob =
                    new Relationship(32L, TestNodes.Carol.Id,
                        TestNodes.Bob.Id, DISLIKES,
                        new Dictionary<string, object>());

                public static IRelationship CarolMarriedToDave =
                    new Relationship(34L, TestNodes.Carol.Id,
                        TestNodes.Dave.Id, MARRIED_TO,
                        new Dictionary<string, object>());

                public static IRelationship DaveWorksForDave =
                    new Relationship(44L, TestNodes.Dave.Id,
                        TestNodes.Dave.Id, WORKS_FOR,
                        new Dictionary<string, object>());
            }
        }

    }
}

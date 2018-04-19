// Copyright (c) 2002-2018 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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

using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Primitives;
using Moq;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.StructHandlers;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO.StructHandlers
{
    public class PointHandlerTests : StructHandlerTests
    {
        internal override IPackStreamStructHandler HandlerUnderTest => new PointHandler();

        [Fact]
        public void ShouldWritePoint2D()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(new Point(7203, 51.5044585, -0.105658));

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(3);
            reader.ReadStructSignature().Should().Be((byte) 'X');
            reader.Read().Should().Be(7203L);
            reader.Read().Should().Be(51.5044585);
            reader.Read().Should().Be(-0.105658);
        }

        [Fact]
        public void ShouldWritePoint3D()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(new Point(7203, 51.5044585, -0.105658, 35.25));

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(4);
            reader.ReadStructSignature().Should().Be((byte) 'Y');
            reader.Read().Should().Be(7203L);
            reader.Read().Should().Be(51.5044585);
            reader.Read().Should().Be(-0.105658);
            reader.Read().Should().Be(35.25);
        }

        [Fact]
        public void ShouldReadPoint2D()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(PointHandler.Point2DStructSize, PointHandler.Point2DStructType);
            writer.Write(7203);
            writer.Write(51.5044585);
            writer.Write(-0.105658);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            var value = reader.Read();

            value.Should().NotBeNull();
            value.Should().BeOfType<Point>().Which.SrId.Should().Be(7203);
            value.Should().BeOfType<Point>().Which.X.Should().Be(51.5044585);
            value.Should().BeOfType<Point>().Which.Y.Should().Be(-0.105658);
            value.Should().BeOfType<Point>().Which.Z.Should().Be(double.NaN);
        }

        [Fact]
        public void ShouldReadPoint3D()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(PointHandler.Point3DStructSize, PointHandler.Point3DStructType);
            writer.Write(7203);
            writer.Write(51.5044585);
            writer.Write(-0.105658);
            writer.Write(35.25);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            var value = reader.Read();

            value.Should().NotBeNull();
            value.Should().BeOfType<Point>().Which.SrId.Should().Be(7203);
            value.Should().BeOfType<Point>().Which.X.Should().Be(51.5044585);
            value.Should().BeOfType<Point>().Which.Y.Should().Be(-0.105658);
            value.Should().BeOfType<Point>().Which.Z.Should().Be(35.25);
        }
    }
}
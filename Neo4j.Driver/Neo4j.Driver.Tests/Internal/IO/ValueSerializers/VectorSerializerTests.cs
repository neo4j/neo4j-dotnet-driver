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

using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.ValueSerializers.VectorSerializers;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.IO.ValueSerializers;

public class VectorSerializerTests : PackStreamSerializerTests
{
    internal override IPackStreamSerializer SerializerUnderTest => new VectorSerializer();

    [Fact]
    public void ShouldSerializeFloat32Vector()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        var float32Vector = new[] { 0.1f, 0.2f, 0.3f };
        var vector = Vector.Create(float32Vector);

        writer.Write(vector);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStreamType.Struct);
        reader.ReadStructHeader().Should().Be(2); // Size of the struct
        reader.ReadStructSignature().Should().Be((byte)'V'); // Vector struct type
        reader.ReadBytes().Should().BeEquivalentTo([PackStream.Float32]);
        reader.ReadBytes().Should().BeEquivalentTo(float32Vector.SelectMany(PackStreamBitConverter.GetBytes).ToArray());
    }

    [Fact]
    public void ShouldSerializeFloat64Vector()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        var float64Vector = new[] { 0.1, 0.2 };
        var vector = Vector.Create(float64Vector);

        writer.Write(vector);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStreamType.Struct);
        reader.ReadStructHeader().Should().Be(2); // Size of the struct
        reader.ReadStructSignature().Should().Be((byte)'V'); // Vector struct type
        reader.ReadBytes().Should().BeEquivalentTo([PackStream.Float64]);
        reader.ReadBytes().Should().BeEquivalentTo(float64Vector.SelectMany(PackStreamBitConverter.GetBytes).ToArray());
    }

    [Fact]
    public void ShouldSerializeByteVector()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        var byteVector = new sbyte[] { 1, 2, 3 };
        var vector = Vector.Create(byteVector);

        writer.Write(vector);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStreamType.Struct);
        reader.ReadStructHeader().Should().Be(2); // Size of the struct
        reader.ReadStructSignature().Should().Be((byte)'V'); // Vector struct type
        reader.ReadBytes().Should().BeEquivalentTo([PackStream.Int8]);
        reader.Read().Should().BeEquivalentTo(byteVector);
    }

    [Fact]
    public void ShouldSerializeInt16Vector()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        var int16Vector = new short[] { 100, 200, 300 };
        var vector = Vector.Create(int16Vector);

        writer.Write(vector);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStreamType.Struct);
        reader.ReadStructHeader().Should().Be(2); // Size of the struct
        reader.ReadStructSignature().Should().Be((byte)'V'); // Vector struct type
        reader.ReadBytes().Should().BeEquivalentTo([PackStream.Int16]);
        reader.ReadBytes().Should().BeEquivalentTo(int16Vector.SelectMany(PackStreamBitConverter.GetBytes).ToArray());
    }

    [Fact]
    public void ShouldSerializeInt32Vector()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        var int32Vector = new[] { 1, 2, 3 };
        var vector = Vector.Create(int32Vector);

        writer.Write(vector);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStreamType.Struct);
        reader.ReadStructHeader().Should().Be(2); // Size of the struct
        reader.ReadStructSignature().Should().Be((byte)'V'); // Vector struct type
        reader.ReadBytes().Should().BeEquivalentTo([PackStream.Int32]);
        reader.ReadBytes().Should().BeEquivalentTo(int32Vector.SelectMany(PackStreamBitConverter.GetBytes).ToArray());
    }

    [Fact]
    public void ShouldSerializeInt64Vector()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        var int64Vector = new[] { 1000L, 2000L, 3000L };
        var vector = Vector.Create(int64Vector);

        writer.Write(vector);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStreamType.Struct);
        reader.ReadStructHeader().Should().Be(2); // Size of the struct
        reader.ReadStructSignature().Should().Be((byte)'V'); // Vector struct type
        reader.ReadBytes().Should().BeEquivalentTo([PackStream.Int64]);
        reader.ReadBytes().Should().BeEquivalentTo(int64Vector.SelectMany(PackStreamBitConverter.GetBytes).ToArray());
    }

    [Fact]
    public void ShouldDeserializeSByteVector()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        var byteVector = new sbyte[] { 1, 2, 3 };

        writer.WriteStructHeader(2, (byte)'V');
        writer.WriteByteArray([PackStream.Int8]);
        writer.WriteByteArray(byteVector.Select(b => (byte)b).ToArray());

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();
        var value = reader.Read();

        value.Should().BeOfType<Vector<sbyte>>();
        var vector = (Vector<sbyte>)value;
        vector.ElementType.Should().Be(typeof(sbyte));
        vector.Values.Should().BeEquivalentTo(byteVector);
    }

    [Fact]
    public void ShouldDeserializeInt16Vector()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        var int16Vector = new short[] { 100, 200, 300 };

        writer.WriteStructHeader(2, (byte)'V');
        writer.WriteByteArray([PackStream.Int16]);
        writer.WriteByteArray(int16Vector.SelectMany(PackStreamBitConverter.GetBytes).ToArray());

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();
        var value = reader.Read();

        value.Should().BeOfType<Vector<short>>();
        var vector = (Vector<short>)value;
        vector.ElementType.Should().Be(typeof(short));
        vector.Values.Should().BeEquivalentTo(int16Vector);
    }

    [Fact]
    public void ShouldDeserializeInt32Vector()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        var int32Vector = new[] { 1, 2, 3 };

        writer.WriteStructHeader(2, (byte)'V');
        writer.WriteByteArray([PackStream.Int32]);
        writer.WriteByteArray(int32Vector.SelectMany(PackStreamBitConverter.GetBytes).ToArray());

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();
        var value = reader.Read();

        value.Should().BeOfType<Vector<int>>();
        var vector = (Vector<int>)value;
        vector.ElementType.Should().Be(typeof(int));
        vector.Values.Should().BeEquivalentTo(int32Vector);
    }

    [Fact]
    public void ShouldDeserializeInt64Vector()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        var int64Vector = new[] { 1000L, 2000L, 3000L };

        writer.WriteStructHeader(2, (byte)'V');
        writer.WriteByteArray([PackStream.Int64]);
        writer.WriteByteArray(int64Vector.SelectMany(PackStreamBitConverter.GetBytes).ToArray());

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();
        var value = reader.Read();

        value.Should().BeOfType<Vector<long>>();
        var vector = (Vector<long>)value;
        vector.ElementType.Should().Be(typeof(long));
        vector.Values.Should().BeEquivalentTo(int64Vector);
    }

    [Fact]
    public void ShouldDeserializeFloat32Vector()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        var float32Vector = new[] { 0.1f, 0.2f, 0.3f };

        writer.WriteStructHeader(2, (byte)'V');
        writer.WriteByteArray([PackStream.Float32]);
        writer.WriteByteArray(float32Vector.SelectMany(PackStreamBitConverter.GetBytes).ToArray());

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();
        var value = reader.Read();

        value.Should().BeOfType<Vector<float>>();
        var vector = (Vector<float>)value;
        vector.ElementType.Should().Be(typeof(float));
        vector.Values.Should().BeEquivalentTo(float32Vector);
    }

    [Fact]
    public void ShouldDeserializeFloat64Vector()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        var float64Vector = new[] { 0.1, 0.2 };

        writer.WriteStructHeader(2, (byte)'V');
        writer.WriteByteArray([PackStream.Float64]);
        writer.WriteByteArray(float64Vector.SelectMany(PackStreamBitConverter.GetBytes).ToArray());

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();
        var value = reader.Read();

        value.Should().BeOfType<Vector<double>>();
        var vector = (Vector<double>)value;
        vector.ElementType.Should().Be(typeof(double));
        vector.Values.Should().BeEquivalentTo(float64Vector);
    }
}

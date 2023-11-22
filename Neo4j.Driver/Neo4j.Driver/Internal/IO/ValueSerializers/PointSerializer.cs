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

namespace Neo4j.Driver.Internal.IO.ValueSerializers;

internal sealed class PointSerializer : IPackStreamSerializer
{
    public const byte Point2DStructType = (byte)'X';
    public const byte Point3DStructType = (byte)'Y';
    public const int Point2DStructSize = 3;
    public const int Point3DStructSize = 4;
    internal static readonly PointSerializer Instance = new();

    public IEnumerable<byte> ReadableStructs => new[] { Point2DStructType, Point3DStructType };
    public IEnumerable<Type> WritableTypes => new[] { typeof(Point) };

    public object Deserialize(BoltProtocolVersion _, PackStreamReader reader, byte signature, long size)
    {
        switch (signature)
        {
            case Point2DStructType:
            {
                PackStream.EnsureStructSize("Point2D", Point2DStructSize, size);
                var srId = reader.ReadInteger();
                var x = reader.ReadDouble();
                var y = reader.ReadDouble();

                return new Point(srId, x, y);
            }

            case Point3DStructType:
            {
                PackStream.EnsureStructSize("Point3D", Point3DStructSize, size);
                var srId = reader.ReadInteger();
                var x = reader.ReadDouble();
                var y = reader.ReadDouble();
                var z = reader.ReadDouble();

                return new Point(srId, x, y, z);
            }

            default:
                throw new ProtocolException(
                    $"Unsupported struct signature {signature} passed to {nameof(PointSerializer)}!");
        }
    }

    public void Serialize(BoltProtocolVersion _, PackStreamWriter writer, object value)
    {
        var point = value.CastOrThrow<Point>();

        switch (point.Dimension)
        {
            case Point.TwoD:
            {
                writer.WriteStructHeader(Point2DStructSize, Point2DStructType);
                writer.WriteInt(point.SrId);
                writer.WriteDouble(point.X);
                writer.WriteDouble(point.Y);

                break;
            }

            case Point.ThreeD:
            {
                writer.WriteStructHeader(Point3DStructSize, Point3DStructType);
                writer.WriteInt(point.SrId);
                writer.WriteDouble(point.X);
                writer.WriteDouble(point.Y);
                writer.WriteDouble(point.Z);

                break;
            }

            default:
                throw new ProtocolException($"{GetType().Name}: Dimension('{point.Dimension}') is not supported.");
        }
    }
}

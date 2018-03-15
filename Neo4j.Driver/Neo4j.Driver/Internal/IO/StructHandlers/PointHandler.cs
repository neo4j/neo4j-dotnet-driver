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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.IO.StructHandlers
{
    internal class PointHandler : IPackStreamStructHandler
    {
        public const byte Point2DStructType = (byte) 'X';
        public const byte Point3DStructType = (byte) 'Y';
        public const int Point2DStructSize = 3;
        public const int Point3DStructSize = 4;

        public IEnumerable<byte> ReadableStructs => new[] {Point2DStructType, Point3DStructType};

        public IEnumerable<Type> WritableTypes => new[] {typeof(Point)};

        public object Read(IPackStreamReader reader, byte signature, long size)
        {
            var srId = 0;
            var x = double.NaN;
            var y = double.NaN;
            var z = double.NaN;

            switch (signature)
            {
                case Point2DStructType:
                {
                    PackStream.EnsureStructSize("Point2D", Point2DStructSize, size);
                    srId = reader.ReadInteger();
                    x = reader.ReadDouble();
                    y = reader.ReadDouble();

                    break;
                }
                case Point3DStructType:
                {
                    PackStream.EnsureStructSize("Point3D", Point3DStructSize, size);
                    srId = reader.ReadInteger();
                    x = reader.ReadDouble();
                    y = reader.ReadDouble();
                    z = reader.ReadDouble();

                    break;
                }
                default:
                    throw new ProtocolException(
                        $"Unsupported struct signature {signature} passed to {nameof(PointHandler)}!");
            }

            return new Point(srId, x, y, z);
        }

        public void Write(IPackStreamWriter writer, object value)
        {
            switch (value)
            {
                case Point point:
                {
                    if (double.IsNaN(point.Z))
                    {
                        writer.WriteStructHeader(Point2DStructSize, Point2DStructType);
                        writer.Write(point.SrId);
                        writer.Write(point.X);
                        writer.Write(point.Y);
                    }
                    else
                    {
                        writer.WriteStructHeader(Point3DStructSize, Point3DStructType);
                        writer.Write(point.SrId);
                        writer.Write(point.X);
                        writer.Write(point.Y);
                        writer.Write(point.Z);
                    }

                    break;
                }
                default:
                    throw new ProtocolException(
                        $"Unsupported type {value.GetType().FullName} passed to {nameof(PointHandler)}");
            }
        }
    }
}
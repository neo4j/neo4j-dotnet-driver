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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Types
{
    internal class Point: IPoint
    {

        public Point(int srId, double x, double y)
            : this(2, srId, x, y, double.NaN)
        {

        }

        public Point(int srId, double x, double y, double z)
            : this(3, srId, x, y, z)
        {

        }

        private Point(int dimension, int srId, double x, double y, double z)
        {
            Dimension = dimension;
            SrId = srId;
            X = x;
            Y = y;
            Z = z;
        }


        internal int Dimension { get; }
        public int SrId { get; } 
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public override string ToString()
        {
            switch (Dimension)
            {
                case 2:
                    return $"Point{{srId={SrId}, x={X}, y={Y}}}";
                case 3:
                    return $"Point{{srId={SrId}, x={X}, y={Y}, z={Z}}}";
                default:
                    return $"Point{{dimension={Dimension}, srId={SrId}, x={X}, y={Y}, z={Z}}}";
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Dimension;
                hashCode = (hashCode * 397) ^ SrId;
                hashCode = (hashCode * 397) ^ X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }

        protected bool Equals(Point other)
        {
            return Dimension == other.Dimension && SrId == other.SrId && X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Point) obj);
        }
    }
}
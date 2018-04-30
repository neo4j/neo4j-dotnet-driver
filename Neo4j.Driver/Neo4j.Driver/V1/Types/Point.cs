// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// Represents a single three-dimensional point in a particular coordinate reference system.
    /// </summary>
    public sealed class Point : IValue, IEquatable<Point>
    {
        internal const int TwoD = 2;
        internal const int ThreeD = 3;

        /// <summary>
        /// Initializes a new instance of <see cref="Point" /> structure with two dimensions
        /// </summary>
        /// <param name="srId"><see cref="SrId" /></param>
        /// <param name="x"><see cref="X" /></param>
        /// <param name="y"><see cref="Y"/></param>
        public Point(int srId, double x, double y)
            : this(TwoD, srId, x, y, double.NaN)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="Point" /> structure with three dimensions
        /// </summary>
        /// <param name="srId"><see cref="SrId" /></param>
        /// <param name="x"><see cref="X" /></param>
        /// <param name="y"><see cref="Y"/></param>
        /// <param name="z"><see cref="Z"/></param>
        public Point(int srId, double x, double y, double z)
            : this(ThreeD, srId, x, y, z)
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

        /// <returns>The coordinate reference system identifier</returns>
        public int SrId { get; }

        /// <returns>X coordinate of the point</returns>
        public double X { get; }

        /// <returns>Y coordinate of the point</returns>
        public double Y { get; }

        /// <returns>Z coordinate of the point</returns>
        public double Z { get; }


        /// <summary>
        /// Converts the value of the current <see cref="Point"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            switch (Dimension)
            {
                case TwoD:
                    return $"Point{{srId={SrId}, x={X}, y={Y}}}";
                case ThreeD:
                    return $"Point{{srId={SrId}, x={X}, y={Y}, z={Z}}}";
                default:
                    return $"Point{{dimension={Dimension}, srId={SrId}, x={X}, y={Y}, z={Z}}}";
            }
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
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

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="Point"/> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(Point other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Dimension == other.Dimension && SrId == other.SrId && X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="Point"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Point && Equals((Point) obj);
        }
    }
}
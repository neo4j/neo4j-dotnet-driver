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
using System.Text.RegularExpressions;

namespace Neo4j.Driver.Internal.Util
{
    internal class ServerVersion : IComparable<ServerVersion>
    {
        public const string Neo4jProduct = "Neo4j";
        private const string InDevVersionString = "Neo4j/dev";

        public static IComparer<ServerVersion> Comparer { get; } = new ServerVersionRelationalComparer();
        public static readonly ServerVersion VInDev = new ServerVersion(int.MaxValue, int.MaxValue, int.MaxValue);
        public static readonly ServerVersion V4_0_0 = new ServerVersion(4, 0, 0);

        private static readonly Regex VersionRegex =
            new Regex($@"({Neo4jProduct}/)?(\d+)\.(\d+)(?:\.)?(\d*)(\.|-|\+)?([0-9A-Za-z-.]*)?",
                RegexOptions.IgnoreCase);

        private readonly string _versionStr;

        public ServerVersion(int major, int minor, int patch, string versionStr = null)
            : this(Neo4jProduct, major, minor, patch, versionStr)
        {
        }

        public ServerVersion(string product, int major, int minor, int patch, string versionStr = null)
        {
            _versionStr = versionStr;
            Product = string.IsNullOrEmpty(product) ? Neo4jProduct : product;
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public string Product { get; }

        public int Major { get; }

        public int Minor { get; }

        public int Patch { get; }

        public override string ToString()
        {
            if (Equals(VInDev))
            {
                return InDevVersionString;
            }

            return _versionStr ?? $"{Product}/{Major}.{Minor}.{Patch}";
        }

        protected bool Equals(ServerVersion other)
        {
            return CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ServerVersion) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Product != null ? Product.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Major;
                hashCode = (hashCode * 397) ^ Minor;
                hashCode = (hashCode * 397) ^ Patch;
                return hashCode;
            }
        }

        public int CompareTo(ServerVersion other)
        {
            return Comparer.Compare(this, other);
        }

        public static bool operator <=(ServerVersion v1, ServerVersion v2)
        {
            return Comparer.Compare(v1, v2) <= 0;
        }

        public static bool operator >=(ServerVersion v1, ServerVersion v2)
        {
            return Comparer.Compare(v1, v2) >= 0;
        }

        public static ServerVersion From(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentNullException(nameof(version));
            }

            if (version == InDevVersionString) return VInDev;

            var match = VersionRegex.Match(version);
            if (match.Success)
            {
                var product = match.Groups[1].Value.TrimEnd('/');
                var major = int.Parse(match.Groups[2].Value);
                var minor = int.Parse(match.Groups[3].Value);
                var patch = 0;
                var patchString = match.Groups[4].Value;
                if (!string.IsNullOrEmpty(patchString))
                {
                    patch = int.Parse(patchString);
                }

                return new ServerVersion(product, major, minor, patch, version);
            }

            throw new ArgumentOutOfRangeException($"Unexpected server version format: {version}");
        }

        private sealed class ServerVersionRelationalComparer : IComparer<ServerVersion>
        {
            public int Compare(ServerVersion x, ServerVersion y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;
                var productComparison = string.Compare(x.Product, y.Product, StringComparison.Ordinal);
                if (productComparison != 0) return productComparison;
                var majorComparison = x.Major.CompareTo(y.Major);
                if (majorComparison != 0) return majorComparison;
                var minorComparison = x.Minor.CompareTo(y.Minor);
                if (minorComparison != 0) return minorComparison;
                return x.Patch.CompareTo(y.Patch);
            }
        }
    }
}
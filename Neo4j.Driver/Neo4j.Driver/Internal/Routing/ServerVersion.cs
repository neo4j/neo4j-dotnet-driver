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
using System.Text.RegularExpressions;

namespace Neo4j.Driver.Internal.Routing
{
    internal class ServerVersion : IComparable<ServerVersion>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }

        public static readonly ServerVersion V3_1_0 = new ServerVersion(3,1,0);
        public static readonly ServerVersion V3_2_0 = new ServerVersion(3,2,0);

        private static readonly Regex VersionRegex = new Regex(@"(Neo4j/)?(\d+)\.(\d+)(?:\.)?(\d*)(\.|-|\+)?([0-9A-Za-z-.]*)?", RegexOptions.IgnoreCase);

        public ServerVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public static ServerVersion Version(string version)
        {
            if (version == null)
            {
                return null;
            }
            var match = VersionRegex.Match(version);
            if (match.Success)
            {
                var major = int.Parse(match.Groups[2].ToString());
                var minor = int.Parse(match.Groups[3].ToString());
                var patch = 0;
                var patchString = match.Groups[4].ToString();
                if ( patchString != null && patchString.Length != 0 )
                {
                    patch = int.Parse( patchString );
                }
                return new ServerVersion(major, minor, patch);
            }
            return null;
        }

        private static int Compare(ServerVersion v1, ServerVersion v2)
        {
            if (v1 == null && v2 == null)
            {
                return 0;
            }
            if (v1 == null)
            {
                return -1;
            }
            if (v2 == null)
            {
                return 1;
            }

            var code = v1.Major - v2.Major;
            if (code == 0)
            {
                code = v1.Minor - v2.Minor;
                if (code == 0)
                {
                    code = v1.Patch - v2.Patch;
                }
            }
            return code;
        }

        public int CompareTo(ServerVersion other)
        {
            return Compare(this, other);
        }

        public static bool operator <=(ServerVersion v1, ServerVersion v2)
        {
            return Compare(v1, v2) <= 0;
        }

        public static bool operator >=(ServerVersion v1, ServerVersion v2)
        {
            return Compare(v1, v2) >= 0;
        }
    }
}

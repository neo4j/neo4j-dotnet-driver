// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

namespace Neo4j.Driver.Internal;

//TODO: Consider converting to struct.
internal sealed class BoltProtocolVersion : IEquatable<BoltProtocolVersion>
{
    // The int 1213486160 is 0x‭48 54 54 50 - or HTTP in ascii codes... this determines the max major and minor versions supported.
    public const int MaxMajorVersion = 80;
    public const int MaxMinorVersion = 84;

    private const int PackingIntValue = 0x00FF;

    public static readonly BoltProtocolVersion V3_0 = new(3, 0);
    public static readonly BoltProtocolVersion V4_0 = new(4, 0);
    public static readonly BoltProtocolVersion V4_1 = new(4, 1);
    public static readonly BoltProtocolVersion V4_2 = new(4, 2);
    public static readonly BoltProtocolVersion V4_3 = new(4, 3);
    public static readonly BoltProtocolVersion V4_4 = new(4, 4);
    public static readonly BoltProtocolVersion V5_0 = new(5, 0);
    public static readonly BoltProtocolVersion V5_1 = new(5, 1);
    public static readonly BoltProtocolVersion V5_2 = new(5, 2);
    private readonly int _compValue;

    public BoltProtocolVersion(int majorVersion, int minorVersion)
    {
        if (majorVersion > MaxMajorVersion || minorVersion > MaxMinorVersion || majorVersion < 0 || minorVersion < 0)
        {
            throw new NotSupportedException(
                "Attempting to create a BoltProtocolVersion with out of bounds major: " +
                majorVersion +
                " or minor: " +
                minorVersion);
        }

        MajorVersion = majorVersion;
        MinorVersion = minorVersion;
        _compValue = MajorVersion * 1000000 + MinorVersion;
    }

    public BoltProtocolVersion(int largeVersion)
    {
        //This version of the constructor is only to be used to handle error codes that come in that are not strictly containing packed values. 
        MajorVersion = UnpackMajor(largeVersion);
        MinorVersion = UnpackMinor(largeVersion);
        _compValue = MajorVersion * 1000000 + MinorVersion;

        if (MajorVersion is < MaxMajorVersion and >= 0 && MinorVersion is < MaxMinorVersion and >= 0)
        {
            throw new NotSupportedException(
                "Attempting to create a BoltProtocolVersion with a large (error code) version number.  " +
                "Resulting Major and Minor are in range of valid versions, which is not allowed: " +
                MajorVersion +
                " or minor: " +
                MinorVersion);
        }
    }

    public int MajorVersion { get; }
    public int MinorVersion { get; }

    public bool Equals(BoltProtocolVersion rhs)
    {
        if (rhs is null)
        {
            return false;
        }

        if (ReferenceEquals(this, rhs))
        {
            return true;
        }

        if (GetType() != rhs.GetType())
        {
            return false;
        }

        //Return if the fields match
        return _compValue == rhs._compValue;
    }

    private static int UnpackMajor(int rawVersion)
    {
        return rawVersion & PackingIntValue;
    }

    private static int UnpackMinor(int rawVersion)
    {
        return (rawVersion >> 8) & PackingIntValue;
    }

    public static BoltProtocolVersion FromPackedInt(int rawVersion)
    {
        return new BoltProtocolVersion(UnpackMajor(rawVersion), UnpackMinor(rawVersion));
    }

    public void CheckVersionRange(BoltProtocolVersion minVersion)
    {
        if (MajorVersion != minVersion.MajorVersion)
        {
            throw new NotSupportedException("Versions should be from same major version");
        }

        if (MinorVersion < minVersion.MinorVersion)
        {
            throw new NotSupportedException("Max version should be newer than minimum version");
        }
    }

    public int PackToIntRange(BoltProtocolVersion minVersion)
    {
        CheckVersionRange(minVersion);

        var range = MinorVersion - minVersion.MinorVersion;
        return (range << 16) | PackToInt();
    }

    public int PackToInt()
    {
        return (MinorVersion << 8) | MajorVersion;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as BoltProtocolVersion);
    }

    public bool Equals(int majorVersion, int minorVersion)
    {
        var tempVersion = new BoltProtocolVersion(majorVersion, minorVersion);
        return Equals(tempVersion);
    }

    public static bool operator ==(BoltProtocolVersion lhs, BoltProtocolVersion rhs)
    {
        return lhs._compValue == rhs._compValue;
    }

    public static bool operator !=(BoltProtocolVersion lhs, BoltProtocolVersion rhs)
    {
        return lhs._compValue != rhs._compValue;
    }

    public static bool operator >=(BoltProtocolVersion lhs, BoltProtocolVersion rhs)
    {
        return lhs._compValue >= rhs._compValue;
    }

    public static bool operator <=(BoltProtocolVersion lhs, BoltProtocolVersion rhs)
    {
        return lhs._compValue <= rhs._compValue;
    }

    public static bool operator >(BoltProtocolVersion lhs, BoltProtocolVersion rhs)
    {
        return lhs._compValue > rhs._compValue;
    }

    public static bool operator <(BoltProtocolVersion lhs, BoltProtocolVersion rhs)
    {
        return lhs._compValue < rhs._compValue;
    }

    public override int GetHashCode()
    {
        //Using a Tuple object rather than XOR'ing the values so that MajorVersion.MinorVersion does not return the same hashcode as MinorVersion.MajorVersion.
        //e.g. We dont want 4.1 == 1.4
        //Be aware of perfomance of Tuple instantiation if using a lot of BoltProtocolVersion in containers.
        return _compValue;
    }

    public override string ToString()
    {
        return $"{MajorVersion}.{MinorVersion}";
    }
}

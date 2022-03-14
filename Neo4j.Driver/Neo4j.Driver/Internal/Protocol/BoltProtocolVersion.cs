using System;

namespace Neo4j.Driver.Internal.Protocol
{
    class BoltProtocolVersion : IEquatable<BoltProtocolVersion>
    {
        public static readonly BoltProtocolVersion V3_0 = new BoltProtocolVersion(3, 0);
        public static readonly BoltProtocolVersion V4_0 = new BoltProtocolVersion(4, 0);
        public static readonly BoltProtocolVersion V4_1 = new BoltProtocolVersion(4, 1);
        public static readonly BoltProtocolVersion V4_2 = new BoltProtocolVersion(4, 2);
        public static readonly BoltProtocolVersion V4_3 = new BoltProtocolVersion(4, 3);
        public static readonly BoltProtocolVersion V4_4 = new BoltProtocolVersion(4, 4);
        public static readonly BoltProtocolVersion V5_0 = new BoltProtocolVersion(5, 0);

        public int MajorVersion { get; }
        public int MinorVersion { get; }

        // The int 1213486160 is 0x‭48 54 54 50 - or HTTP in ascii codes... this determines the max major and minor versions supported.
        public const int MaxMajorVersion = 80;
        public const int MaxMinorVersion = 84;

        private const int PackingIntValue = 0x00FF;
        private const ushort PackingUShortValue = 0x00FF;
        private const byte PackingByteValue = 0x000F;

        public BoltProtocolVersion(int majorVersion, int minorVersion)
        {
            if(majorVersion > MaxMajorVersion ||  minorVersion > MaxMinorVersion ||  majorVersion < 0  ||  minorVersion < 0  )
            {
                throw new NotSupportedException("Attempting to create a BoltProtocolVersion with out of bounds major: "+ majorVersion + " or minor: " + minorVersion);
            }

            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
        }

        public BoltProtocolVersion(int largeVersion)
        {   
            //This version of the constructor is only to be used to handle error codes that come in that are not strictly containing packed values. 
            MajorVersion = UnpackMajor(largeVersion);
            MinorVersion = UnpackMinor(largeVersion);

            if ((MajorVersion < MaxMajorVersion && MajorVersion >= 0) && (MinorVersion < MaxMinorVersion && MinorVersion >= 0))
            {
                throw new NotSupportedException("Attempting to create a BoltProtocolVersion with a large (error code) version number.  " +
                                                "Resulting Major and Minor are in range of valid versions, which is not allowed: " + MajorVersion + " or minor: " + MinorVersion);
            }
        }

        private static int UnpackMajor(int rawVersion)
        {
            return (rawVersion & PackingIntValue);
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
            else if(MinorVersion < minVersion.MinorVersion)
			{
                throw new NotSupportedException("Max version should be newer than minimum version");
            }
        }

        public int PackToIntRange(BoltProtocolVersion minVersion)
        {
            CheckVersionRange(minVersion);

            int range = MinorVersion - minVersion.MinorVersion;
            return (range << 16) | PackToInt();
        }

        public int PackToInt()
        {
            return (MinorVersion << 8) | MajorVersion;
        }

        public static BoltProtocolVersion FromPackedUShort(ushort rawVersion)
        {
            int major = rawVersion & PackingUShortValue;
            int minor = (rawVersion >> 8) & PackingUShortValue;

            return new BoltProtocolVersion(major, minor);
        }
        
        public ushort PackToUShort()
        {
            return (ushort)((MinorVersion << 8) | MajorVersion);
        }

        public static BoltProtocolVersion FromPackedByte(byte rawVersion)
        {
            int major = rawVersion & PackingByteValue;
            int minor = (rawVersion >> 4) & PackingByteValue;

            return new BoltProtocolVersion(major, minor);
        }

        public byte PackToByte()
        {
            return (byte)((MinorVersion << 4) | MajorVersion);
        }
        
        public override bool Equals(object obj)
        {
            return this.Equals(obj as BoltProtocolVersion);
        }

        public bool Equals(int majorVersion, int minorVersion)
        {
            var tempVersion = new BoltProtocolVersion(majorVersion, minorVersion);
            return this.Equals(tempVersion);
        }

        public bool Equals(BoltProtocolVersion rhs)
        {
            if(rhs is null)
            {
                return false;
            }

            if(Object.ReferenceEquals(this, rhs))
            {
                return true;
            }

            if(this.GetType() != rhs.GetType())
            {
                return false;
            }

            //Return if the fields match
            return (MajorVersion == rhs.MajorVersion)  &&  (MinorVersion == rhs.MinorVersion);
        }



        public static bool operator==(BoltProtocolVersion lhs, BoltProtocolVersion rhs)
        {
            if(Object.ReferenceEquals(lhs, null))
            {
                if(Object.ReferenceEquals(rhs, null))
                {
                    //null == null
                    return true;
                }

                return false;
            }

            //Equals handles case of null on rhs.
            return lhs.Equals(rhs);
        }

        public static bool operator!=(BoltProtocolVersion lhs, BoltProtocolVersion rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator>=(BoltProtocolVersion lhs, BoltProtocolVersion rhs)
        {
            return (lhs == rhs) || (lhs > rhs);
        }

        public static bool operator <=(BoltProtocolVersion lhs, BoltProtocolVersion rhs)
        {
            return (lhs == rhs) || (lhs < rhs);              
        }

        public static bool operator >(BoltProtocolVersion lhs, BoltProtocolVersion rhs)
        {
            if (lhs == rhs)
                return false;

            if (lhs.MajorVersion < rhs.MajorVersion)
            {
                return false;
            }
            else
            {
                if (lhs.MinorVersion < rhs.MinorVersion)
                    return false;
            }

            return true;
        }

        public static bool operator <(BoltProtocolVersion lhs, BoltProtocolVersion rhs)
        {
            if (lhs == rhs)
                return false;

            if (lhs.MajorVersion > rhs.MajorVersion)
            {
                return false;
            }
            else
            {
                if (lhs.MinorVersion > rhs.MinorVersion)
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            //Using a Tuple object rather than XOR'ing the values so that MajorVersion.MinorVersion does not return the same hashcode as MinorVersion.MajorVersion.
            //e.g. We dont want 4.1 == 1.4
            //Be aware of perfomance of Tuple instantiation if using a lot of BoltProtocolVersion in containers.
            return Tuple.Create(MajorVersion, MinorVersion).GetHashCode();
        }

		public override string ToString()
		{
			return $"{MajorVersion}.{MinorVersion}";
		}
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4j.Driver.Internal.Protocol
{   
    // TODO: Document this

    class BoltProtocolVersion : IEquatable<BoltProtocolVersion>
    {
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }

        private const int PackingIntValue = 0xFFFF;
        private const ushort PackingUShortValue = 0x00FF;
        private const byte PackingByteValue = 0x000F;

        public BoltProtocolVersion(int majorVersion, int minorVersion)
        {
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
        }

        public static BoltProtocolVersion FromPackedInt(int rawVersion)
        {
            int major = rawVersion & PackingIntValue;
            int minor = (rawVersion >> 16) & PackingIntValue;

            return new BoltProtocolVersion(major, minor);
        }



        public int PackToInt()
        {
            return (MinorVersion << 16) | MajorVersion;
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
    }
}

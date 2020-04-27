using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Neo4j.Driver.Tests;
using Moq;
using FluentAssertions;

namespace Neo4j.Driver.Internal.Protocol
{
    public class BoltProtocolVersionTests
    {
        [Theory]
        [InlineData(2, 0, 1, 0)]
        [InlineData(1, 1, 1, 0)]
        [InlineData(2, 1, 1, 0)]
        public void GreaterThanSuccess(int lhsMajor, int lhsMinor, int rhsMajor, int rhsMinor)
        {
            var lhs = new BoltProtocolVersion(lhsMajor, lhsMinor);
            var rhs = new BoltProtocolVersion(rhsMajor, rhsMinor);

            var result = (lhs > rhs);
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(1, 0, 2, 0)]
        [InlineData(1, 0, 1, 1)]
        [InlineData(1, 0, 2, 1)]
        [InlineData(1, 0, 1, 0)]
        public void GreaterThanFailure(int lhsMajor, int lhsMinor, int rhsMajor, int rhsMinor)
        {
            var lhs = new BoltProtocolVersion(lhsMajor, lhsMinor);
            var rhs = new BoltProtocolVersion(rhsMajor, rhsMinor);

            var result = (lhs > rhs);
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(2, 0, 1, 0)]
        [InlineData(1, 1, 1, 0)]
        [InlineData(2, 1, 1, 0)]
        [InlineData(1, 0, 1, 0)]
        public void GreaterOrEqaulThanSuccess(int lhsMajor, int lhsMinor, int rhsMajor, int rhsMinor)
        {
            var lhs = new BoltProtocolVersion(lhsMajor, lhsMinor);
            var rhs = new BoltProtocolVersion(rhsMajor, rhsMinor);

            var result = (lhs >= rhs);
            result.Should().BeTrue();
        }




        [Theory]
        [InlineData(1, 0, 2, 0)]
        [InlineData(1, 0, 1, 1)]
        [InlineData(1, 0, 2, 1)]
        public void LessThanSuccess(int lhsMajor, int lhsMinor, int rhsMajor, int rhsMinor)
        {
            var lhs = new BoltProtocolVersion(lhsMajor, lhsMinor);
            var rhs = new BoltProtocolVersion(rhsMajor, rhsMinor);

            var result = (lhs < rhs);
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(2, 0, 0, 0)]
        [InlineData(1, 1, 1, 0)]
        [InlineData(2, 1, 1, 0)]
        [InlineData(1, 0, 1, 0)]
        public void LessThanFailure(int lhsMajor, int lhsMinor, int rhsMajor, int rhsMinor)
        {
            var lhs = new BoltProtocolVersion(lhsMajor, lhsMinor);
            var rhs = new BoltProtocolVersion(rhsMajor, rhsMinor);

            var result = (lhs < rhs);
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(1, 0, 2, 0)]
        [InlineData(1, 0, 1, 1)]
        [InlineData(1, 0, 2, 1)]
        [InlineData(1, 0, 1, 0)]
        public void LessOrEqaulThanSuccess(int lhsMajor, int lhsMinor, int rhsMajor, int rhsMinor)
        {
            var lhs = new BoltProtocolVersion(lhsMajor, lhsMinor);
            var rhs = new BoltProtocolVersion(rhsMajor, rhsMinor);

            var result = (lhs <= rhs);
            result.Should().BeTrue();
        }




        [Theory]
        [InlineData(1, 0, 1, 0)]
        [InlineData(1, 1, 1, 1)]
        public void EqualitySuccess(int lhsMajor, int lhsMinor, int rhsMajor, int rhsMinor)
        {
            var lhs = new BoltProtocolVersion(lhsMajor, lhsMinor);
            var rhs = new BoltProtocolVersion(rhsMajor, rhsMinor);

            var result = (lhs == rhs);
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(1, 0, 1, 1)]
        [InlineData(1, 0, 2, 0)]
        [InlineData(1, 0, 2, 1)]
        public void EqualityFailure(int lhsMajor, int lhsMinor, int rhsMajor, int rhsMinor)
        {
            var lhs = new BoltProtocolVersion(lhsMajor, lhsMinor);
            var rhs = new BoltProtocolVersion(rhsMajor, rhsMinor);

            var result = (lhs == rhs);
            result.Should().BeFalse();
        }

        [Theory] 
        [InlineData(1, 0, 1, 1)]
        [InlineData(2, 0, 1, 0)]
        [InlineData(2, 0, 1, 1)]
        public void InequalitySuccess(int lhsMajor, int lhsMinor, int rhsMajor, int rhsMinor)
        {
            var lhs = new BoltProtocolVersion(lhsMajor, lhsMinor);
            var rhs = new BoltProtocolVersion(rhsMajor, rhsMinor);

            var result = (lhs != rhs);
            result.Should().BeTrue();
        }


        [Theory]
        [InlineData(1, 0, 1, 0)]
        [InlineData(1, 1, 1, 1)]
        public void InequalityFailure(int lhsMajor, int lhsMinor, int rhsMajor, int rhsMinor)
        {
            var lhs = new BoltProtocolVersion(lhsMajor, lhsMinor);
            var rhs = new BoltProtocolVersion(rhsMajor, rhsMinor);

            var result = (lhs != rhs);
            result.Should().BeFalse();
        }

        [Fact]
        public void EqualsSuccess()
        {
            var v1 = new BoltProtocolVersion(1, 1);
            var v2 = new BoltProtocolVersion(1, 1);
            (v1.Equals(v2)).Should().BeTrue();

            v2 = new BoltProtocolVersion(1, 3);
            (v1.Equals(v2)).Should().BeFalse();

            (v1.Equals(null)).Should().BeFalse();
        }

        [Fact]
        public void PackAndUnpackSuccess()
        {   
            const int    packedIntVersion = 65540,
                         majorVersion = 4,
                         minorVersion = 1;
            const ushort packedShortVersion = 260;
            const byte   packedByteVersion = 20;
                        
            var bv = new BoltProtocolVersion(majorVersion, minorVersion);

            (bv.PackToInt() == packedIntVersion).Should().BeTrue();
            (bv.PackToUShort() == packedShortVersion).Should().BeTrue();
            (bv.PackToByte() == packedByteVersion).Should().BeTrue();

            bv = BoltProtocolVersion.FromPackedInt(packedIntVersion);
            (bv.MajorVersion == majorVersion && bv.MinorVersion == minorVersion).Should().BeTrue();

            bv = BoltProtocolVersion.FromPackedUShort(packedShortVersion);
            (bv.MajorVersion == majorVersion && bv.MinorVersion == minorVersion).Should().BeTrue();

            bv = BoltProtocolVersion.FromPackedByte(packedByteVersion);
            (bv.MajorVersion == majorVersion && bv.MinorVersion == minorVersion).Should().BeTrue();
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(256, 0)]
        [InlineData(1, 256)]
        [InlineData(1, -1)]
        public void ProtocolBoundsTest(int majorVersion, int minorVersion)
        {
            string errorMessage = "Attempting to create a BoltProtocolVersion with out of bounds major: " + majorVersion + " or minor: " + minorVersion;
            var exception = Record.Exception(() => new BoltProtocolVersion(majorVersion, minorVersion));
            exception.Should().BeOfType<NotSupportedException>();
            exception.Message.Should().StartWith(errorMessage);
        }

        [Fact]
        public void ProtocolLargeBoundsTest()
        {
            int successLargeNumber = 1213486160;    ////0x‭48 54 54 50 - or HTTP in ascii codes...
            const int majorVersion = 21584,
                      minorVersion = 18516;
            var bv = new BoltProtocolVersion(successLargeNumber);
            (bv.MajorVersion == majorVersion && bv.MinorVersion == minorVersion).Should().BeTrue();
            
            int failureLargeNumber = 15728881;
            string errorMessage = "Attempting to create a BoltProtocolVersion with a large (error code) version number.  Resulting Major and Minor are in range of valid versions, which is not allowed: ";

            var exception = Record.Exception(() => new BoltProtocolVersion(failureLargeNumber));
            exception.Should().BeOfType<NotSupportedException>();
            exception.Message.Should().StartWith(errorMessage);
        }

    }
}

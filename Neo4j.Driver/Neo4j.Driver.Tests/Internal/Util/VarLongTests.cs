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
using FluentAssertions;
using Neo4j.Driver.Internal.Util;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Util;

public class VarLongTests
{
    public class AddSegmentMethod
    {
        [Fact]
        public void ShouldAddNewSegment()
        {
            var varLong = new VarLong();
            const byte newSegment = 0x1;
            varLong.AddSegment(newSegment);

            varLong.Value.Should().Be(newSegment);
        }

        [Theory]
        [InlineData(new byte[] { 0x8F, 0x01 }, 143)]          // = 0000001 0001111  
        [InlineData(new byte[] { 0xFF, 0x01 }, 255)]          // = 0000001 1111111
        [InlineData(new byte[] { 0xFF, 0xFF, 0x01 }, 32767)]  // = 0000001 1111111 1111111
        [InlineData(new byte[] { 0x81, 0x81, 0x01 }, 16513)]  // = 0000001 0000001 0000001
        [InlineData(new byte[] { 0x8F, 0x8F, 0x04 }, 67471)]  // = 0000100 0001111 0001111
        public void ShouldAddMultipleNewSegments(byte[] data, long finalValue)
        {
            var varLong = new VarLong();

            foreach (var element in data)
            {
                varLong.AddSegment(element);
            }

            varLong.Value.Should().Be(finalValue);
        }

        [Fact]
        public void ShouldSegmentFaultOnTooManyAdds()
        {
            var varLong = new VarLong();
            const byte newSegment = 0x1;

            var exception = Record.Exception(
                () =>
                {
                    for (var i = 0; i < 9; i++)
                    {
                        varLong.AddSegment(newSegment);
                    }
                });

            exception.Should().BeOfType<ArgumentException>().Which.Message.Should().Be("VarLong Segment overflow");  
        }
    }
}

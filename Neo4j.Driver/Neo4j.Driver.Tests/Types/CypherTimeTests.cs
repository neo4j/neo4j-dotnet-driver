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
using FluentAssertions;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.Types
{
    public class CypherTimeTests
    {

        [Fact]
        public void ShouldCreateTimeWithTimeComponents()
        {
            var cypherTime = new CypherTime(13, 15, 59);

            cypherTime.ToTimeSpan().Should().Be(new TimeSpan(13, 15, 59));
        }

        [Fact]
        public void ShouldCreateTimeWithTimeSpan()
        {
            var time = new TimeSpan(0, 13, 59, 59, 255);
            var cypherTime = new CypherTime(time);

            cypherTime.ToTimeSpan().Should().Be(time);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(24)]
        public void ShouldThrowOnInvalidHour(int hour)
        {
            var ex = Record.Exception(() => new CypherTime(hour, 0, 0, 0));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(60)]
        [InlineData(61)]
        public void ShouldThrowOnInvalidMinute(int minute)
        {
            var ex = Record.Exception(() => new CypherTime(0, minute, 0, 0));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(60)]
        [InlineData(61)]
        public void ShouldThrowOnInvalidSecond(int second)
        {
            var ex = Record.Exception(() => new CypherTime(0, 0, second, 0));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(999_999_999 + 1)]
        public void ShouldThrowOnInvalidNanosecond(int nanosecond)
        {
            var ex = Record.Exception(() => new CypherTime(0, 0, 0, nanosecond));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(13, 15, 59, 274000000, "13:15:59.274000000")]
        [InlineData(0, 1, 2, 000000000, "00:01:02.000000000")]
        [InlineData(0, 1, 2, 5001, "00:01:02.000005001")]
        public void ShouldGenerateCorrectString(int hour, int minute, int second, int nanosecond, string expected)
        {
            var cypherTime = new CypherTime(hour, minute, second, nanosecond);
            var cypherTimeStr = cypherTime.ToString();

            cypherTimeStr.Should().Be(expected);
        }

        [Fact]
        public void ShouldGenerateSameHashcode()
        {
            var time1 = new CypherTime(12, 49, 55, 123000000);
            var time2 = new CypherTime(new DateTime(2017, 1, 1, 12, 49, 55, 123));
            var time3 = new CypherTime(new TimeSpan(0, 12, 49, 55, 123));

            time1.GetHashCode().Should().Be(time2.GetHashCode()).And.Be(time3.GetHashCode());
        }

        [Fact]
        public void ShouldGenerateDifferentHashcode()
        {
            var time1 = new CypherTime(12, 49, 55, 123000001);
            var time2 = new CypherTime(new DateTime(2017, 1, 1, 12, 49, 55, 123));
            var time3 = new CypherTime(new TimeSpan(0, 12, 49, 55, 124));

            time1.GetHashCode().Should().NotBe(time2.GetHashCode()).And.NotBe(time3.GetHashCode());
        }

        [Fact]
        public void ShouldBeEqual()
        {
            var time1 = new CypherTime(12, 49, 55, 123000000);
            var time2 = new CypherTime(new DateTime(2017, 1, 1, 12, 49, 55, 123));
            var time3 = new CypherTime(new TimeSpan(0, 12, 49, 55, 123));

            time1.Equals(time2).Should().BeTrue();
            time1.Equals(time3).Should().BeTrue();
        }

        [Fact]
        public void ShouldNotBeEqual()
        {
            var time1 = new CypherTime(12, 49, 55, 123000001);
            var time2 = new CypherTime(new DateTime(2017, 1, 1, 12, 49, 55, 123));
            var time3 = new CypherTime(new TimeSpan(0, 12, 49, 55, 125));

            time1.Equals(time2).Should().BeFalse();
            time1.Equals(time3).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToAnotherType()
        {
            var time = new CypherTime(12, 49, 55, 123000001);
            var other = "some string";

            time.Equals(other).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToNull()
        {
            var time = new CypherTime(12, 49, 55, 123000001);
            var other = (object)null;

            time.Equals(other).Should().BeFalse();
        }
    }
}
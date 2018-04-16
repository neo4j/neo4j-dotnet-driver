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
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.Types
{
    public class CypherDateTimeWithZoneIdTests
    {

        [Fact]
        public void ShouldCreateDateTimeWithZoneIdWithDateTimeComponents()
        {
            var cypherDateTime = new CypherDateTimeWithZoneId(1947, 12, 17, 23, 49, 54, "Europe/Rome");

            cypherDateTime.DateTime.Should().Be(new DateTime(1947, 12, 17, 23, 49, 54));
            cypherDateTime.Offset.Should().Be(TimeSpan.FromHours(1));
            cypherDateTime.ZoneId.Should().Be("Europe/Rome");
        }

        [Fact]
        public void ShouldCreateDateTimeWithZoneIdWithDateTimeComponentsWithNanoseconds()
        {
            var cypherDateTime = new CypherDateTimeWithZoneId(1947, 12, 17, 23, 49, 54, 192794500, "Europe/Rome");

            cypherDateTime.DateTime.Should().Be(new DateTime(1947, 12, 17, 23, 49, 54).AddTicks(1927945));
            cypherDateTime.Offset.Should().Be(TimeSpan.FromHours(1));
            cypherDateTime.ZoneId.Should().Be("Europe/Rome");
        }

        [Fact]
        public void ShouldCreateDateTimeWithZoneIdWithDateTime()
        {
            var dateTime = new DateTime(1947, 12, 17, 23, 49, 54, 120);
            var cypherDateTime = new CypherDateTimeWithZoneId(dateTime, "Europe/Rome");

            cypherDateTime.DateTime.Should().Be(dateTime);
            cypherDateTime.Offset.Should().Be(TimeSpan.FromHours(1));
            cypherDateTime.ZoneId.Should().Be("Europe/Rome");
        }

        [Theory]
        [InlineData(-1000000000)]
        [InlineData(1000000000)]
        public void ShouldThrowOnInvalidYear(int year)
        {
            var ex = Record.Exception(() => new CypherDateTimeWithZoneId(year, 1, 1, 0, 0, 0, "Europe/Amsterdam"));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public void ShouldThrowOnInvalidMonth(int month)
        {
            var ex = Record.Exception(() => new CypherDateTimeWithZoneId(1990, month, 1, 0, 0, 0, "Europe/Istanbul"));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(2018, 1, 0)]
        [InlineData(2018, 1, 32)]
        [InlineData(2018, 6, 31)]
        [InlineData(2018, 2, 29)]
        [InlineData(2018, 12, -1)]
        public void ShouldThrowOnInvalidDay(int year, int month, int day)
        {
            var ex = Record.Exception(() => new CypherDateTimeWithZoneId(year, month, day, 0, 0, 0, "Europe/Istanbul"));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(24)]
        public void ShouldThrowOnInvalidHour(int hour)
        {
            var ex = Record.Exception(() => new CypherDateTimeWithZoneId(1990, 1, 1, hour, 0, 0, "Europe/Istanbul"));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(60)]
        [InlineData(61)]
        public void ShouldThrowOnInvalidMinute(int minute)
        {
            var ex = Record.Exception(() => new CypherDateTimeWithZoneId(1990, 1, 1, 0, minute, 0, "Europe/Paris"));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(60)]
        [InlineData(61)]
        public void ShouldThrowOnInvalidSecond(int second)
        {
            var ex = Record.Exception(() => new CypherDateTimeWithZoneId(1990, 1, 1, 0, 0, second, "Europe/Rome"));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(999_999_999 + 1)]
        public void ShouldThrowOnInvalidNanosecond(int nanosecond)
        {
            var ex = Record.Exception(() => new CypherDateTimeWithZoneId(1990, 1, 1, 0, 0, 0, nanosecond, "Europe/Athens"));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(-9999)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(10000)]
        [InlineData(9999999)]
        public void ShouldThrowOnOverflow(int year)
        {
            var dateTime = new CypherDateTimeWithZoneId(year, 1, 1, 0, 0, 0, 0, "Europe/London");
            var ex = Record.Exception(() => dateTime.DateTime);

            ex.Should().NotBeNull().And.BeOfType<ValueOverflowException>();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(20)]
        [InlineData(99)]
        [InlineData(999000727)]
        [InlineData(999000750)]
        [InlineData(999000001)]
        public void ShouldThrowOnTruncation(int nanosecond)
        {
            var dateTime = new CypherDateTimeWithZoneId(1, 1, 1, 0, 0, 0, nanosecond, "Europe/London");
            var ex = Record.Exception(() => dateTime.DateTime);

            ex.Should().NotBeNull().And.BeOfType<ValueTruncationException>();
        }

        [Theory]
        [InlineData(1947, 12, 17, 23, 5, 54, 192794500, "Europe/Rome", "1947-12-17T23:05:54.192794500[Europe/Rome]")]
        [InlineData(1947, 12, 5, 0, 5, 54, 192794500, "Europe/Amsterdam", "1947-12-05T00:05:54.192794500[Europe/Amsterdam]")]
        [InlineData(1947, 12, 5, 0, 5, 54, 0, "Europe/Istanbul", "1947-12-05T00:05:54.000000000[Europe/Istanbul]")]
        [InlineData(5, 1, 5, 0, 5, 54, 0, "Africa/Nairobi", "0005-01-05T00:05:54.000000000[Africa/Nairobi]")]
        [InlineData(-5, 1, 5, 0, 5, 54, 1250, "America/Halifax", "-0005-01-05T00:05:54.000001250[America/Halifax]")]
        [InlineData(999999, 1, 1, 5, 1, 25, 1, "America/New_York", "999999-01-01T05:01:25.000000001[America/New_York]")]
        [InlineData(-999999, 1, 1, 5, 1, 25, 1, "Asia/Seoul", "-999999-01-01T05:01:25.000000001[Asia/Seoul]")]
        public void ShouldGenerateCorrectString(int year, int month, int day, int hour, int minute, int second, int nanosecond, string zoneId, string expected)
        {
            var cypherDateTime = new CypherDateTimeWithZoneId(year, month, day, hour, minute, second, nanosecond, zoneId);
            var cypherDateTimeStr = cypherDateTime.ToString();

            cypherDateTimeStr.Should().Be(expected);
        }

        [Fact]
        public void ShouldGenerateSameHashcode()
        {
            var dateTime1 = new CypherDateTimeWithZoneId(1947, 12, 17, 15, 12, 01, 789000000, "Europe/Rome");
            var dateTime2 = new CypherDateTimeWithZoneId(new DateTime(1947, 12, 17, 15, 12, 01, 789), "Europe/Rome");

            dateTime1.GetHashCode().Should().Be(dateTime2.GetHashCode());
        }

        [Fact]
        public void ShouldGenerateDifferentHashcode()
        {
            var dateTime1 = new CypherDateTimeWithZoneId(1947, 12, 17, 15, 12, 01, 789000000, "Europe/Rome");
            var dateTime2 = new CypherDateTimeWithZoneId(new DateTime(1947, 12, 17, 15, 12, 01, 790), "Europe/Rome");

            dateTime1.GetHashCode().Should().NotBe(dateTime2.GetHashCode());
        }

        [Fact]
        public void ShouldBeEqual()
        {
            var dateTime1 = new CypherDateTimeWithZoneId(1947, 12, 17, 15, 12, 01, 789000000, "Europe/Rome");
            var dateTime2 = new CypherDateTimeWithZoneId(new DateTime(1947, 12, 17, 15, 12, 01, 789), "Europe/Rome");

            dateTime1.Should().Be(dateTime2);
        }

        [Fact]
        public void ShouldNotBeEqual()
        {
            var dateTime1 = new CypherDateTimeWithZoneId(1947, 12, 17, 15, 12, 01, 789000000, "Europe/Rome");
            var dateTime2 = new CypherDateTimeWithZoneId(new DateTime(1947, 12, 17, 15, 12, 01, 790), "Europe/Rome");

            dateTime1.Should().NotBe(dateTime2);
        }

        [Fact]
        public void ShouldNotBeEqualToAnotherType()
        {
            var dateTime = new CypherDateTimeWithZoneId(1947, 12, 17, 15, 12, 01, 789000000, "Europe/Rome");
            var other = "some string";

            dateTime.Equals(other).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToNull()
        {
            var dateTime = new CypherDateTimeWithZoneId(1947, 12, 17, 15, 12, 01, 789000000, "Europe/Rome");
            var other = (object)null;

            dateTime.Equals(other).Should().BeFalse();
        }
    }
}
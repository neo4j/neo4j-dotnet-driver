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
using System.Collections;
using FluentAssertions;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.Types
{
    public class CypherTimeWithOffsetTests
    {

        [Fact]
        public void ShouldCreateTimeWithOffsetWithTimeComponents()
        {
            var cypherTime = new CypherTimeWithOffset(13, 15, 59, 1500);

            cypherTime.Time.Should().Be(new TimeSpan(13, 15, 59));
            cypherTime.Offset.Should().Be(TimeSpan.FromSeconds(1500));
        }

        [Fact]
        public void ShouldCreateTimeWithOffsetWithTimeSpan()
        {
            var time = new TimeSpan(0, 13, 59, 59, 255);
            var cypherTime = new CypherTimeWithOffset(time, TimeSpan.FromSeconds(1500));

            cypherTime.Time.Should().Be(time);
            cypherTime.Offset.Should().Be(TimeSpan.FromSeconds(1500));
        }

        [Fact]
        public void ShouldCreateTimeWithOffsetWithDateTime()
        {
            var time = new DateTime(1, 1, 1, 13, 59, 59, 25);
            var cypherTime = new CypherTimeWithOffset(time, TimeSpan.FromSeconds(1500));

            cypherTime.Time.Should().Be(time.TimeOfDay);
            cypherTime.Offset.Should().Be(TimeSpan.FromSeconds(1500));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(24)]
        public void ShouldThrowOnInvalidHour(int hour)
        {
            var ex = Record.Exception(() => new CypherTimeWithOffset(hour, 0, 0, 0));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(60)]
        [InlineData(61)]
        public void ShouldThrowOnInvalidMinute(int minute)
        {
            var ex = Record.Exception(() => new CypherTimeWithOffset(0, minute, 0, 0));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(60)]
        [InlineData(61)]
        public void ShouldThrowOnInvalidSecond(int second)
        {
            var ex = Record.Exception(() => new CypherTimeWithOffset(0, 0, second, 0));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(999_999_999 + 1)]
        public void ShouldThrowOnInvalidNanosecond(int nanosecond)
        {
            var ex = Record.Exception(() => new CypherTimeWithOffset(0, 0, 0, nanosecond, 0));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(-64801)]
        [InlineData(64801)]
        public void ShouldThrowOnInvalidOffset(int offset)
        {
            var ex = Record.Exception(() => new CypherTimeWithOffset(0, 0, 0, 0, offset));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
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
            var time = new CypherTimeWithOffset(0, 0, 0, nanosecond, 0);
            var ex = Record.Exception(() => time.Time);

            ex.Should().NotBeNull().And.BeOfType<ValueTruncationException>();
        }

        [Theory]
        [InlineData(13, 15, 59, 274000000, 1500, "13:15:59.274000000+00:25")]
        [InlineData(0, 1, 2, 000000000, 1501, "00:01:02.000000000+00:25:01")]
        [InlineData(0, 1, 2, 000000000, -1501, "00:01:02.000000000-00:25:01")]
        [InlineData(0, 1, 2, 750000000, 10800, "00:01:02.750000000+03:00")]
        [InlineData(0, 1, 2, 750000000, 10805, "00:01:02.750000000+03:00:05")]
        [InlineData(0, 1, 2, 750000000, 10795, "00:01:02.750000000+02:59:55")]
        [InlineData(0, 1, 2, 750000000, 0, "00:01:02.750000000Z")]
        public void ShouldGenerateCorrectString(int hour, int minute, int second, int nanosecond, int offsetSeconds, string expected)
        {
            var cypherTime = new CypherTimeWithOffset(hour, minute, second, nanosecond, offsetSeconds);
            var cypherTimeStr = cypherTime.ToString();

            cypherTimeStr.Should().Be(expected);
        }

        [Fact]
        public void ShouldGenerateSameHashcode()
        {
            var time1 = new CypherTimeWithOffset(12, 49, 55, 123000000, 1500);
            var time2 = new CypherTimeWithOffset(new DateTime(2017, 1, 1, 12, 49, 55, 123), TimeSpan.FromSeconds(1500));
            var time3 = new CypherTimeWithOffset(new TimeSpan(0, 12, 49, 55, 123), TimeSpan.FromSeconds(1500));

            time1.GetHashCode().Should().Be(time2.GetHashCode()).And.Be(time3.GetHashCode());
        }

        [Fact]
        public void ShouldGenerateDifferentHashcode()
        {
            var time1 = new CypherTimeWithOffset(12, 49, 55, 123000000, 1500);
            var time2 = new CypherTimeWithOffset(new DateTime(2017, 1, 1, 12, 49, 55, 123), TimeSpan.FromSeconds(1800));
            var time3 = new CypherTimeWithOffset(new TimeSpan(0, 12, 49, 55, 125), TimeSpan.FromSeconds(1500));

            time1.GetHashCode().Should().NotBe(time2.GetHashCode()).And.NotBe(time3.GetHashCode());
        }

        [Fact]
        public void ShouldBeEqual()
        {
            var time1 = new CypherTimeWithOffset(12, 49, 55, 123000000, 1500);
            var time2 = new CypherTimeWithOffset(new DateTime(2017, 1, 1, 12, 49, 55, 123), TimeSpan.FromSeconds(1500));
            var time3 = new CypherTimeWithOffset(new TimeSpan(0, 12, 49, 55, 123), TimeSpan.FromSeconds(1500));

            time1.Equals(time2).Should().BeTrue();
            time1.Equals(time3).Should().BeTrue();
        }

        [Fact]
        public void ShouldNotBeEqual()
        {
            var time1 = new CypherTimeWithOffset(12, 49, 55, 123000000, 1800);
            var time2 = new CypherTimeWithOffset(new DateTime(2017, 1, 1, 12, 49, 55, 123), TimeSpan.FromSeconds(1200));
            var time3 = new CypherTimeWithOffset(new TimeSpan(0, 12, 49, 55, 125), TimeSpan.FromSeconds(1500));

            time1.Equals(time2).Should().BeFalse();
            time1.Equals(time3).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToAnotherType()
        {
            var time = new CypherTimeWithOffset(12, 49, 55, 123000000, 1800);
            var other = "some string";

            time.Equals(other).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToNull()
        {
            var time = new CypherTimeWithOffset(12, 49, 55, 123000000, 1800);
            var other = (object)null;

            time.Equals(other).Should().BeFalse();
        }

        [Fact]
        public void ShouldThrowOnCompareToOtherType()
        {
            var time1 = new CypherTimeWithOffset(0, 0, 0, 0, 1800);

            var ex = Record.Exception(() => time1.CompareTo(new DateTime(1947, 12, 17)));

            ex.Should().NotBeNull().And.BeOfType<ArgumentException>();
        }

        [Fact]
        public void ShouldReportLargerOnCompareToNull()
        {
            var time1 = new CypherTimeWithOffset(0, 0, 0, 0, 1800);

            var comp = time1.CompareTo(null);

            comp.Should().BeGreaterThan(0);
        }

        [Fact]
        public void ShouldReportLargerOnCompareTo()
        {
            var time1 = new CypherTimeWithOffset(0, 0, 0, 0, 1800);
            var time2 = new CypherTimeWithOffset(23, 59, 59, 999999999, 1800);

            var comp = time1.CompareTo(time2);

            comp.Should().BeGreaterThan(0);
        }

        [Fact]
        public void ShouldReportLargerOnCompareToDiffOffset()
        {
            var time1 = new CypherTimeWithOffset(23, 59, 59, 999999999, 1700);
            var time2 = new CypherTimeWithOffset(23, 59, 59, 999999999, 1750);

            var comp = time1.CompareTo(time2);

            comp.Should().BeGreaterThan(0);
        }

        [Fact]
        public void ShouldReportEqualOnCompareTo()
        {
            var time1 = new CypherTimeWithOffset(23, 59, 59, 999999999, 1800);
            var time2 = new CypherTimeWithOffset(23, 59, 59, 999999999, 1800);

            var comp = time1.CompareTo(time2);

            comp.Should().Be(0);
        }

        [Fact]
        public void ShouldReportEqualOnCompareToDiffOffset()
        {
            var time1 = new CypherTimeWithOffset(23, 59, 59, 999999999, 1800);
            var time2 = new CypherTimeWithOffset(0, 59, 59, 999999999, 5400);

            var comp = time1.CompareTo(time2);

            comp.Should().Be(0);
        }

        [Fact]
        public void ShouldReportSmallerOnCompareTo()
        {
            var time1 = new CypherTimeWithOffset(0, 59, 59, 999999999, 1800);
            var time2 = new CypherTimeWithOffset(23, 59, 59, 999999999, 1800);

            var comp = time1.CompareTo(time2);

            comp.Should().BeLessThan(0);
        }

        [Fact]
        public void ShouldReportSmallerOnCompareToDiffOffset()
        {
            var time1 = new CypherTimeWithOffset(23, 59, 59, 999999999, 1800);
            var time2 = new CypherTimeWithOffset(23, 59, 59, 999999999, -1799);

            var comp = time1.CompareTo(time2);

            comp.Should().BeLessThan(0);
        }

        [Fact]
        public void ShouldBeConvertableToString()
        {
            var time = new CypherTimeWithOffset(12, 15, 59, 660000999, 1800);
            var timeStr1 = Convert.ToString(time);
            var timeStr2 = Convert.ChangeType(time, typeof(string));

            timeStr1.Should().Be("12:15:59.660000999+00:30");
            timeStr2.Should().Be("12:15:59.660000999+00:30");
        }

        [Fact]
        public void ShouldThrowWhenConversionIsNotSupported()
        {
            var time = new CypherTimeWithOffset(12, 15, 59, 660000999, 1800);
            var conversions = new Action[]
            {
                () => Convert.ToDateTime(time),
                () => Convert.ToBoolean(time),
                () => Convert.ToByte(time),
                () => Convert.ToChar(time),
                () => Convert.ToDecimal(time),
                () => Convert.ToDouble(time),
                () => Convert.ToInt16(time),
                () => Convert.ToInt32(time),
                () => Convert.ToInt64(time),
                () => Convert.ToSByte(time),
                () => Convert.ToUInt16(time),
                () => Convert.ToUInt32(time),
                () => Convert.ToUInt64(time),
                () => Convert.ToSingle(time),
                () => Convert.ChangeType(time, typeof(ArrayList))
            };

            foreach (var testAction in conversions)
            {
                testAction.ShouldThrow<InvalidCastException>();
            }
        }
    }
}
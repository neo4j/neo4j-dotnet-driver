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
using System.Collections;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Tests.Types;

public class LocalTimeTests
{
    [Fact]
    public void ShouldCreateTimeWithTimeComponents()
    {
        var cypherTime = new LocalTime(13, 15, 59);

        cypherTime.ToTimeSpan().Should().Be(new TimeSpan(13, 15, 59));
    }

    [Fact]
    public void ShouldCreateTimeWithTimeSpan()
    {
        var time = new TimeSpan(0, 13, 59, 59, 255);
        var cypherTime = new LocalTime(time);

        cypherTime.ToTimeSpan().Should().Be(time);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void ShouldCreateTimeWithTimeOnly()
    {
        var time = new TimeOnly(13, 59, 59, 255);
        var cypherTime = new LocalTime(time);

        cypherTime.ToTimeOnly().Should().Be(time);
    }
#endif

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void ShouldThrowOnInvalidHour(int hour)
    {
        var ex = Record.Exception(() => new LocalTime(hour, 0, 0, 0));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(60)]
    [InlineData(61)]
    public void ShouldThrowOnInvalidMinute(int minute)
    {
        var ex = Record.Exception(() => new LocalTime(0, minute, 0, 0));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(60)]
    [InlineData(61)]
    public void ShouldThrowOnInvalidSecond(int second)
    {
        var ex = Record.Exception(() => new LocalTime(0, 0, second, 0));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999_999_999 + 1)]
    public void ShouldThrowOnInvalidNanosecond(int nanosecond)
    {
        var ex = Record.Exception(() => new LocalTime(0, 0, 0, nanosecond));

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
        var time = new LocalTime(0, 0, 0, nanosecond);
        var ex = Record.Exception(() => time.ToTimeSpan());

        ex.Should().NotBeNull().And.BeOfType<ValueTruncationException>();
    }

#if NET6_0_OR_GREATER
    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(99)]
    [InlineData(999000727)]
    [InlineData(999000750)]
    [InlineData(999000001)]
    public void ShouldThrowOnTimeOnlyTruncation(int nanosecond)
    {
        var time = new LocalTime(0, 0, 0, nanosecond);
        var ex = Record.Exception(() => time.ToTimeOnly());

        ex.Should().NotBeNull().And.BeOfType<ValueTruncationException>();
    }
#endif

    [Theory]
    [InlineData(13, 15, 59, 274000000, "13:15:59.274000000")]
    [InlineData(0, 1, 2, 000000000, "00:01:02")]
    [InlineData(0, 1, 2, 5001, "00:01:02.000005001")]
    public void ShouldGenerateCorrectString(int hour, int minute, int second, int nanosecond, string expected)
    {
        var cypherTime = new LocalTime(hour, minute, second, nanosecond);
        var cypherTimeStr = cypherTime.ToString();

        cypherTimeStr.Should().Be(expected);
    }

    [Fact]
    public void ShouldGenerateSameHashcode()
    {
        var time1 = new LocalTime(12, 49, 55, 123000000);
        var time2 = new LocalTime(new DateTime(2017, 1, 1, 12, 49, 55, 123));
        var time3 = new LocalTime(new TimeSpan(0, 12, 49, 55, 123));

        time1.GetHashCode().Should().Be(time2.GetHashCode()).And.Be(time3.GetHashCode());
    }

    [Fact]
    public void ShouldGenerateDifferentHashcode()
    {
        var time1 = new LocalTime(12, 49, 55, 123000001);
        var time2 = new LocalTime(new DateTime(2017, 1, 1, 12, 49, 55, 123));
        var time3 = new LocalTime(new TimeSpan(0, 12, 49, 55, 124));

        time1.GetHashCode().Should().NotBe(time2.GetHashCode()).And.NotBe(time3.GetHashCode());
    }

    [Fact]
    public void ShouldBeEqual()
    {
        var time1 = new LocalTime(12, 49, 55, 123000000);
        var time2 = new LocalTime(new DateTime(2017, 1, 1, 12, 49, 55, 123));
        var time3 = new LocalTime(new TimeSpan(0, 12, 49, 55, 123));

        time1.Equals(time2).Should().BeTrue();
        time1.Equals(time3).Should().BeTrue();
    }

    [Fact]
    public void ShouldNotBeEqual()
    {
        var time1 = new LocalTime(12, 49, 55, 123000001);
        var time2 = new LocalTime(new DateTime(2017, 1, 1, 12, 49, 55, 123));
        var time3 = new LocalTime(new TimeSpan(0, 12, 49, 55, 125));

        time1.Equals(time2).Should().BeFalse();
        time1.Equals(time3).Should().BeFalse();
    }

    [Fact]
    public void ShouldNotBeEqualToAnotherType()
    {
        var time = new LocalTime(12, 49, 55, 123000001);
        var other = "some string";

        time.Equals(other).Should().BeFalse();
    }

    [Fact]
    public void ShouldNotBeEqualToNull()
    {
        var time = new LocalTime(12, 49, 55, 123000001);
        var other = (object)null;

        time.Equals(other).Should().BeFalse();
    }

    [Fact]
    public void ShouldThrowOnCompareToOtherType()
    {
        var time1 = new LocalTime(0, 0, 0, 0);

        var ex = Record.Exception(() => time1.CompareTo(new DateTime(1947, 12, 17)));

        ex.Should().NotBeNull().And.BeOfType<ArgumentException>();
    }

    [Fact]
    public void ShouldReportLargerOnCompareToNull()
    {
        var time1 = new LocalTime(0, 0, 0, 0);

        var comp = time1.CompareTo(null);

        comp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldReportLargerOnCompareTo()
    {
        var time1 = new LocalTime(23, 59, 59, 999999999);
        var time2 = new LocalTime(23, 59, 59, 0);

        var comp = time1.CompareTo(time2);

        comp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldReportEqualOnCompareTo()
    {
        var time1 = new LocalTime(23, 59, 59, 999999999);
        var time2 = new LocalTime(23, 59, 59, 999999999);

        var comp = time1.CompareTo(time2);

        comp.Should().Be(0);
    }

    [Fact]
    public void ShouldReportSmallerOnCompareTo()
    {
        var time1 = new LocalTime(0, 59, 59, 999999999);
        var time2 = new LocalTime(23, 59, 59, 999999999);

        var comp = time1.CompareTo(time2);

        comp.Should().BeLessThan(0);
    }

    [Fact]
    public void ShouldBeConvertableToDateTime()
    {
        var time = new TimeSpan(0, 12, 15, 59, 660);
        var time1 = new LocalTime(DateTime.Today.Add(time));
        var time2 = Convert.ToDateTime(time1);
        var time3 = (DateTime)Convert.ChangeType(time1, typeof(DateTime));

        time2.TimeOfDay.Should().Be(time);
        time3.TimeOfDay.Should().Be(time);
    }

    [Fact]
    public void ShouldBeConvertableToTimeSpan()
    {
        var time = new TimeSpan(0, 12, 15, 59, 660);
        var date = DateTime.Today.Add(time);
        var time1 = new LocalTime(date);
        var time2 = (TimeSpan)Convert.ChangeType(time1, typeof(TimeSpan));

        time2.Should().Be(time);
    }

    [Fact]
    public void ShouldBeConvertableToString()
    {
        var time = new LocalTime(12, 15, 59, 660000999);
        var timeStr1 = Convert.ToString(time);
        var timeStr2 = Convert.ChangeType(time, typeof(string));

        timeStr1.Should().Be("12:15:59.660000999");
        timeStr2.Should().Be("12:15:59.660000999");
    }

    [Fact]
    public void ShouldThrowWhenConversionIsNotSupported()
    {
        var time = new LocalTime(12, 15, 59, 660000999);
        var conversions = new Action[]
        {
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
            testAction.Should().Throw<InvalidCastException>();
        }
    }
}

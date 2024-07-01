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

public class LocalDateTimeTests
{
    [Fact]
    public void ShouldCreateDateTimeWithDateTimeComponents()
    {
        var cypherDateTime = new LocalDateTime(1947, 12, 17, 23, 49, 54);

        cypherDateTime.ToDateTime().Should().Be(new DateTime(1947, 12, 17, 23, 49, 54));
    }

    [Fact]
    public void ShouldCreateDateTimeWithDateTimeComponentsWithNanoseconds()
    {
        var cypherDateTime = new LocalDateTime(1947, 12, 17, 23, 49, 54, 192794500);

        cypherDateTime.ToDateTime().Should().Be(new DateTime(1947, 12, 17, 23, 49, 54).AddTicks(1927945));
    }

    [Fact]
    public void ShouldCreateDateTimeWithDateTime()
    {
        var dateTime = new DateTime(1947, 12, 17, 23, 49, 54, 120, DateTimeKind.Local);
        var cypherDateTime = new LocalDateTime(dateTime);

        cypherDateTime.ToDateTime().Should().Be(dateTime);
    }

    [Theory]
    [InlineData(-1000000000)]
    [InlineData(1000000000)]
    public void ShouldThrowOnInvalidYear(int year)
    {
        var ex = Record.Exception(() => new LocalDateTime(year, 1, 1, 0, 0, 0));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void ShouldThrowOnInvalidMonth(int month)
    {
        var ex = Record.Exception(() => new LocalDateTime(1990, month, 1, 0, 0, 0));

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
        var ex = Record.Exception(() => new LocalDateTime(year, month, day, 0, 0, 0));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void ShouldThrowOnInvalidHour(int hour)
    {
        var ex = Record.Exception(() => new LocalDateTime(1990, 1, 1, hour, 0, 0));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(60)]
    [InlineData(61)]
    public void ShouldThrowOnInvalidMinute(int minute)
    {
        var ex = Record.Exception(() => new LocalDateTime(1990, 1, 1, 0, minute, 0));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(60)]
    [InlineData(61)]
    public void ShouldThrowOnInvalidSecond(int second)
    {
        var ex = Record.Exception(() => new LocalDateTime(1990, 1, 1, 0, 0, second));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999_999_999 + 1)]
    public void ShouldThrowOnInvalidNanosecond(int nanosecond)
    {
        var ex = Record.Exception(() => new LocalDateTime(1990, 1, 1, 0, 0, 0, nanosecond));

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
        var dateTime = new LocalDateTime(year, 1, 1, 0, 0, 0, 0);
        var ex = Record.Exception(() => dateTime.ToDateTime());

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
        var dateTime = new LocalDateTime(1, 1, 1, 0, 0, 0, nanosecond);
        var ex = Record.Exception(() => dateTime.ToDateTime());

        ex.Should().NotBeNull().And.BeOfType<ValueTruncationException>();
    }

    [Theory]
    [InlineData(1947, 12, 17, 23, 5, 54, 192794500, "1947-12-17T23:05:54.192794500")]
    [InlineData(1947, 12, 5, 0, 5, 54, 192794500, "1947-12-05T00:05:54.192794500")]
    [InlineData(1947, 12, 5, 0, 5, 54, 0, "1947-12-05T00:05:54")]
    [InlineData(5, 1, 5, 0, 5, 54, 0, "0005-01-05T00:05:54")]
    [InlineData(-5, 1, 5, 0, 5, 54, 1250, "-0005-01-05T00:05:54.000001250")]
    [InlineData(999999, 1, 1, 5, 1, 25, 1, "999999-01-01T05:01:25.000000001")]
    [InlineData(-999999, 1, 1, 5, 1, 25, 1, "-999999-01-01T05:01:25.000000001")]
    public void ShouldGenerateCorrectString(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int nanosecond,
        string expected)
    {
        var cypherDateTime = new LocalDateTime(year, month, day, hour, minute, second, nanosecond);
        var cypherDateTimeStr = cypherDateTime.ToString();

        cypherDateTimeStr.Should().Be(expected);
    }

    [Fact]
    public void ShouldGenerateSameHashcode()
    {
        var dateTime1 = new LocalDateTime(1947, 12, 17, 15, 12, 01, 789000000);
        var dateTime2 = new LocalDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 789, DateTimeKind.Local));

        dateTime1.GetHashCode().Should().Be(dateTime2.GetHashCode());
    }

    [Fact]
    public void ShouldGenerateDifferentHashcode()
    {
        var dateTime1 = new LocalDateTime(1947, 12, 17, 15, 12, 01, 789000000);
        var dateTime2 = new LocalDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 790));

        dateTime1.GetHashCode().Should().NotBe(dateTime2.GetHashCode());
    }

    [Fact]
    public void ShouldBeEqual()
    {
        var dateTime1 = new LocalDateTime(1947, 12, 17, 15, 12, 01, 789000000);
        var dateTime2 = new LocalDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 789, DateTimeKind.Local));

        dateTime1.Equals(dateTime2).Should().BeTrue();
    }

    [Fact]
    public void ShouldNotBeEqual()
    {
        var dateTime1 = new LocalDateTime(1947, 12, 17, 15, 12, 01, 789000000);
        var dateTime2 = new LocalDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 788));

        dateTime1.Equals(dateTime2).Should().BeFalse();
    }

    [Fact]
    public void ShouldNotBeEqualToAnotherType()
    {
        var dateTime = new LocalDateTime(1947, 12, 17, 15, 12, 01, 789000000);
        var other = "some string";

        dateTime.Equals(other).Should().BeFalse();
    }

    [Fact]
    public void ShouldNotBeEqualToNull()
    {
        var dateTime = new LocalDateTime(1947, 12, 17, 15, 12, 01, 789000000);
        var other = (object)null;

        dateTime.Equals(other).Should().BeFalse();
    }

    [Fact]
    public void ShouldThrowOnCompareToOtherType()
    {
        var dateTime1 = new LocalDateTime(1947, 12, 17, 0, 0, 0, 0);

        var ex = Record.Exception(() => dateTime1.CompareTo(new DateTime(1947, 12, 17)));

        ex.Should().NotBeNull().And.BeOfType<ArgumentException>();
    }

    [Fact]
    public void ShouldReportLargerOnCompareToNull()
    {
        var dateTime1 = new LocalDateTime(1947, 12, 17, 0, 0, 0, 0);

        var comp = dateTime1.CompareTo(null);

        comp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldReportLargerOnCompareTo()
    {
        var dateTime1 = new LocalDateTime(1947, 12, 17, 0, 0, 0, 0);
        var dateTime2 = new LocalDateTime(1947, 12, 16, 23, 59, 59, 999999999);

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldReportEqualOnCompareTo()
    {
        var dateTime1 = new LocalDateTime(1947, 12, 16, 23, 59, 59, 999999999);
        var dateTime2 = new LocalDateTime(1947, 12, 16, 23, 59, 59, 999999999);

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().Be(0);
    }

    [Fact]
    public void ShouldReportSmallerOnCompareTo()
    {
        var dateTime1 = new LocalDateTime(1947, 12, 16, 23, 59, 59, 999999999);
        var dateTime2 = new LocalDateTime(1947, 12, 17, 0, 59, 59, 999999999);

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().BeLessThan(0);
    }

    [Fact]
    public void ShouldBeConvertableToDateTime()
    {
        var date = new DateTime(1947, 12, 16, 12, 15, 59, 660);
        var date1 = new LocalDateTime(date);
        var date2 = Convert.ToDateTime(date1);
        var date3 = Convert.ChangeType(date1, typeof(DateTime));

        date2.Should().Be(date);
        date3.Should().Be(date);
    }

    [Fact]
    public void ShouldBeConvertableToString()
    {
        var date = new LocalDateTime(1947, 12, 16, 12, 15, 59, 660000999);
        var dateStr1 = Convert.ToString(date);
        var dateStr2 = Convert.ChangeType(date, typeof(string));

        dateStr1.Should().Be("1947-12-16T12:15:59.660000999");
        dateStr2.Should().Be("1947-12-16T12:15:59.660000999");
    }

    [Fact]
    public void ShouldThrowWhenConversionIsNotSupported()
    {
        var date = new LocalDateTime(1947, 12, 16, 12, 15, 59, 660000999);
        var conversions = new Action[]
        {
            () => Convert.ToBoolean(date),
            () => Convert.ToByte(date),
            () => Convert.ToChar(date),
            () => Convert.ToDecimal(date),
            () => Convert.ToDouble(date),
            () => Convert.ToInt16(date),
            () => Convert.ToInt32(date),
            () => Convert.ToInt64(date),
            () => Convert.ToSByte(date),
            () => Convert.ToUInt16(date),
            () => Convert.ToUInt32(date),
            () => Convert.ToUInt64(date),
            () => Convert.ToSingle(date),
            () => Convert.ChangeType(date, typeof(ArrayList))
        };

        foreach (var testAction in conversions)
        {
            testAction.Should().Throw<InvalidCastException>();
        }
    }
}

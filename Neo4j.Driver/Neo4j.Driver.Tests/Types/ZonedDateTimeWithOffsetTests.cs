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
using Neo4j.Driver.Internal.Helpers;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete - but we still test obsolete members

namespace Neo4j.Driver.Tests.Types;

public class ZonedDateTimeWithOffsetTests
{
    [Fact]
    public void ShouldCreateDateTimeWithOffsetWithDateTimeComponents()
    {
        var cypherDateTime = new ZonedDateTime(1947, 12, 17, 23, 49, 54, Zone.Of(1500));

        cypherDateTime.Year.Should().Be(1947);
        cypherDateTime.Month.Should().Be(12);
        cypherDateTime.Day.Should().Be(17);
        cypherDateTime.Hour.Should().Be(23);
        cypherDateTime.Minute.Should().Be(49);
        cypherDateTime.Second.Should().Be(54);
        cypherDateTime.OffsetSeconds.Should().Be(1500);
    }

    [Fact]
    public void ShouldCreateDateTimeWithOffsetWithDateTimeComponentsWithNanoseconds()
    {
        var cypherDateTime = new ZonedDateTime(1947, 12, 17, 23, 49, 54, 192794500, Zone.Of(1500));

        cypherDateTime.Year.Should().Be(1947);
        cypherDateTime.Month.Should().Be(12);
        cypherDateTime.Day.Should().Be(17);
        cypherDateTime.Hour.Should().Be(23);
        cypherDateTime.Minute.Should().Be(49);
        cypherDateTime.Second.Should().Be(54);
        cypherDateTime.Nanosecond.Should().Be(192794500);
        cypherDateTime.OffsetSeconds.Should().Be(1500);
    }

    [Fact]
    public void ShouldCreateDateTimeWithOffsetWithDateTime()
    {
        var dateTime = new DateTime(1947, 12, 17, 23, 49, 54, 120);
        var cypherDateTime = new ZonedDateTime(dateTime, TimeSpan.FromSeconds(1500));

        cypherDateTime.Year.Should().Be(1947);
        cypherDateTime.Month.Should().Be(12);
        cypherDateTime.Day.Should().Be(17);
        cypherDateTime.Hour.Should().Be(23);
        cypherDateTime.Minute.Should().Be(49);
        cypherDateTime.Second.Should().Be(54);
        cypherDateTime.Nanosecond.Should().Be(120000000);
        cypherDateTime.OffsetSeconds.Should().Be(1500);
    }

    [Fact]
    public void ShouldCreateDateTimeWithOffsetWithDateTimeOffset()
    {
        var dateTime = new DateTimeOffset(1947, 12, 17, 23, 49, 54, 120, TimeSpan.FromSeconds(1500));
        var cypherDateTime = new ZonedDateTime(dateTime);

        cypherDateTime.Year.Should().Be(1947);
        cypherDateTime.Month.Should().Be(12);
        cypherDateTime.Day.Should().Be(17);
        cypherDateTime.Hour.Should().Be(23);
        cypherDateTime.Minute.Should().Be(49);
        cypherDateTime.Second.Should().Be(54);
        cypherDateTime.Nanosecond.Should().Be(120000000);
        cypherDateTime.OffsetSeconds.Should().Be(1500);
    }

    [Theory]
    [InlineData(-1000000000)]
    [InlineData(1000000000)]
    public void ShouldThrowOnInvalidYear(int year)
    {
        var ex = Record.Exception(() => new ZonedDateTime(year, 1, 1, 0, 0, 0, Zone.Of(0)));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void ShouldThrowOnInvalidMonth(int month)
    {
        var ex = Record.Exception(() => new ZonedDateTime(1990, month, 1, 0, 0, 0, Zone.Of(0)));

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
        var ex = Record.Exception(() => new ZonedDateTime(year, month, day, 0, 0, 0, Zone.Of(0)));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void ShouldThrowOnInvalidHour(int hour)
    {
        var ex = Record.Exception(() => new ZonedDateTime(1990, 1, 1, hour, 0, 0, Zone.Of(0)));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(60)]
    [InlineData(61)]
    public void ShouldThrowOnInvalidMinute(int minute)
    {
        var ex = Record.Exception(() => new ZonedDateTime(1990, 1, 1, 0, minute, 0, Zone.Of(0)));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(60)]
    [InlineData(61)]
    public void ShouldThrowOnInvalidSecond(int second)
    {
        var ex = Record.Exception(() => new ZonedDateTime(1990, 1, 1, 0, 0, second, Zone.Of(0)));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999_999_999 + 1)]
    public void ShouldThrowOnInvalidNanosecond(int nanosecond)
    {
        var ex = Record.Exception(() => new ZonedDateTime(1990, 1, 1, 0, 0, 0, nanosecond, Zone.Of(0)));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-64801)]
    [InlineData(64801)]
    public void ShouldThrowOnInvalidOffset(int offset)
    {
        var ex = Record.Exception(() => new ZonedDateTime(1990, 1, 1, 0, 0, 0, 0, Zone.Of(offset)));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ShouldConvertToDateTimeOffset()
    {
        var dateTime = new DateTimeOffset(1947, 12, 17, 23, 49, 54, 120, TimeSpan.FromSeconds(1500));
        var cypherDateTime = new ZonedDateTime(dateTime);

        cypherDateTime.ToDateTimeOffset().Should().Be(dateTime);
    }

    [Theory]
    [InlineData(-9999)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(10000)]
    [InlineData(9999999)]
    public void ShouldThrowOnOverflow(int year)
    {
        var dateTime = new ZonedDateTime(year, 1, 1, 0, 0, 0, 0, Zone.Of(0));
        var ex = Record.Exception(() => dateTime.ToDateTimeOffset());

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
        var dateTime = new ZonedDateTime(1, 1, 1, 0, 0, 0, nanosecond, Zone.Of(0));
        var ex = Record.Exception(() => dateTime.ToDateTimeOffset());

        ex.Should().NotBeNull().And.BeOfType<ValueTruncationException>();
    }

    [Theory]
    [InlineData(1947, 12, 17, 23, 5, 54, 192794500, 1500, "1947-12-17T23:05:54.192794500+00:25")]
    [InlineData(1947, 12, 5, 0, 5, 54, 192794500, -1500, "1947-12-05T00:05:54.192794500-00:25")]
    [InlineData(1947, 12, 17, 23, 5, 54, 192794500, 1501, "1947-12-17T23:05:54.192794500+00:25:01")]
    [InlineData(1947, 12, 5, 0, 5, 54, 192794500, -1499, "1947-12-05T00:05:54.192794500-00:24:59")]
    [InlineData(1947, 12, 5, 0, 5, 54, 0, 1800, "1947-12-05T00:05:54+00:30")]
    [InlineData(5, 1, 5, 0, 5, 54, 0, -1800, "0005-01-05T00:05:54-00:30")]
    [InlineData(-5, 1, 5, 0, 5, 54, 1250, 0, "-0005-01-05T00:05:54.000001250Z")]
    [InlineData(999999, 1, 1, 5, 1, 25, 1, 64800, "999999-01-01T05:01:25.000000001+18:00")]
    [InlineData(-999999, 1, 1, 5, 1, 25, 1, -60000, "-999999-01-01T05:01:25.000000001-16:40")]
    public void ShouldGenerateCorrectString(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int nanosecond,
        int offsetSeconds,
        string expected)
    {
        var cypherDateTime =
            new ZonedDateTime(year, month, day, hour, minute, second, nanosecond, Zone.Of(offsetSeconds));

        var cypherDateTimeStr = cypherDateTime.ToString();

        cypherDateTimeStr.Should().Be(expected);
    }

    [Fact]
    public void ShouldGenerateSameHashcode()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 15, 12, 01, 789000000, Zone.Of(1800));
        var dateTime2 = new ZonedDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 789), 1800);

        dateTime1.GetHashCode().Should().Be(dateTime2.GetHashCode());
    }

    [Fact]
    public void ShouldGenerateDifferentHashcode()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 15, 12, 01, 789000000, Zone.Of(1801));
        var dateTime2 = new ZonedDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 790), 1800);

        dateTime1.GetHashCode().Should().NotBe(dateTime2.GetHashCode());
    }

    [Fact]
    public void ShouldBeEqual()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 15, 12, 01, 789000000, Zone.Of(1800));
        var dateTime2 = new ZonedDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 789), 1800);

        dateTime1.Should().Be(dateTime2);
    }

    [Fact]
    public void ShouldNotBeEqual()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 15, 12, 01, 789000000, Zone.Of(1801));
        var dateTime2 = new ZonedDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 790), 1800);

        dateTime1.Should().NotBe(dateTime2);
    }

    [Fact]
    public void ShouldNotBeEqualToAnotherType()
    {
        var dateTime = new ZonedDateTime(1947, 12, 17, 15, 12, 01, 789000000, Zone.Of(1800));
        var other = "some string";

        dateTime.Equals(other).Should().BeFalse();
    }

    [Fact]
    public void ShouldNotBeEqualToNull()
    {
        var dateTime = new ZonedDateTime(1947, 12, 17, 15, 12, 01, 789000000, Zone.Of(1800));
        var other = (object)null;

        dateTime.Equals(other).Should().BeFalse();
    }

    [Fact]
    public void ShouldThrowOnCompareToOtherType()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 0, 0, 0, 0, Zone.Of(1800));

        var ex = Record.Exception(() => dateTime1.CompareTo(new DateTime(1947, 12, 17)));

        ex.Should().NotBeNull().And.BeOfType<ArgumentException>();
    }

    [Fact]
    public void ShouldReportLargerOnCompareToNull()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 0, 0, 0, 0, Zone.Of(1800));

        var comp = dateTime1.CompareTo(null);

        comp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldReportLargerOnCompareTo()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 0, 0, 0, 0, Zone.Of(1800));
        var dateTime2 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999999, Zone.Of(1800));

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldReportLargerOnCompareToDiffOffset()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 23, 59, 59, 999999999, Zone.Of(1800));
        var dateTime2 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999999, Zone.Of(1750));

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldReportEqualOnCompareTo()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999999, Zone.Of(1800));
        var dateTime2 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999999, Zone.Of(1800));

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().Be(0);
    }

    [Fact]
    public void ShouldReportEqualOnCompareToDiffOffset()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999999, Zone.Of(1800));
        var dateTime2 = new ZonedDateTime(1947, 12, 17, 0, 59, 59, 999999999, Zone.Of(5400));

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().Be(0);
    }

    [Fact]
    public void ShouldReportSmallerOnCompareTo()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999999, Zone.Of(1800));
        var dateTime2 = new ZonedDateTime(1947, 12, 17, 0, 59, 59, 999999999, Zone.Of(1800));

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().BeLessThan(0);
    }

    [Fact]
    public void ShouldReportSmallerOnCompareToDiffOffset()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999999, Zone.Of(1800));
        var dateTime2 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999999, Zone.Of(-1799));

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().BeLessThan(0);
    }

    [Fact]
    public void ShouldBeConvertableToDateTimeOffset()
    {
        var date = new DateTime(1947, 12, 16, 12, 15, 59, 660);
        var date1 = new ZonedDateTime(date, 3600);
        var date2 = Convert.ChangeType(date1, typeof(DateTimeOffset));

        date2.Should().Be(new DateTimeOffset(date, TimeSpan.FromSeconds(3600)));
    }

    [Fact]
    public void ShouldBeConvertableToString()
    {
        var date = new ZonedDateTime(1947, 12, 16, 12, 15, 59, 660000999, Zone.Of(3600));
        var dateStr1 = Convert.ToString(date);
        var dateStr2 = Convert.ChangeType(date, typeof(string));

        dateStr1.Should().Be("1947-12-16T12:15:59.660000999+01:00");
        dateStr2.Should().Be("1947-12-16T12:15:59.660000999+01:00");
    }

    [Fact]
    public void ShouldThrowWhenConversionIsNotSupported()
    {
        var date = new ZonedDateTime(1947, 12, 16, 12, 15, 59, 660000999, Zone.Of(3600));
        var conversions = new Action[]
        {
            () => Convert.ToBoolean(date),
            () => Convert.ToDateTime(date),
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

    [Fact]
    public void ShouldCreateMinZonedDateTime()
    {
        var zone = new ZonedDateTime(TemporalHelpers.MinUtcForZonedDateTime, 0, Zone.Of(0));
        zone.Year.Should().Be(-999_999_999);
        zone.Month.Should().Be(1);
        zone.Day.Should().Be(1);
        zone.Hour.Should().Be(0);
        zone.Minute.Should().Be(0);
        zone.Second.Should().Be(0);
        zone.Nanosecond.Should().Be(0);
    }

    [Fact]
    public void ShouldCreateMinZonedDateTimeFromComponents()
    {
        var zone = new ZonedDateTime(-999_999_999, 1, 1, 0, 0, 0, Zone.Of(0));
        zone.UtcSeconds.Should().Be(TemporalHelpers.MinUtcForZonedDateTime);
    }

    [Fact]
    public void ShouldCreateMaxZonedDateTime()
    {
        var zone = new ZonedDateTime(TemporalHelpers.MaxUtcForZonedDateTime, 0, Zone.Of(0));
        zone.Year.Should().Be(999_999_999);
        zone.Month.Should().Be(12);
        zone.Day.Should().Be(31);
        zone.Hour.Should().Be(23);
        zone.Minute.Should().Be(59);
        zone.Second.Should().Be(59);
        zone.Nanosecond.Should().Be(0);
    }

    [Fact]
    public void ShouldCreateMaxZonedDateTimeFromComponents()
    {
        var zone = new ZonedDateTime(999_999_999, 12, 31, 23, 59, 59, Zone.Of(0));
        zone.UtcSeconds.Should().Be(TemporalHelpers.MaxUtcForZonedDateTime);
    }
}

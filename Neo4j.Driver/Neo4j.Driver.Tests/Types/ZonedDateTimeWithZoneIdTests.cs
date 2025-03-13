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

#pragma warning disable CS0618 // Type or member is obsolete - but we still test obsolete members

namespace Neo4j.Driver.Tests.Types;

public class ZonedDateTimeWithZoneIdTests
{
    public static TheoryData<Func<ZonedDateTime>> NullZoneConstructors = new()
    {
        () => new ZonedDateTime(0L, 0, null),
        () => new ZonedDateTime(0L, null),
        () => new ZonedDateTime(DateTime.Now, null),
        () => new ZonedDateTime(new LocalDateTime(DateTime.Now), null),
        () => new ZonedDateTime(2020, 12, 31, 12, 0, 0, null)
    };

    public static TheoryData<Func<ZonedDateTime>> LocalConstructorsWithUnkownZoneIds = new()
    {
        () => new ZonedDateTime(DateTime.Now, "Europe/Neo4j"),
        () => new ZonedDateTime(2020, 12, 31, 12, 0, 0, new ZoneId("Europe/Neo4j"))
    };

    [Fact]
    public void ShouldCreateDateTimeWithZoneIdWithDateTimeComponents()
    {
        var cypherDateTime = new ZonedDateTime(1947, 12, 17, 23, 49, 54, Zone.Of("Europe/Rome"));

        cypherDateTime.Year.Should().Be(1947);
        cypherDateTime.Month.Should().Be(12);
        cypherDateTime.Day.Should().Be(17);
        cypherDateTime.Hour.Should().Be(23);
        cypherDateTime.Minute.Should().Be(49);
        cypherDateTime.Second.Should().Be(54);
        cypherDateTime.Nanosecond.Should().Be(0);
        cypherDateTime.OffsetSeconds.Should().Be(60 * 60);
        cypherDateTime.Zone.Should().Be(Zone.Of("Europe/Rome"));
    }

    [Fact]
    public void ShouldCreateDateTimeWithZoneIdWithDateTimeComponentsWithNanoseconds()
    {
        var cypherDateTime = new ZonedDateTime(1947, 12, 17, 23, 49, 54, 192794500, Zone.Of("Europe/Rome"));

        cypherDateTime.Year.Should().Be(1947);
        cypherDateTime.Month.Should().Be(12);
        cypherDateTime.Day.Should().Be(17);
        cypherDateTime.Hour.Should().Be(23);
        cypherDateTime.Minute.Should().Be(49);
        cypherDateTime.Second.Should().Be(54);
        cypherDateTime.Nanosecond.Should().Be(192794500);
        cypherDateTime.OffsetSeconds.Should().Be(60 * 60);
        cypherDateTime.Zone.Should().Be(Zone.Of("Europe/Rome"));
    }

    [Fact]
    public void ShouldCreateDateTimeWithZoneIdWithDateTime()
    {
        var dateTime = new DateTime(1947, 12, 17, 23, 49, 54, 120);
        var cypherDateTime = new ZonedDateTime(dateTime, "Europe/Rome");

        cypherDateTime.Year.Should().Be(1947);
        cypherDateTime.Month.Should().Be(12);
        cypherDateTime.Day.Should().Be(17);
        cypherDateTime.Hour.Should().Be(23);
        cypherDateTime.Minute.Should().Be(49);
        cypherDateTime.Second.Should().Be(54);
        cypherDateTime.Nanosecond.Should().Be(120000000);
        cypherDateTime.OffsetSeconds.Should().Be(60 * 60);
        cypherDateTime.Zone.Should().Be(Zone.Of("Europe/Rome"));
    }

    [Theory]
    [InlineData(-1000000000)]
    [InlineData(1000000000)]
    public void ShouldThrowOnInvalidYear(int year)
    {
        var ex = Record.Exception(() => new ZonedDateTime(year, 1, 1, 0, 0, 0, Zone.Of("Europe/Amsterdam")));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void ShouldThrowOnInvalidMonth(int month)
    {
        var ex = Record.Exception(() => new ZonedDateTime(1990, month, 1, 0, 0, 0, Zone.Of("Europe/Istanbul")));

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
        var ex = Record.Exception(() => new ZonedDateTime(year, month, day, 0, 0, 0, Zone.Of("Europe/Istanbul")));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void ShouldThrowOnInvalidHour(int hour)
    {
        var ex = Record.Exception(() => new ZonedDateTime(1990, 1, 1, hour, 0, 0, Zone.Of("Europe/Istanbul")));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(60)]
    [InlineData(61)]
    public void ShouldThrowOnInvalidMinute(int minute)
    {
        var ex = Record.Exception(() => new ZonedDateTime(1990, 1, 1, 0, minute, 0, Zone.Of("Europe/Paris")));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(60)]
    [InlineData(61)]
    public void ShouldThrowOnInvalidSecond(int second)
    {
        var ex = Record.Exception(() => new ZonedDateTime(1990, 1, 1, 0, 0, second, Zone.Of("Europe/Rome")));

        ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999_999_999 + 1)]
    public void ShouldThrowOnInvalidNanosecond(int nanosecond)
    {
        var ex = Record.Exception(() => new ZonedDateTime(1990, 1, 1, 0, 0, 0, nanosecond, Zone.Of("Europe/Athens")));

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
        var dateTime = new ZonedDateTime(year, 1, 1, 0, 0, 0, 0, Zone.Of("Europe/London"));
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
        var dateTime = new ZonedDateTime(1, 1, 1, 0, 0, 0, nanosecond, Zone.Of("Europe/London"));
        var ex = Record.Exception(() => dateTime.ToDateTimeOffset());

        ex.Should().NotBeNull().And.BeOfType<ValueTruncationException>();
    }

    [Theory]
    [InlineData(1947, 12, 17, 23, 5, 54, 192794500, "Europe/Rome", "1947-12-17T23:05:54.192794500[Europe/Rome]")]
    [InlineData(
        1947,
        12,
        5,
        0,
        5,
        54,
        192794500,
        "Europe/Amsterdam",
        "1947-12-05T00:05:54.192794500[Europe/Amsterdam]")]
    [InlineData(1947, 12, 5, 0, 5, 54, 0, "Europe/Istanbul", "1947-12-05T00:05:54[Europe/Istanbul]")]
    [InlineData(5, 1, 5, 0, 5, 54, 0, "Africa/Nairobi", "0005-01-05T00:05:54[Africa/Nairobi]")]
    [InlineData(-5, 1, 5, 0, 5, 54, 1250, "America/Halifax", "-0005-01-05T00:05:54.000001250[America/Halifax]")]
    [InlineData(999999, 1, 1, 5, 1, 25, 1, "America/New_York", "999999-01-01T05:01:25.000000001[America/New_York]")]
    [InlineData(-999999, 1, 1, 5, 1, 25, 1, "Asia/Seoul", "-999999-01-01T05:01:25.000000001[Asia/Seoul]")]
    public void ShouldGenerateCorrectString(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int nanosecond,
        string zoneId,
        string expected)
    {
        var cypherDateTime = new ZonedDateTime(year, month, day, hour, minute, second, nanosecond, Zone.Of(zoneId));
        var cypherDateTimeStr = cypherDateTime.ToString();

        cypherDateTimeStr.Should().Be(expected);
    }

    [Fact]
    public void ShouldGenerateSameHashcode()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 15, 12, 01, 789000000, Zone.Of("Europe/Rome"));
        var dateTime2 = new ZonedDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 789), "Europe/Rome");

        dateTime1.GetHashCode().Should().Be(dateTime2.GetHashCode());
    }

    [Fact]
    public void ShouldGenerateDifferentHashcode()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 15, 12, 01, 789000000, Zone.Of("Europe/Rome"));
        var dateTime2 = new ZonedDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 790), "Europe/Rome");

        dateTime1.GetHashCode().Should().NotBe(dateTime2.GetHashCode());
    }

    [Fact]
    public void ShouldBeEqual()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 15, 12, 01, 789000000, Zone.Of("Europe/Rome"));
        var dateTime2 = new ZonedDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 789), "Europe/Rome");

        dateTime1.Should().Be(dateTime2);
    }

    [Fact]
    public void ShouldNotBeEqual()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 15, 12, 01, 789000000, Zone.Of("Europe/Rome"));
        var dateTime2 = new ZonedDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 790), "Europe/Rome");

        dateTime1.Should().NotBe(dateTime2);
    }

    [Fact]
    public void ShouldNotBeEqualToAnotherType()
    {
        var dateTime = new ZonedDateTime(1947, 12, 17, 15, 12, 01, 789000000, Zone.Of("Europe/Rome"));
        var other = "some string";

        dateTime.Equals(other).Should().BeFalse();
    }

    [Fact]
    public void ShouldNotBeEqualToNull()
    {
        var dateTime = new ZonedDateTime(1947, 12, 17, 15, 12, 01, 789000000, Zone.Of("Europe/Rome"));
        var other = (object)null;

        dateTime.Equals(other).Should().BeFalse();
    }

    [Fact]
    public void ShouldThrowOnCompareToOtherType()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 0, 0, 0, 0, Zone.Of("Europe/Amsterdam"));

        var ex = Record.Exception(() => dateTime1.CompareTo(new DateTime(1947, 12, 17)));

        ex.Should().NotBeNull().And.BeOfType<ArgumentException>();
    }

    [Fact]
    public void ShouldReportLargerOnCompareToNull()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 0, 0, 0, 0, Zone.Of("Europe/Amsterdam"));

        var comp = dateTime1.CompareTo(null);

        comp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldReportLargerOnCompareTo()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 0, 0, 0, 0, Zone.Of("Europe/Amsterdam"));
        var dateTime2 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999900, Zone.Of("Europe/Amsterdam"));

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldReportLargerOnCompareToDiffOffset()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 17, 23, 59, 59, 999999900, Zone.Of("Europe/Amsterdam"));
        var dateTime2 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999900, Zone.Of("Europe/London"));

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldReportEqualOnCompareTo()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999900, Zone.Of("Europe/Amsterdam"));
        var dateTime2 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999900, Zone.Of("Europe/Amsterdam"));

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().Be(0);
    }

    [Fact]
    public void ShouldReportEqualOnCompareToDiffOffset()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999900, Zone.Of("Europe/London"));
        var dateTime2 = new ZonedDateTime(1947, 12, 17, 0, 59, 59, 999999900, Zone.Of("Europe/Amsterdam"));

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().Be(0);
    }

    [Fact]
    public void ShouldReportSmallerOnCompareTo()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999900, Zone.Of("Europe/Amsterdam"));
        var dateTime2 = new ZonedDateTime(1947, 12, 17, 0, 59, 59, 999999900, Zone.Of("Europe/Amsterdam"));

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().BeLessThan(0);
    }

    [Fact]
    public void ShouldReportSmallerOnCompareToDiffOffset()
    {
        var dateTime1 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999900, Zone.Of("Europe/Amsterdam"));
        var dateTime2 = new ZonedDateTime(1947, 12, 16, 23, 59, 59, 999999900, Zone.Of("Europe/London"));

        var comp = dateTime1.CompareTo(dateTime2);

        comp.Should().BeLessThan(0);
    }

    [Fact]
    public void ShouldBeConvertableToDateTimeOffset()
    {
        var date = new DateTime(1947, 12, 16, 12, 15, 59, 660);
        var date1 = new ZonedDateTime(date, "Europe/Rome");
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
        var date = new ZonedDateTime(1947, 12, 16, 12, 15, 59, 660000999, Zone.Of("America/Dominica"));
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
    public void ShouldSupportUnknownZoneIds()
    {
        var date = new ZonedDateTime(0, 0, Zone.Of("Europe/Neo4j"));
        // Unknown ZoneIds should not be able to be converted to a local DateTime or DateTimeOffset
        Record.Exception(() => date.ToDateTimeOffset()).Should().BeOfType<TimeZoneNotFoundException>();
        Record.Exception(() => date.LocalDateTime).Should().BeOfType<TimeZoneNotFoundException>();

        // But they should be able to be converted to a UTC DateTime.
        date.UtcDateTime.Should().Be(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        date.ToString().Should().Be("{UtcSeconds: 0, Nanoseconds: 0, Zone: [Europe/Neo4j]}");
        date.UtcSeconds.Should().Be(0);
        date.Zone.Should().Be(Zone.Of("Europe/Neo4j"));
        date.Nanosecond.Should().Be(0);
        date.Ambiguous.Should().Be(false);
    }

    [Theory]
    [MemberData(nameof(NullZoneConstructors))]
    public void ShouldThrowWithNullZoneId(Func<ZonedDateTime> ctor)
    {
        Record.Exception(ctor).Should().BeOfType<ArgumentNullException>();
    }

    [Theory]
    [MemberData(nameof(LocalConstructorsWithUnkownZoneIds))]
    public void ShouldThrowExceptionWhenNonMonotonicTimeProvidedAndUnknownZoneId(Func<ZonedDateTime> ctor)
    {
        Record.Exception(ctor).Should().BeOfType<TimeZoneNotFoundException>();
    }

    [Fact]
    public void ShouldNotThrowExceptionWhenNonMonotonicTimeProvidedAndUnknownZoneId()
    {
        Record.Exception(() => new ZonedDateTime(new LocalDateTime(DateTime.Now), new ZoneId("Europe/Neo4j")))
            .Should()
            .BeNull();
    }
}

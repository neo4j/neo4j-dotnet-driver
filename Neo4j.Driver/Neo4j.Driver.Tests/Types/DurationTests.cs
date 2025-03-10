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

public class DurationTests
{
    [Fact]
    public void ShouldCreateDuration()
    {
        var cypherDuration = new Duration(15, 32, 785, 789215800);

        cypherDuration.Months.Should().Be(15);
        cypherDuration.Days.Should().Be(32);
        cypherDuration.Seconds.Should().Be(785);
        cypherDuration.Nanos.Should().Be(789215800);
    }

    [Fact]
    public void ShouldCreateDurationWithSecondsOnly()
    {
        var cypherDuration = new Duration(785);

        cypherDuration.Months.Should().Be(0);
        cypherDuration.Days.Should().Be(0);
        cypherDuration.Seconds.Should().Be(785);
        cypherDuration.Nanos.Should().Be(0);
    }

    [Fact]
    public void ShouldCreateDurationWithSecondsAndNanoseconds()
    {
        var cypherDuration = new Duration(785, 789215800);

        cypherDuration.Months.Should().Be(0);
        cypherDuration.Days.Should().Be(0);
        cypherDuration.Seconds.Should().Be(785);
        cypherDuration.Nanos.Should().Be(789215800);
    }

    [Fact]
    public void ShouldCreateDurationWithDaysSecondsAndNanoseconds()
    {
        var cypherDuration = new Duration(45, 785, 789215800);

        cypherDuration.Months.Should().Be(0);
        cypherDuration.Days.Should().Be(45);
        cypherDuration.Seconds.Should().Be(785);
        cypherDuration.Nanos.Should().Be(789215800);
    }

    [Theory]
    [InlineData(15, 32, 785, 789215800, "P15M32DT785.789215800S")]
    [InlineData(0, 32, 785, 789215800, "P0M32DT785.789215800S")]
    [InlineData(0, 0, 785, 789215800, "P0M0DT785.789215800S")]
    [InlineData(0, 0, 0, 789215800, "P0M0DT0.789215800S")]
    [InlineData(0, 0, -1, 0, "P0M0DT-1S")]
    [InlineData(0, 0, 0, 999999999, "P0M0DT0.999999999S")]
    [InlineData(0, 0, -1, 5, "P0M0DT-0.999999995S")]
    [InlineData(0, 0, -1, 999999999, "P0M0DT-0.000000001S")]
    [InlineData(500, 0, 0, 0, "P500M0DT0S")]
    [InlineData(0, 0, 0, 5, "P0M0DT0.000000005S")]
    [InlineData(0, 0, -500, 1, "P0M0DT-499.999999999S")]
    [InlineData(0, 0, -500, 0, "P0M0DT-500S")]
    [InlineData(-10, 5, -2, 500, "P-10M5DT-1.999999500S")]
    [InlineData(-10, -5, -2, 500, "P-10M-5DT-1.999999500S")]
    public void ShouldGenerateCorrectString(int months, int days, int seconds, int nanoseconds, string expected)
    {
        var cypherDuration = new Duration(months, days, seconds, nanoseconds);
        var cypherDurationStr = cypherDuration.ToString();

        cypherDurationStr.Should().Be(expected);
    }

    [Fact]
    public void ShouldGenerateSameHashcode()
    {
        var duration1 = new Duration(15, 32, 785, 789215800);
        var duration2 = new Duration(15, 32, 785, 789215800);

        duration1.GetHashCode().Should().Be(duration2.GetHashCode());
    }

    [Fact]
    public void ShouldGenerateDifferentHashcode()
    {
        var duration1 = new Duration(15, 32, 785, 789215800);
        var duration2 = new Duration(15, 32, 785, 789215801);

        duration1.GetHashCode().Should().NotBe(duration2.GetHashCode());
    }

    [Fact]
    public void ShouldBeEqual()
    {
        var duration1 = new Duration(15, 32, 785, 789215800);
        var duration2 = new Duration(15, 32, 785, 789215800);

        duration1.Equals(duration2).Should().BeTrue();
    }

    [Fact]
    public void ShouldNotBeEqual()
    {
        var duration1 = new Duration(15, 32, 785, 789215800);
        var duration2 = new Duration(15, 32, 786, 789215800);

        duration1.Equals(duration2).Should().BeFalse();
    }

    [Fact]
    public void ShouldNotBeEqualToAnotherType()
    {
        var duration = new Duration(15, 32, 785, 789215800);
        var other = "some string";

        duration.Equals(other).Should().BeFalse();
    }

    [Fact]
    public void ShouldNotBeEqualToNull()
    {
        var duration = new Duration(15, 32, 785, 789215800);
        var other = (object)null;

        duration.Equals(other).Should().BeFalse();
    }

    [Fact]
    public void ShouldThrowOnCompareToOtherType()
    {
        var duration1 = new Duration(0, 0, 0, 0);

        var ex = Record.Exception(() => duration1.CompareTo(new DateTime(1947, 12, 17)));

        ex.Should().NotBeNull().And.BeOfType<ArgumentException>();
    }

    [Fact]
    public void ShouldReportLargerOnCompareToNull()
    {
        var duration1 = new Duration(0, 0, 0, 0);

        var comp = duration1.CompareTo(null);

        comp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldReportLargerOnCompareTo()
    {
        var duration1 = new Duration(1, 12, 500, 999999999);
        var duration2 = new Duration(1, 12, 500, 0);

        var comp = duration1.CompareTo(duration2);

        comp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldReportLargerOnCompareToAbsolute()
    {
        var duration1 = new Duration(0, 1, 0, 1);
        var duration2 = new Duration(0, 0, 86400, 0);

        var comp = duration1.CompareTo(duration2);

        comp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldReportEqualOnCompareTo()
    {
        var duration1 = new Duration(1, 12, 500, 999999999);
        var duration2 = new Duration(1, 12, 500, 999999999);

        var comp = duration1.CompareTo(duration2);

        comp.Should().Be(0);
    }

    [Fact]
    public void ShouldReportEqualOnCompareToAbsolute()
    {
        var duration1 = new Duration(0, 1, 0, 999999999);
        var duration2 = new Duration(0, 0, 86400, 999999999);

        var comp = duration1.CompareTo(duration2);

        comp.Should().Be(0);
    }

    [Fact]
    public void ShouldReportSmallerOnCompareTo()
    {
        var duration1 = new Duration(1, 12, 500, 999999999);
        var duration2 = new Duration(1, 12, 501, 0);

        var comp = duration1.CompareTo(duration2);

        comp.Should().BeLessThan(0);
    }

    [Fact]
    public void ShouldReportSmallerOnCompareToAbsolute()
    {
        var duration1 = new Duration(0, 1, 0, 999999999);
        var duration2 = new Duration(0, 0, 86401, 999999999);

        var comp = duration1.CompareTo(duration2);

        comp.Should().BeLessThan(0);
    }

    [Fact]
    public void ShouldBeConvertableToString()
    {
        var duration = new Duration(12, 15, 59, 660000999);
        var durationStr1 = Convert.ToString(duration);
        var durationStr2 = Convert.ChangeType(duration, typeof(string));

        durationStr1.Should().Be("P12M15DT59.660000999S");
        durationStr2.Should().Be("P12M15DT59.660000999S");
    }

    [Fact]
    public void ShouldThrowWhenConversionIsNotSupported()
    {
        var duration = new Duration(12, 15, 59, 660000999);
        var conversions = new Action[]
        {
            () => Convert.ToDateTime(duration),
            () => Convert.ToBoolean(duration),
            () => Convert.ToByte(duration),
            () => Convert.ToChar(duration),
            () => Convert.ToDecimal(duration),
            () => Convert.ToDouble(duration),
            () => Convert.ToInt16(duration),
            () => Convert.ToInt32(duration),
            () => Convert.ToInt64(duration),
            () => Convert.ToSByte(duration),
            () => Convert.ToUInt16(duration),
            () => Convert.ToUInt32(duration),
            () => Convert.ToUInt64(duration),
            () => Convert.ToSingle(duration),
            () => Convert.ChangeType(duration, typeof(ArrayList))
        };

        foreach (var testAction in conversions)
        {
            testAction.Should().Throw<InvalidCastException>();
        }
    }
}

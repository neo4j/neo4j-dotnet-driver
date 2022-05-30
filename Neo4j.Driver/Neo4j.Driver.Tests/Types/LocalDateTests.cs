// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
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
using System.Diagnostics;
using FluentAssertions;
using Neo4j.Driver;
using Xunit;

namespace Neo4j.Driver.Tests.Types
{
    public class LocalDateTests
    {

        [Fact]
        public void ShouldCreateDateWithDateTimeComponents()
        {
            var cypherDate = new LocalDate(1947, 12, 17);

            cypherDate.ToDateTime().Should().Be(new DateTime(1947, 12, 17));
        }

        [Fact]
        public void ShouldCreateDateWithDateTime()
        {
            var date = new DateTime(1947, 12, 17);
            var cypherDate = new LocalDate(date);

            cypherDate.ToDateTime().Should().Be(date);
        }

        [Fact]
        [Conditional("NET6_0_OR_GREATER")]
        public void ShouldCreateDateWithDateOnly()
        {
            var date = new DateOnly(1947, 12, 17);
            var cypherDate = new LocalDate(date);

            cypherDate.ToDateOnly().Should().Be(date);
        }

        [Theory]
        [InlineData(-1000000000)]
        [InlineData(1000000000)]
        public void ShouldThrowOnInvalidYear(int year)
        {
            var ex = Record.Exception(() => new LocalDate(year, 1, 1));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public void ShouldThrowOnInvalidMonth(int month)
        {
            var ex = Record.Exception(() => new LocalDate(1990, month, 1));

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
            var ex = Record.Exception(() => new LocalDate(year, month, day));

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
            var date = new LocalDate(year, 1, 1);
            var ex = Record.Exception(() => date.ToDateTime());

            ex.Should().NotBeNull().And.BeOfType<ValueOverflowException>();
        }


        [Theory]
        [InlineData(-9999)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(10000)]
        [InlineData(9999999)]
        [Conditional("NET6_0_OR_GREATER")]
        public void ShouldThrowOnOverflowDateOnly(int year)
        {
            var date = new LocalDate(year, 1, 1);
            var ex = Record.Exception(() => date.ToDateOnly());

            ex.Should().NotBeNull().And.BeOfType<ValueOverflowException>();
        }

        [Theory]
        [InlineData(1947, 12, 17, "1947-12-17")]
        [InlineData(1947, 1, 1, "1947-01-01")]
        [InlineData(1, 1, 1, "0001-01-01")]
        [InlineData(9999, 1, 1, "9999-01-01")]
        [InlineData(-9999, 1, 1, "-9999-01-01")]
        [InlineData(999999, 1, 1, "999999-01-01")]
        [InlineData(-999999, 1, 1, "-999999-01-01")]
        public void ShouldGenerateCorrectString(int year, int month, int day, string expected)
        {
            var cypherDate = new LocalDate(year, month, day);
            var cypherDateStr = cypherDate.ToString();

            cypherDateStr.Should().Be(expected);
        }

        [Fact]
        public void ShouldGenerateSameHashcode()
        {
            var date1 = new LocalDate(1947, 12, 17);
            var date2 = new LocalDate(new DateTime(1947, 12, 17));

            date1.GetHashCode().Should().Be(date2.GetHashCode());
        }

        [Fact]
        public void ShouldGenerateDifferentHashcode()
        {
            var date1 = new LocalDate(1947, 12, 18);
            var date2 = new LocalDate(new DateTime(1947, 12, 17));

            date1.GetHashCode().Should().NotBe(date2.GetHashCode());
        }

        [Fact]
        public void ShouldBeEqual()
        {
            var date1 = new LocalDate(1947, 12, 17);
            var date2 = new LocalDate(new DateTime(1947, 12, 17));

            date1.Equals(date2).Should().BeTrue();
        }

        [Fact]
        public void ShouldNotBeEqual()
        {
            var date1 = new LocalDate(1947, 12, 17);
            var date2 = new LocalDate(new DateTime(1947, 12, 18));

            date1.Equals(date2).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToAnotherType()
        {
            var date = new LocalDate(1947, 12, 17);
            var other = "some string";

            date.Equals(other).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToNull()
        {
            var date = new LocalDate(1947, 12, 17);
            var other = (object) null;

            date.Equals(other).Should().BeFalse();
        }

        [Fact]
        public void ShouldThrowOnCompareToOtherType()
        {
            var date1 = new LocalDate(1947, 12, 17);

            var ex = Record.Exception(() => date1.CompareTo(new DateTime(1947, 12, 17)));

            ex.Should().NotBeNull().And.BeOfType<ArgumentException>();
        }

        [Fact]
        public void ShouldReportLargerOnCompareToNull()
        {
            var date1 = new LocalDate(1947, 12, 17);

            var comp = date1.CompareTo(null);

            comp.Should().BeGreaterThan(0);
        }

        [Fact]
        public void ShouldReportLargerOnCompareTo()
        {
            var date1 = new LocalDate(1947, 12, 17);
            var date2 = new LocalDate(1947, 12, 16);

            var comp = date1.CompareTo(date2);

            comp.Should().BeGreaterThan(0);
        }

        [Fact]
        public void ShouldReportEqualOnCompareTo()
        {
            var date1 = new LocalDate(1947, 12, 17);
            var date2 = new LocalDate(1947, 12, 17);

            var comp = date1.CompareTo(date2);

            comp.Should().Be(0);
        }

        [Fact]
        public void ShouldReportSmallerOnCompareTo()
        {
            var date1 = new LocalDate(1947, 12, 16);
            var date2 = new LocalDate(1947, 12, 17);

            var comp = date1.CompareTo(date2);

            comp.Should().BeLessThan(0);
        }

        [Fact]
        public void ShouldBeConvertableToDateTime()
        {
            var date = new DateTime(1947, 12, 16);
            var date1 = new LocalDate(date);
            var date2 = Convert.ToDateTime(date1);
            var date3 = Convert.ChangeType(date1, typeof(DateTime));

            date2.Should().Be(date);
            date3.Should().Be(date);
        }

        [Fact]
        public void ShouldBeConvertableToString()
        {
            var date = new LocalDate(1947, 12, 16);
            var dateStr1 = Convert.ToString(date);
            var dateStr2 = Convert.ChangeType(date, typeof(string));

            dateStr1.Should().Be("1947-12-16");
            dateStr2.Should().Be("1947-12-16");
        }

        [Fact]
        public void ShouldThrowWhenConversionIsNotSupported()
        {
            var date = new LocalDate(1947, 12, 16);
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
}
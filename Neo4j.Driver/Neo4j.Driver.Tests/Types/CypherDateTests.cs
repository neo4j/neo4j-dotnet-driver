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
    public class CypherDateTests
    {

        [Fact]
        public void ShouldCreateDateWithDateTimeComponents()
        {
            var cypherDate = new CypherDate(1947, 12, 17);

            cypherDate.DateTime.Should().Be(new DateTime(1947, 12, 17));
        }

        [Fact]
        public void ShouldCreateDateWithDateTime()
        {
            var date = new DateTime(1947, 12, 17);
            var cypherDate = new CypherDate(date);

            cypherDate.DateTime.Should().Be(date);
        }

        [Theory]
        [InlineData(-1000000000)]
        [InlineData(1000000000)]
        public void ShouldThrowOnInvalidYear(int year)
        {
            var ex = Record.Exception(() => new CypherDate(year, 1, 1));

            ex.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public void ShouldThrowOnInvalidMonth(int month)
        {
            var ex = Record.Exception(() => new CypherDate(1990, month, 1));

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
            var ex = Record.Exception(() => new CypherDate(year, month, day));

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
            var date = new CypherDate(year, 1, 1);
            var ex = Record.Exception(() => date.DateTime);

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
            var cypherDate = new CypherDate(year, month, day);
            var cypherDateStr = cypherDate.ToString();

            cypherDateStr.Should().Be(expected);
        }

        [Fact]
        public void ShouldGenerateSameHashcode()
        {
            var date1 = new CypherDate(1947, 12, 17);
            var date2 = new CypherDate(new DateTime(1947, 12, 17));

            date1.GetHashCode().Should().Be(date2.GetHashCode());
        }

        [Fact]
        public void ShouldGenerateDifferentHashcode()
        {
            var date1 = new CypherDate(1947, 12, 18);
            var date2 = new CypherDate(new DateTime(1947, 12, 17));

            date1.GetHashCode().Should().NotBe(date2.GetHashCode());
        }

        [Fact]
        public void ShouldBeEqual()
        {
            var date1 = new CypherDate(1947, 12, 17);
            var date2 = new CypherDate(new DateTime(1947, 12, 17));

            date1.Equals(date2).Should().BeTrue();
        }

        [Fact]
        public void ShouldNotBeEqual()
        {
            var date1 = new CypherDate(1947, 12, 17);
            var date2 = new CypherDate(new DateTime(1947, 12, 18));

            date1.Equals(date2).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToAnotherType()
        {
            var date = new CypherDate(1947, 12, 17);
            var other = "some string";

            date.Equals(other).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToNull()
        {
            var date = new CypherDate(1947, 12, 17);
            var other = (object) null;

            date.Equals(other).Should().BeFalse();
        }
    }
}
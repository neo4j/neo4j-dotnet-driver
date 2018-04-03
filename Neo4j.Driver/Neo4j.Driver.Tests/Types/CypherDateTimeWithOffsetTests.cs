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
    public class CypherDateTimeWithOffsetTests
    {

        [Fact]
        public void ShouldCreateDateTimeWithOffsetWithDateTimeComponents()
        {
            var cypherDateTime = new CypherDateTimeWithOffset(1947, 12, 17, 23, 49, 54, 1500);

            cypherDateTime.DateTime.Should().Be(new DateTime(1947, 12, 17, 23, 49, 54));
            cypherDateTime.Offset.Should().Be(TimeSpan.FromSeconds(1500));
        }

        [Fact]
        public void ShouldCreateDateTimeWithOffsetWithDateTimeComponentsWithNanoseconds()
        {
            var cypherDateTime = new CypherDateTimeWithOffset(1947, 12, 17, 23, 49, 54, 192794500, 1500);

            cypherDateTime.DateTime.Should().Be(new DateTime(1947, 12, 17, 23, 49, 54).AddTicks(1927945));
            cypherDateTime.Offset.Should().Be(TimeSpan.FromSeconds(1500));
        }

        [Fact]
        public void ShouldCreateDateTimeWithOffsetWithDateTime()
        {
            var dateTime = new DateTime(1947, 12, 17, 23, 49, 54, 120);
            var cypherDateTime = new CypherDateTimeWithOffset(dateTime, TimeSpan.FromSeconds(1500));

            cypherDateTime.DateTime.Should().Be(dateTime);
            cypherDateTime.Offset.Should().Be(TimeSpan.FromSeconds(1500));
        }

        [Fact]
        public void ShouldCreateDateTimeWithOffsetWithDateTimeOffset()
        {
            var dateTime = new DateTimeOffset(1947, 12, 17, 23, 49, 54, 120, TimeSpan.FromSeconds(1500));
            var cypherDateTime = new CypherDateTimeWithOffset(dateTime);

            cypherDateTime.DateTime.Should().Be(dateTime.DateTime);
            cypherDateTime.Offset.Should().Be(dateTime.Offset);
        }

        [Fact]
        public void ShouldConvertToDateTimeOffset()
        {
            var dateTime = new DateTimeOffset(1947, 12, 17, 23, 49, 54, 120, TimeSpan.FromSeconds(1500));
            var cypherDateTime = new CypherDateTimeWithOffset(dateTime);

            cypherDateTime.ToDateTimeOffset().Should().Be(dateTime);
        }

        [Fact]
        public void ShouldCreateDateWithRawValues()
        {
            var dateTime = new DateTime(1947, 12, 17, 23, 49, 54).AddTicks(1927945);
            var cypherDateTime = new CypherDateTimeWithOffset(TemporalHelpers.SecondsSinceEpoch(dateTime.Ticks),
                TemporalHelpers.NanosOfSecond(dateTime.Ticks), 1500);

            cypherDateTime.DateTime.Should().Be(dateTime);
            cypherDateTime.Offset.Should().Be(TimeSpan.FromSeconds(1500));
        }

        [Fact]
        public void ShouldGenerateCorrectString()
        {
            var cypherDateTime = new CypherDateTimeWithOffset(1947, 12, 17, 23, 49, 54, 192794500, 1500);
            var cypherDateTimeStr = cypherDateTime.ToString();

            cypherDateTimeStr.Should()
                .Be(
                    $"DateTimeWithOffset{{epochSeconds: {cypherDateTime.EpochSeconds}, nanosOfSecond: {cypherDateTime.NanosOfSecond}, offsetSeconds: {cypherDateTime.OffsetSeconds}}}");
        }

        [Fact]
        public void ShouldGenerateSameHashcode()
        {
            var dateTime1 = new CypherDateTimeWithOffset(1947, 12, 17, 15, 12, 01, 789000000, 1800);
            var dateTime2 = new CypherDateTimeWithOffset(new DateTime(1947, 12, 17, 15, 12, 01, 789), 1800);
            var dateTime3 = new CypherDateTimeWithOffset(-695551679, 789000000, 1800);

            dateTime1.GetHashCode().Should().Be(dateTime2.GetHashCode()).And.Be(dateTime3.GetHashCode());
        }

        [Fact]
        public void ShouldGenerateDifferentHashcode()
        {
            var dateTime1 = new CypherDateTimeWithOffset(1947, 12, 17, 15, 12, 01, 789000000, 1801);
            var dateTime2 = new CypherDateTimeWithOffset(new DateTime(1947, 12, 17, 15, 12, 01, 790), 1800);
            var dateTime3 = new CypherDateTimeWithOffset(-695553479, 789000200, 1800);

            dateTime1.GetHashCode().Should().NotBe(dateTime2.GetHashCode()).And.NotBe(dateTime3.GetHashCode());
        }

        [Fact]
        public void ShouldBeEqual()
        {
            var dateTime1 = new CypherDateTimeWithOffset(1947, 12, 17, 15, 12, 01, 789000000, 1800);
            var dateTime2 = new CypherDateTimeWithOffset(new DateTime(1947, 12, 17, 15, 12, 01, 789), 1800);
            var dateTime3 = new CypherDateTimeWithOffset(-695551679, 789000000, 1800);

            dateTime1.Should().Be(dateTime2);
            dateTime1.Should().Be(dateTime3);
        }

        [Fact]
        public void ShouldNotBeEqual()
        {
            var dateTime1 = new CypherDateTimeWithOffset(1947, 12, 17, 15, 12, 01, 789000000, 1801);
            var dateTime2 = new CypherDateTimeWithOffset(new DateTime(1947, 12, 17, 15, 12, 01, 790), 1800);
            var dateTime3 = new CypherDateTimeWithOffset(-695553479, 789005000, 1800);

            dateTime1.Should().NotBe(dateTime2);
            dateTime1.Should().NotBe(dateTime3);
        }

        [Fact]
        public void ShouldNotBeEqualToAnotherType()
        {
            var dateTime = new CypherDateTimeWithOffset(1947, 12, 17, 15, 12, 01, 789000000, 1800);
            var other = "some string";

            dateTime.Equals(other).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToNull()
        {
            var dateTime = new CypherDateTimeWithOffset(1947, 12, 17, 15, 12, 01, 789000000, 1800);
            var other = (object)null;

            dateTime.Equals(other).Should().BeFalse();
        }
    }
}
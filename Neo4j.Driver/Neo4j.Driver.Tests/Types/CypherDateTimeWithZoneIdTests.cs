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

        [Fact]
        public void ShouldCreateDateWithRawValues()
        {
            var dateTime = new DateTime(1947, 12, 17, 23, 49, 54).AddTicks(1927945);
            var cypherDateTime = new CypherDateTimeWithZoneId(TemporalHelpers.SecondsSinceEpoch(dateTime.Ticks),
                TemporalHelpers.NanosOfSecond(dateTime.Ticks), "Europe/Rome");

            cypherDateTime.DateTime.Should().Be(dateTime);
            cypherDateTime.Offset.Should().Be(TimeSpan.FromHours(1));
            cypherDateTime.ZoneId.Should().Be("Europe/Rome");
        }

        [Fact]
        public void ShouldGenerateCorrectString()
        {
            var cypherDateTime = new CypherDateTimeWithZoneId(1947, 12, 17, 23, 49, 54, 192794500, "Europe/Rome");
            var cypherDateTimeStr = cypherDateTime.ToString();

            cypherDateTimeStr.Should()
                .Be(
                    $"DateTimeWithZoneId{{epochSeconds: {cypherDateTime.EpochSeconds}, nanosOfSecond: {cypherDateTime.NanosOfSecond}, zoneId: '{cypherDateTime.ZoneId}'}}");
        }

        [Fact]
        public void ShouldGenerateSameHashcode()
        {
            var dateTime1 = new CypherDateTimeWithZoneId(1947, 12, 17, 15, 12, 01, 789000000, "Europe/Rome");
            var dateTime2 = new CypherDateTimeWithZoneId(new DateTime(1947, 12, 17, 15, 12, 01, 789), "Europe/Rome");
            var dateTime3 = new CypherDateTimeWithZoneId(-695551679, 789000000, "Europe/Rome");

            dateTime1.GetHashCode().Should().Be(dateTime2.GetHashCode()).And.Be(dateTime3.GetHashCode());
        }

        [Fact]
        public void ShouldGenerateDifferentHashcode()
        {
            var dateTime1 = new CypherDateTimeWithZoneId(1947, 12, 17, 15, 12, 01, 789000000, "Europe/Rome");
            var dateTime2 = new CypherDateTimeWithZoneId(new DateTime(1947, 12, 17, 15, 12, 01, 790), "Europe/Rome");
            var dateTime3 = new CypherDateTimeWithZoneId(-695555279, 789000200, "Europe/Rome");

            dateTime1.GetHashCode().Should().NotBe(dateTime2.GetHashCode()).And.NotBe(dateTime3.GetHashCode());
        }

        [Fact]
        public void ShouldBeEqual()
        {
            var dateTime1 = new CypherDateTimeWithZoneId(1947, 12, 17, 15, 12, 01, 789000000, "Europe/Rome");
            var dateTime2 = new CypherDateTimeWithZoneId(new DateTime(1947, 12, 17, 15, 12, 01, 789), "Europe/Rome");
            var dateTime3 = new CypherDateTimeWithZoneId(-695551679, 789000000, "Europe/Rome");

            dateTime1.Should().Be(dateTime2);
            dateTime1.Should().Be(dateTime3);
        }

        [Fact]
        public void ShouldNotBeEqual()
        {
            var dateTime1 = new CypherDateTimeWithZoneId(1947, 12, 17, 15, 12, 01, 789000000, "Europe/Rome");
            var dateTime2 = new CypherDateTimeWithZoneId(new DateTime(1947, 12, 17, 15, 12, 01, 790), "Europe/Rome");
            var dateTime3 = new CypherDateTimeWithZoneId(-695555279, 789005000, "Europe/Rome");

            dateTime1.Should().NotBe(dateTime2);
            dateTime1.Should().NotBe(dateTime3);
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
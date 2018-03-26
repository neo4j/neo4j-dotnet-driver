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
    public class CypherDateTimeTests
    {

        [Fact]
        public void ShouldCreateDateTimeWithDateTimeComponents()
        {
            var cypherDateTime = new CypherDateTime(1947, 12, 17, 23, 49, 54);

            cypherDateTime.ToDateTime().Should().Be(new DateTime(1947, 12, 17, 23, 49, 54));
        }

        [Fact]
        public void ShouldCreateDateTimeWithDateTimeComponentsWithNanoseconds()
        {
            var cypherDateTime = new CypherDateTime(1947, 12, 17, 23, 49, 54, 192794500);

            cypherDateTime.ToDateTime().Should().Be(new DateTime(1947, 12, 17, 23, 49, 54).AddTicks(1927945));
        }

        [Fact]
        public void ShouldCreateDateTimeWithDateTime()
        {
            var dateTime = new DateTime(1947, 12, 17, 23, 49, 54, 120);
            var cypherDateTime = new CypherDateTime(dateTime);

            cypherDateTime.ToDateTime().Should().Be(dateTime);
        }

        [Fact]
        public void ShouldCreateDateTimeWithRawValues()
        {
            var dateTime = new DateTime(1947, 12, 17, 23, 49, 54).AddTicks(1927945);
            var cypherDateTime = new CypherDateTime(TemporalHelpers.SecondsSinceEpoch(dateTime.Ticks),
                TemporalHelpers.NanosOfSecond(dateTime.Ticks));

            cypherDateTime.ToDateTime().Should().Be(dateTime);
        }

        [Fact]
        public void ShouldGenerateCorrectString()
        {
            var cypherDateTime = new CypherDateTime(1947, 12, 17, 23, 49, 54, 192794500);
            var cypherDateTimeStr = cypherDateTime.ToString();

            cypherDateTimeStr.Should()
                .Be(
                    $"DateTime{{epochSeconds: {cypherDateTime.EpochSeconds}, nanosOfSecond: {cypherDateTime.NanosOfSecond}}}");
        }

        [Fact]
        public void ShouldGenerateSameHashcode()
        {
            var dateTime1 = new CypherDateTime(1947, 12, 17, 15, 12, 01, 789000000);
            var dateTime2 = new CypherDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 789));
            var dateTime3 = new CypherDateTime(-695551679, 789000000);

            dateTime1.GetHashCode().Should().Be(dateTime2.GetHashCode()).And.Be(dateTime3.GetHashCode());
        }

        [Fact]
        public void ShouldGenerateDifferentHashcode()
        {
            var dateTime1 = new CypherDateTime(1947, 12, 17, 15, 12, 01, 789000000);
            var dateTime2 = new CypherDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 790));
            var dateTime3 = new CypherDateTime(-695551678, 788000000);

            dateTime1.GetHashCode().Should().NotBe(dateTime2.GetHashCode()).And.NotBe(dateTime3.GetHashCode());
        }

        [Fact]
        public void ShouldBeEqual()
        {
            var dateTime1 = new CypherDateTime(1947, 12, 17, 15, 12, 01, 789000000);
            var dateTime2 = new CypherDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 789));

            dateTime1.Equals(dateTime2).Should().BeTrue();
        }

        [Fact]
        public void ShouldNotBeEqual()
        {
            var dateTime1 = new CypherDateTime(1947, 12, 17, 15, 12, 01, 789000000);
            var dateTime2 = new CypherDateTime(new DateTime(1947, 12, 17, 15, 12, 01, 788));

            dateTime1.Equals(dateTime2).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToAnotherType()
        {
            var dateTime = new CypherDateTime(1947, 12, 17, 15, 12, 01, 789000000);
            var other = "some string";

            dateTime.Equals(other).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToNull()
        {
            var dateTime = new CypherDateTime(1947, 12, 17, 15, 12, 01, 789000000);
            var other = (object)null;

            dateTime.Equals(other).Should().BeFalse();
        }
    }
}
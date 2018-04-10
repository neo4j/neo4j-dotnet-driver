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
    public class CypherDurationTests
    {

        [Fact]
        public void ShouldCreateDuration()
        {
            var cypherDuration = new CypherDuration(15, 32, 785, 789215800);

            cypherDuration.Months.Should().Be(15);
            cypherDuration.Days.Should().Be(32);
            cypherDuration.Seconds.Should().Be(785);
            cypherDuration.Nanos.Should().Be(789215800);
        }

        [Fact]
        public void ShouldCreateDurationWithSecondsOnly()
        {
            var cypherDuration = new CypherDuration(785);

            cypherDuration.Months.Should().Be(0);
            cypherDuration.Days.Should().Be(0);
            cypherDuration.Seconds.Should().Be(785);
            cypherDuration.Nanos.Should().Be(0);
        }

        [Fact]
        public void ShouldCreateDurationWithSecondsAndNanoseconds()
        {
            var cypherDuration = new CypherDuration(785, 789215800);

            cypherDuration.Months.Should().Be(0);
            cypherDuration.Days.Should().Be(0);
            cypherDuration.Seconds.Should().Be(785);
            cypherDuration.Nanos.Should().Be(789215800);
        }

        [Fact]
        public void ShouldCreateDurationWithDaysSecondsAndNanoseconds()
        {
            var cypherDuration = new CypherDuration(45, 785, 789215800);

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
        [InlineData(0, 0, 0, 0, "P0M0DT0.000000000S")]
        [InlineData(500, 0, 0, 0, "P500M0DT0.000000000S")]
        [InlineData(0, 0, 0, 5, "P0M0DT0.000000005S")]
        public void ShouldGenerateCorrectString(int months, int days, int seconds, int nanoseconds, string expected)
        {
            var cypherDuration = new CypherDuration(months, days, seconds, nanoseconds);
            var cypherDurationStr = cypherDuration.ToString();

            cypherDurationStr.Should().Be(expected);
        }

        [Fact]
        public void ShouldGenerateSameHashcode()
        {
            var duration1 = new CypherDuration(15, 32, 785, 789215800);
            var duration2 = new CypherDuration(15, 32, 785, 789215800);

            duration1.GetHashCode().Should().Be(duration2.GetHashCode());
        }

        [Fact]
        public void ShouldGenerateDifferentHashcode()
        {
            var duration1 = new CypherDuration(15, 32, 785, 789215800);
            var duration2 = new CypherDuration(15, 32, 785, 789215801);

            duration1.GetHashCode().Should().NotBe(duration2.GetHashCode());
        }

        [Fact]
        public void ShouldBeEqual()
        {
            var duration1 = new CypherDuration(15, 32, 785, 789215800);
            var duration2 = new CypherDuration(15, 32, 785, 789215800);

            duration1.Equals(duration2).Should().BeTrue();
        }

        [Fact]
        public void ShouldNotBeEqual()
        {
            var duration1 = new CypherDuration(15, 32, 785, 789215800);
            var duration2 = new CypherDuration(15, 32, 786, 789215800);

            duration1.Equals(duration2).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToAnotherType()
        {
            var duration = new CypherDuration(15, 32, 785, 789215800);
            var other = "some string";

            duration.Equals(other).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToNull()
        {
            var duration = new CypherDuration(15, 32, 785, 789215800);
            var other = (object) null;

            duration.Equals(other).Should().BeFalse();
        }
    }
}
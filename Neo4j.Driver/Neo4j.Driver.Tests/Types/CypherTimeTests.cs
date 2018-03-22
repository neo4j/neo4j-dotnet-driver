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
    public class CypherTimeTests
    {

        [Fact]
        public void ShouldCreateTimeWithTimeComponents()
        {
            var cypherTime = new CypherTime(13, 15, 59);

            cypherTime.ToTimeSpan().Should().Be(new TimeSpan(13, 15, 59));
        }

        [Fact]
        public void ShouldCreateTimeWithTimeSpan()
        {
            var time = new TimeSpan(0, 13, 59, 59, 255);
            var cypherTime = new CypherTime(time);

            cypherTime.ToTimeSpan().Should().Be(time);
        }

        [Fact]
        public void ShouldCreateTimeWithRawValues()
        {
            var time = new TimeSpan(0, 13, 59, 59, 25);
            var cypherTime = new CypherTime(time.Ticks * 100);

            cypherTime.ToTimeSpan().Should().Be(time);
        }

        [Fact]
        public void ShouldGenerateCorrectString()
        {
            var cypherTime = new CypherTime(13, 15, 59, 274000000);
            var cypherTimeStr = cypherTime.ToString();

            cypherTimeStr.Should().Be($"Time{{nanosOfDay: {cypherTime.NanosecondsOfDay}}}");
        }

        [Fact]
        public void ShouldGenerateSameHashcode()
        {
            var time1 = new CypherTime(12, 49, 55, 123000000);
            var time2 = new CypherTime(new DateTime(2017, 1, 1, 12, 49, 55, 123));
            var time3 = new CypherTime(new TimeSpan(0, 12, 49, 55, 123));
            var time4 = new CypherTime(46195123000000);

            time1.GetHashCode().Should().Be(time2.GetHashCode()).And.Be(time3.GetHashCode()).And
                .Be(time4.GetHashCode());
        }

        [Fact]
        public void ShouldGenerateDifferentHashcode()
        {
            var time1 = new CypherTime(12, 49, 55, 123000001);
            var time2 = new CypherTime(new DateTime(2017, 1, 1, 12, 49, 55, 123));
            var time3 = new CypherTime(new TimeSpan(0, 12, 49, 55, 124));
            var time4 = new CypherTime(46195123000020);

            time1.GetHashCode().Should().NotBe(time2.GetHashCode()).And.NotBe(time3.GetHashCode()).And
                .NotBe(time4.GetHashCode());
        }

        [Fact]
        public void ShouldBeEqual()
        {
            var time1 = new CypherTime(12, 49, 55, 123000000);
            var time2 = new CypherTime(new DateTime(2017, 1, 1, 12, 49, 55, 123));
            var time3 = new CypherTime(new TimeSpan(0, 12, 49, 55, 123));
            var time4 = new CypherTime(46195123000000);

            time1.Equals(time2).Should().BeTrue();
            time1.Equals(time3).Should().BeTrue();
            time1.Equals(time4).Should().BeTrue();
        }

        [Fact]
        public void ShouldNotBeEqual()
        {
            var time1 = new CypherTime(12, 49, 55, 123000001);
            var time2 = new CypherTime(new DateTime(2017, 1, 1, 12, 49, 55, 123));
            var time3 = new CypherTime(new TimeSpan(0, 12, 49, 55, 125));
            var time4 = new CypherTime(46195123002000);

            time1.Equals(time2).Should().BeFalse();
            time1.Equals(time3).Should().BeFalse();
            time1.Equals(time4).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToAnotherType()
        {
            var time = new CypherTime(12, 49, 55, 123000001);
            var other = "some string";

            time.Equals(other).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqualToNull()
        {
            var time = new CypherTime(12, 49, 55, 123000001);
            var other = (object)null;

            time.Equals(other).Should().BeFalse();
        }
    }
}
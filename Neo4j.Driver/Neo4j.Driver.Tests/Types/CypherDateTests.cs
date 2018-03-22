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

            cypherDate.ToDateTime().Should().Be(new DateTime(1947, 12, 17));
        }

        [Fact]
        public void ShouldCreateDateWithDateTime()
        {
            var date = new DateTime(1947, 12, 17);
            var cypherDate = new CypherDate(date);

            cypherDate.ToDateTime().Should().Be(date);
        }

        [Fact]
        public void ShouldCreateDateWithRawValues()
        {
            var date = new DateTime(1947, 12, 17);
            var cypherDate = new CypherDate((long) date.Subtract(new DateTime(1970, 1, 1)).TotalDays);

            cypherDate.ToDateTime().Should().Be(date);
        }

        [Fact]
        public void ShouldGenerateCorrectString()
        {
            var cypherDate = new CypherDate(1947, 12, 17);
            var cypherDateStr = cypherDate.ToString();

            cypherDateStr.Should().Be($"Date{{epochDays: {cypherDate.EpochDays}}}");
        }

        [Fact]
        public void ShouldGenerateSameHashcode()
        {
            var date1 = new CypherDate(1947, 12, 17);
            var date2 = new CypherDate(new DateTime(1947, 12, 17));
            var date3 = new CypherDate(-8051);

            date1.GetHashCode().Should().Be(date2.GetHashCode()).And.Be(date3.GetHashCode());
        }

        [Fact]
        public void ShouldGenerateDifferentHashcode()
        {
            var date1 = new CypherDate(1947, 12, 18);
            var date2 = new CypherDate(new DateTime(1947, 12, 17));
            var date3 = new CypherDate(-8052);

            date1.GetHashCode().Should().NotBe(date2.GetHashCode()).And.NotBe(date3.GetHashCode());
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
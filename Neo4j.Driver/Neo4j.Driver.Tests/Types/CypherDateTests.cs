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
    }
}
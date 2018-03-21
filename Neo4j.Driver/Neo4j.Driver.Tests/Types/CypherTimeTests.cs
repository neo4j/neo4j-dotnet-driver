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
    }
}
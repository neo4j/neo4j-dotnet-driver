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
        public void ShouldCreateDateWithDateTimeComponents()
        {
            var cypherDateTime = new CypherDateTime(1947, 12, 17, 23, 49, 54, 192794500);

            cypherDateTime.ToDateTime().Should().Be(new DateTime(1947, 12, 17, 23, 49, 54).AddTicks(1927945));
        }

        [Fact]
        public void ShouldCreateDateWithDateTime()
        {
            var dateTime = new DateTime(1947, 12, 17, 23, 49, 54, 120);
            var cypherDateTime = new CypherDateTime(dateTime);

            cypherDateTime.ToDateTime().Should().Be(dateTime);
        }

        [Fact]
        public void ShouldCreateDateWithRawValues()
        {
            var dateTime = new DateTime(1947, 12, 17, 23, 49, 54).AddTicks(1927945);
            var cypherDateTime = new CypherDateTime(TemporalHelpers.ComputeSecondsSinceEpoch(dateTime.Ticks),
                TemporalHelpers.ComputeNanosOfSecond(dateTime.Ticks));

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
    }
}
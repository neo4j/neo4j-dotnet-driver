// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Tests.Temporal;

public class TemporalHelpersTests
{
    [Theory]
    // zero duration
    [InlineData(0, 0, 0, 0, "P0D")]
    // simple date parts only
    [InlineData(1, 0, 0, 0, "P1M")]
    [InlineData(0, 10, 0, 0, "P10D")]
    [InlineData(13, 0, 0, 0, "P1Y1M")]                    // 13 months -> 1Y1M
    [InlineData(25, 5, 0, 0, "P2Y1M5D")]                  // 25 months, 5 days -> 2Y1M5D

    // seconds normalization into H/M/S
    [InlineData(0, 0, 65, 0, "PT1M5S")]                   // 65 seconds -> 1M5S
    [InlineData(0, 0, 3600, 0, "PT1H")]                   // 3600 seconds -> 1H
    [InlineData(0, 0, 3665, 0, "PT1H1M5S")]               // 3665 seconds -> 1H1M5S
    [InlineData(0, 0, 24 * 3600, 0, "P1D")]               // 86400 seconds -> 1D
    [InlineData(0, 1, 24 * 3600, 0, "P2D")]               // 1 day + 86400s -> 2D

    // fractional seconds, normalization of nanos
    [InlineData(0, 0, 0, 1_000_000_000, "PT1S")]          // 1e9 ns -> 1S
    [InlineData(0, 0, 1, 500_000_000, "PT1.500000000S")]  // 1.5s
    [InlineData(0, 0, 61, 250_000_000, "PT1M1.250000000S")] // 61.25s -> 1M1.25S

    // negative duration (all negative -> leading '-')
    [InlineData(-13, 0, 0, 0, "-P1Y1M")]                  // -13 months -> -P1Y1M
    [InlineData(0, -1, 0, 0, "-P1D")]                     // -1 day
    [InlineData(0, 0, -65, 0, "-PT1M5S")]                 // -65 seconds
    [InlineData(0, 0, -1, -500_000_000, "-PT1.500000000S")] // -1.5s

    // mixed signs (method treats it as negative overall)
    [InlineData(-1, 2, 0, 0, "-P1M2D")]
    [InlineData(1, -2, 0, 0, "-P1M2D")]
    [InlineData(0, 0, -1, 500_000_000, "-PT1.500000000S")]
    public void ToIsoDurationString_ProducesExpectedString(
        long months,
        long days,
        long seconds,
        int nanoseconds,
        string expected)
    {
        // act
        var actual = Neo4j.Driver.Internal.Helpers.TemporalHelpers.ToIsoDurationString(
            months, days, seconds, nanoseconds);

        // assert
        actual.Should().Be(expected);
    }
}

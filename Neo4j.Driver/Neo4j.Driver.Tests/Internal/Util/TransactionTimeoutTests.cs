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

using System;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Util;

public class TransactionTimeoutTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 1)]
    [InlineData(0.1, 0, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 2)]
    [InlineData(1.23, 0, 2)]
    [InlineData(598, 1, 599)]
    [InlineData(598.2, 0, 599)]
    [InlineData(2000, 96, 2001)]
    [InlineData(2000.8, 0, 2001)]
    public void ShouldRoundSubmillisecondTimeoutsToMilliseconds(
        double milliseconds,
        int ticks,
        int expectedMilliseconds)
    {
        var inputMilliseconds = TimeSpan.FromMilliseconds(milliseconds);
        var inputTicks = TimeSpan.FromTicks(ticks);
        var totalInput = inputMilliseconds + inputTicks;
        var expectedTimeout = TimeSpan.FromMilliseconds(expectedMilliseconds);

        var autoMocker = new AutoMocker(MockBehavior.Strict);
        if (expectedTimeout != inputMilliseconds)
        {
            // only logs if it changes the timeout
            autoMocker.GetMock<ILogger>()
                .Setup(
                    x => x.Info(
                        It.Is<string>(s => s.Contains("rounded up")),
                        It.IsAny<object[]>()))
                .Verifiable();
        }

        var sut =
            new TransactionConfigBuilder(autoMocker.GetMock<ILogger>().Object, TransactionConfig.Default)
                .WithTimeout(totalInput);

        var result = sut.Build().Timeout;

        result.Should().Be(expectedTimeout);
        (result?.Ticks % TimeSpan.TicksPerMillisecond).Should().Be(0);
        autoMocker.VerifyAll();
    }
}

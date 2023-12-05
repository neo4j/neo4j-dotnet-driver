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

using System.Threading;
using FluentAssertions;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.Tests.TestBackend;
using Xunit;

namespace Neo4j.Driver.Internal.Util;

public class DateTimeBasedTimerTests
{
    public DateTimeBasedTimerTests()
    {
        FakeTime.Instance.Unfreeze();
    }

    [Fact]
    public void ShouldRunNormally()
    {
        var timer = new DateTimeBasedTimer();

        Thread.Sleep(100);

        timer.ElapsedMilliseconds.Should().BeInRange(100, 150);
    }

    [Fact]
    public void ShouldReset()
    {
        var timer = new DateTimeBasedTimer();

        Thread.Sleep(100);
        timer.Reset();

        timer.ElapsedMilliseconds.Should().BeInRange(0, 50);
    }

    [Fact]
    public void ShouldFreezeWithFakeTime()
    {
        DateTimeProvider.StaticInstance = FakeTime.Instance;
        FakeTime.Instance.Freeze();
        var timer = new DateTimeBasedTimer();

        Thread.Sleep(100);

        timer.ElapsedMilliseconds.Should().Be(0);
    }

    [Fact]
    public void ShouldReflectFakeTimeTick()
    {
        DateTimeProvider.StaticInstance = FakeTime.Instance;
        FakeTime.Instance.Freeze();
        var timer = new DateTimeBasedTimer();

        FakeTime.Instance.Advance(1000);

        timer.ElapsedMilliseconds.Should().Be(1000);
    }

    // this test is actually to illustrate a potential issue when the timer is created before FakeTime is installed
    [Fact]
    public void MustBeCreatedAfterFakeTimeInstalled()
    {
        var timer = new DateTimeBasedTimer();
        Thread.Sleep(100);
        DateTimeProvider.StaticInstance = FakeTime.Instance;

        FakeTime.Instance.Freeze();
        Thread.Sleep(100);

        // the first sleep still happened - this might cause some issues
        timer.ElapsedMilliseconds.Should().BeInRange(100, 150);
    }
}

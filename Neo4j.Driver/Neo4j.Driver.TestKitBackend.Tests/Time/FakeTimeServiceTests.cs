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

using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.TestKitBackend.Time;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Time;

public class FakeTimeServiceTests : IDisposable
{
    private readonly IDateTimeProvider _original = DateTimeProvider.StaticInstance;
    private readonly FakeTimeService _service = new();

    public void Dispose()
    {
        DateTimeProvider.StaticInstance = _original;
    }

    [Fact]
    public void Install_freezes_the_driver_clock()
    {
        _service.Install();

        var first = DateTimeProvider.StaticInstance.Now();
        var second = DateTimeProvider.StaticInstance.Now();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Tick_advances_the_frozen_clock_by_exactly_the_increment()
    {
        _service.Install();
        var before = DateTimeProvider.StaticInstance.Now();

        _service.Tick(3_599_999);

        Assert.Equal(before.AddMilliseconds(3_599_999), DateTimeProvider.StaticInstance.Now());
    }

    [Fact]
    public void Tick_advances_timers_created_while_installed()
    {
        _service.Install();
        var timer = DateTimeProvider.StaticInstance.NewTimer();
        timer.Start();

        _service.Tick(1_500);

        Assert.Equal(1_500, timer.ElapsedMilliseconds);
    }

    [Fact]
    public void Uninstall_restores_the_real_provider()
    {
        _service.Install();

        _service.Uninstall();

        Assert.Same(_original, DateTimeProvider.StaticInstance);
    }
}

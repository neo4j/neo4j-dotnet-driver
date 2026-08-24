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
using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.TestKitBackend.Time;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Time;

[Collection(FakeSystemClockCollection.Name)]
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

        second.Should().Be(first);
    }

    [Fact]
    public void Tick_advances_the_frozen_clock_by_exactly_the_increment()
    {
        _service.Install();
        var before = DateTimeProvider.StaticInstance.Now();

        _service.Tick(3_599_999);

        DateTimeProvider.StaticInstance.Now().Should().Be(before.AddMilliseconds(3_599_999));
    }

    [Fact]
    public void Tick_advances_timers_created_while_installed()
    {
        _service.Install();
        var timer = DateTimeProvider.StaticInstance.NewTimer();
        timer.Start();

        _service.Tick(1_500);

        timer.ElapsedMilliseconds.Should().Be(1_500);
    }

    [Fact]
    public void Install_twice_without_uninstalling_throws()
    {
        _service.Install();

        var act = () => _service.Install();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Uninstall_after_a_rejected_double_install_still_restores_the_real_provider()
    {
        _service.Install();
        var act = () => _service.Install();
        act.Should().Throw<InvalidOperationException>();

        _service.Uninstall();

        DateTimeProvider.StaticInstance.Should().BeSameAs(_original);
    }

    [Fact]
    public void Uninstall_restores_the_real_provider()
    {
        _service.Install();

        _service.Uninstall();

        DateTimeProvider.StaticInstance.Should().BeSameAs(_original);
    }

    [Fact]
    public void Dispose_uninstalls_a_clock_left_installed()
    {
        _service.Install();

        _service.Dispose();

        DateTimeProvider.StaticInstance.Should().BeSameAs(_original);
    }

    [Fact]
    public void Dispose_without_a_preceding_install_leaves_the_real_provider_in_place()
    {
        _service.Dispose();

        DateTimeProvider.StaticInstance.Should().BeSameAs(_original);
    }

    [Fact]
    public void Uninstall_without_a_preceding_install_leaves_the_real_provider_in_place()
    {
        _service.Uninstall();

        DateTimeProvider.StaticInstance.Should().BeSameAs(_original);
    }

    [Fact]
    public void A_new_timer_does_not_accrue_until_started()
    {
        _service.Install();
        var timer = DateTimeProvider.StaticInstance.NewTimer();

        _service.Tick(100);

        timer.ElapsedMilliseconds.Should().Be(0);
    }

    [Fact]
    public void Reset_stops_the_timer_and_zeroes_it()
    {
        _service.Install();
        var timer = DateTimeProvider.StaticInstance.NewTimer();
        timer.Start();
        _service.Tick(100);

        timer.Reset();
        _service.Tick(100);

        timer.ElapsedMilliseconds.Should().Be(0);
    }

    [Fact]
    public void Start_after_reset_resumes_accrual_from_zero()
    {
        _service.Install();
        var timer = DateTimeProvider.StaticInstance.NewTimer();
        timer.Start();
        _service.Tick(100);
        timer.Reset();

        timer.Start();
        _service.Tick(50);

        timer.ElapsedMilliseconds.Should().Be(50);
    }

    [Fact]
    public void Uninstall_of_a_superseded_service_leaves_the_newer_fake_installed()
    {
        var newerService = new FakeTimeService();
        _service.Install();
        newerService.Install();
        var newerFake = DateTimeProvider.StaticInstance;

        _service.Uninstall();

        DateTimeProvider.StaticInstance.Should().BeSameAs(newerFake);
    }

    [Fact]
    public void Uninstall_after_overlapping_installs_restores_the_real_provider()
    {
        var newerService = new FakeTimeService();
        _service.Install();
        newerService.Install();

        _service.Uninstall();
        newerService.Uninstall();

        DateTimeProvider.StaticInstance.Should().BeSameAs(_original);
    }

    [Fact]
    public async Task Timers_can_be_created_while_a_tick_is_advancing()
    {
        _service.Install();
        var provider = DateTimeProvider.StaticInstance;

        var ticking = Task.Run(
            () =>
            {
                for (var i = 0; i < 2000; i++)
                {
                    _service.Tick(1);
                }
            },
            TestContext.Current.CancellationToken);

        var creating = Task.Run(
            () =>
            {
                for (var i = 0; i < 2000; i++)
                {
                    provider.NewTimer();
                }
            },
            TestContext.Current.CancellationToken);

        var act = () => Task.WhenAll(ticking, creating);

        await act.Should().NotThrowAsync();
    }
}

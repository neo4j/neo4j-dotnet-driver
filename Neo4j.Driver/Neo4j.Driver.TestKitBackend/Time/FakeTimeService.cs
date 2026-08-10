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

using ITimer = Neo4j.Driver.Internal.ITimer;

namespace Neo4j.Driver.TestKitBackend.Time;

internal interface IFakeTimeService
{
    void Install();

    void Tick(long incrementMs);

    void Uninstall();
}

[RegistrationLifetime(RegistrationLifetime.PerLifetimeScope)]
internal class FakeTimeService : IFakeTimeService, IDisposable
{
    private IDateTimeProvider? _original;
    private FakeDateTimeProvider? _fake;

    public void Dispose()
    {
        Uninstall();
    }

    public void Install()
    {
        if (_fake is not null)
        {
            throw new InvalidOperationException("The fake time service is already installed.");
        }

        _original = DateTimeProvider.StaticInstance;
        _fake = new FakeDateTimeProvider();
        DateTimeProvider.StaticInstance = _fake;
    }

    public void Tick(long incrementMs)
    {
        _fake!.Advance(incrementMs);
    }

    public void Uninstall()
    {
        if (_fake is null)
        {
            return;
        }

        DateTimeProvider.StaticInstance = _original!;
        _original = null;
        _fake = null;
    }

    private class FakeDateTimeProvider : IDateTimeProvider
    {
        private readonly List<FakeTimer> _timers = [];
        private DateTime _now = DateTime.UtcNow;

        public DateTime Now()
        {
            return _now;
        }

        public ITimer NewTimer()
        {
            var timer = new FakeTimer();
            _timers.Add(timer);
            return timer;
        }

        public void Advance(long milliseconds)
        {
            _now = _now.AddMilliseconds(milliseconds);
            foreach (var timer in _timers)
            {
                timer.Advance(milliseconds);
            }
        }
    }

    private class FakeTimer : ITimer
    {
        public long ElapsedMilliseconds { get; private set; }

        public void Reset()
        {
            ElapsedMilliseconds = 0;
        }

        public void Start()
        {
        }

        public void Advance(long milliseconds)
        {
            ElapsedMilliseconds += milliseconds;
        }
    }
}

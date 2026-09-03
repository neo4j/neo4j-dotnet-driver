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

        var current = DateTimeProvider.StaticInstance;
        _fake = new FakeDateTimeProvider(current is FakeDateTimeProvider superseded ? superseded.Original : current);
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

        if (DateTimeProvider.StaticInstance == _fake)
        {
            DateTimeProvider.StaticInstance = _fake.Original;
        }

        _fake = null;
    }

    private class FakeDateTimeProvider : IDateTimeProvider
    {
        private readonly Lock _lock = new();
        private readonly List<FakeTimer> _timers = [];
        private DateTime _now = DateTime.UtcNow;

        public FakeDateTimeProvider(IDateTimeProvider original)
        {
            Original = original;
        }

        public IDateTimeProvider Original { get; }

        public DateTime Now()
        {
            lock (_lock)
            {
                return _now;
            }
        }

        public ITimer NewTimer()
        {
            var timer = new FakeTimer();
            lock (_lock)
            {
                _timers.Add(timer);
            }

            return timer;
        }

        public void Advance(long milliseconds)
        {
            lock (_lock)
            {
                _now = _now.AddMilliseconds(milliseconds);
                foreach (var timer in _timers)
                {
                    timer.Advance(milliseconds);
                }
            }
        }
    }

    private class FakeTimer : ITimer
    {
        private bool _running;

        public long ElapsedMilliseconds { get; private set; }

        public void Reset()
        {
            _running = false;
            ElapsedMilliseconds = 0;
        }

        public void Start()
        {
            _running = true;
        }

        public void Advance(long milliseconds)
        {
            if (_running)
            {
                ElapsedMilliseconds += milliseconds;
            }
        }
    }
}

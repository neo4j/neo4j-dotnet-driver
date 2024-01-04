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
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Services;

namespace Neo4j.Driver.Tests.TestBackend;

internal class FakeTimeHolder
{
    internal static IDateTimeProvider OriginalTimeProvider;
}

internal class FakeTimeInstall : ProtocolObject
{
    public object data { get; set; }

    public override Task Process()
    {
        FakeTimeHolder.OriginalTimeProvider = DateTimeProvider.StaticInstance;
        DateTimeProvider.StaticInstance = FakeTime.Instance;
        FakeTime.Instance.Freeze();
        return Task.CompletedTask;
    }

    public override string Respond()
    {
        return new ProtocolResponse("FakeTimeAck").Encode();
    }
}

internal class FakeTimeTick : ProtocolObject
{
    public FakeTimeTickDto data { get; set; }

    public override Task Process()
    {
        FakeTime.Instance.Advance(data.incrementMs);
        return Task.CompletedTask;
    }

    public override string Respond()
    {
        return new ProtocolResponse("FakeTimeAck").Encode();
    }

    public record FakeTimeTickDto(int incrementMs);
}

internal class FakeTimeUninstall : ProtocolObject
{
    public object data { get; set; }

    public override Task Process()
    {
        DateTimeProvider.StaticInstance = FakeTimeHolder.OriginalTimeProvider;
        FakeTime.Instance.Uninstall();
        return Task.CompletedTask;
    }

    public override string Respond()
    {
        return new ProtocolResponse("FakeTimeAck").Encode();
    }
}

internal class FakeTime : IDateTimeProvider
{
    public static readonly FakeTime Instance = new();

    private DateTime? _frozenTime;
    private readonly List<FakeTimer> _timers = new();

    public DateTime Now()
    {
        return _frozenTime ?? DateTime.Now;
    }

    public ITimer NewTimer()
    {
        var fakeTimer = new FakeTimer();
        _timers.Add(fakeTimer);
        return fakeTimer;
    }

    public void Freeze()
    {
        _frozenTime = DateTime.Now;
    }

    public void Advance(int milliseconds)
    {
        _frozenTime = Now().AddMilliseconds(milliseconds);
        foreach (var timer in _timers)
        {
            timer.Advance(milliseconds);
        }
    }

    public void Uninstall()
    {
        _frozenTime = null;
        _timers.Clear();
    }
}

internal class FakeTimer : ITimer
{
    private long _advanced;
    
    public void Advance(int milliseconds)
    {
        _advanced += milliseconds;
    }

    public long ElapsedMilliseconds => _advanced;
    
    public void Reset()
    {
        _advanced = 0;
    }

    public void Start()
    {
    }
}

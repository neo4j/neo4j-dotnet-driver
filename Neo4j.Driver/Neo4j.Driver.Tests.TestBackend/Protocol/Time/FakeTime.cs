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

internal class FakeTimeInstall : IProtocolObject
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

internal class FakeTimeTick : IProtocolObject
{
    public DataType data { get; set; }

    public override Task Process()
    {
        FakeTime.Instance.Advance(data.incrementMs);
        return Task.CompletedTask;
    }

    public override string Respond()
    {
        return new ProtocolResponse("FakeTimeAck").Encode();
    }

    public record DataType(int incrementMs);
}

internal class FakeTimeUninstall : IProtocolObject
{
    public object data { get; set; }

    public override Task Process()
    {
        DateTimeProvider.StaticInstance = FakeTimeHolder.OriginalTimeProvider;
        FakeTime.Instance.Unfreeze();
        return Task.CompletedTask;
    }

    public override string Respond()
    {
        return new ProtocolResponse("FakeTimeAck").Encode();
    }
}

internal class FakeTime : IDateTimeProvider
{
    public static FakeTime Instance = new();

    private DateTime? _frozenTime;
    private List<FakeTimer> Timers = new();

    public DateTime Now()
    {
        return _frozenTime ?? DateTime.Now;
    }

    public ITimer NewTimer()
    {
        var fakeTimer = new FakeTimer();
        Timers.Add(fakeTimer);
        return fakeTimer;
    }

    public void Freeze()
    {
        _frozenTime = DateTime.Now;
    }

    public void Advance(int milliseconds)
    {
        _frozenTime = Now().AddMilliseconds(milliseconds);
        foreach (var t in Timers)
        {
            t.Advance(milliseconds);
        }
    }

    public void Unfreeze()
    {
        _frozenTime = null;
    }
}

public class FakeTimer : ITimer
{
    public void Advance(int milliseconds)
    {
        ElapsedMilliseconds += milliseconds;
    }

    public long ElapsedMilliseconds { get; private set; }
    public void Reset()
    {
        ElapsedMilliseconds = 0;
    }

    public void Start()
    {
    }
}

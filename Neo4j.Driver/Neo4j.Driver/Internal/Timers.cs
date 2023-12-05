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
using System.Diagnostics;
using Neo4j.Driver.Internal.Services;

namespace Neo4j.Driver.Internal;

internal interface ITimer
{
    /// <summary>Gets the total elapsed time measured by the current instance, in milliseconds.</summary>
    /// <returns>A read-only long integer representing the total number of milliseconds measured by the current instance.</returns>
    /// <filterpriority>1</filterpriority>
    long ElapsedMilliseconds { get; }

    /// <summary>Stops time interval measurement and resets the elapsed time to zero.</summary>
    void Reset();

    /// <summary>Starts, or resumes, measuring elapsed time for an interval.</summary>
    void Start();
}

internal class StopwatchBasedTimer : ITimer
{
    private readonly Stopwatch _stopwatch = new();

    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

    public void Reset()
    {
        _stopwatch.Reset();
    }

    public void Start()
    {
        _stopwatch.Start();
    }
}

internal class DateTimeBasedTimer : ITimer
{
    private DateTime _startTime = DateTimeProvider.Instance.Now();

    public long ElapsedMilliseconds => (long)(DateTimeProvider.Instance.Now() - _startTime).TotalMilliseconds;

    public void Reset()
    {
        _startTime = DateTimeProvider.Instance.Now();
    }

    public void Start() => Reset();
}

// by using this class to create timers, we allow tests using FakeTime to use DateTimeBasedTimer
// so that the amount of time passed will be controlled by TestKit
internal static class TimerFactory
{
    private static Type _stopwatchType = typeof(StopwatchBasedTimer);

    internal static void SetTimerType<T>() where T : ITimer, new()
    {
        _stopwatchType = typeof(T);
    }

    internal static ITimer CreateTimer()
    {
        return (ITimer)Activator.CreateInstance(_stopwatchType);
    }
}

// Copyright (c) 2002-2018 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using HdrHistogram.Utilities;
using Neo4j.Driver.V1;
using TimeZoneConverter;

namespace Neo4j.Driver.Internal
{
    internal static class TemporalHelpers
    {
        public const int NanosecondsPerTick = 100;

        public static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        public static long ComputeDaysSinceEpoch(this DateTime date)
        {
            return (long)date.Subtract(Epoch).TotalDays;
        }

        public static long ComputeNanosOfDay(this TimeSpan time)
        {
            return time.Ticks * NanosecondsPerTick;
        }

        public static long ComputeSecondsSinceEpoch(long ticks)
        {
            return (ticks - Epoch.Ticks) / TimeSpan.TicksPerSecond;
        }

        public static int ComputeNanosOfSecond(long ticks)
        {
            return (int)((ticks - Epoch.Ticks) % TimeSpan.TicksPerSecond) * NanosecondsPerTick;
        }

        public static long ComputeNanosOfDay(int hour, int minute, int second, int nanoOfSecond)
        {
            return ((hour * TimeSpan.TicksPerHour + minute * TimeSpan.TicksPerMinute +
                     second * TimeSpan.TicksPerSecond) * NanosecondsPerTick) + nanoOfSecond;
        }

        public static DateTime ComputeDate(long epochDays)
        {
            return Epoch.AddDays(epochDays);
        }

        public static DateTime ComputeDateTime(long epochSeconds, int nanosOfSecond)
        {
            return Epoch.AddSeconds(epochSeconds).AddTicks(nanosOfSecond / 100);
        }

        public static TimeZoneInfo GetTimeZoneInfo(string zoneId)
        {
            try
            {
                return TZConvert.GetTimeZoneInfo(zoneId);
            }
            catch (Exception exc)
            {
                throw new ProtocolException($"The given time zone identifier ({zoneId}) is not recognized.");
            }
        }

    }
}
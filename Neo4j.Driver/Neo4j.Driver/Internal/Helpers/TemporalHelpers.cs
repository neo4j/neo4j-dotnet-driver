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
        public static readonly long EpochSeconds = new DateTime(1970, 1, 1).Ticks / TimeSpan.TicksPerSecond;

        public static long DaysSinceEpoch(this DateTime date)
        {
            return (long)date.Subtract(Epoch).TotalDays;
        }

        public static long NanosOf(this TimeSpan time)
        {
            return time.Ticks * NanosecondsPerTick;
        }

        public static long SecondsSinceEpoch(long ticks)
        {
            return (ticks / TimeSpan.TicksPerSecond) - EpochSeconds;
        }

        public static int NanosOfSecond(long ticks)
        {
            return (int)((ticks % TimeSpan.TicksPerSecond) * NanosecondsPerTick);
        }

        public static long NanosOf(int hour, int minute, int second, int nanoOfSecond)
        {
            return ((hour * TimeSpan.TicksPerHour + minute * TimeSpan.TicksPerMinute +
                     second * TimeSpan.TicksPerSecond) * NanosecondsPerTick) + nanoOfSecond;
        }

        public static DateTime DateOf(long epochDays)
        {
            return Epoch.AddDays(epochDays);
        }

        public static TimeSpan TimeOf(long nanosOfDay, bool throwOnTruncate = false)
        {
            if (throwOnTruncate && nanosOfDay % NanosecondsPerTick != 0)
            {
                throw new TruncationException(
                    $"Conversion of the incoming data into TimeSpan will cause a truncation of ${nanosOfDay % NanosecondsPerTick}ns.");
            }

            return new TimeSpan(nanosOfDay / NanosecondsPerTick);
        }

        public static DateTime DateTimeOf(long epochSeconds, int nanosOfSecond, DateTimeKind kind = DateTimeKind.Local, bool throwOnTruncate = false)
        {
            if (throwOnTruncate && nanosOfSecond % NanosecondsPerTick != 0)
            {
                throw new TruncationException(
                    $"Conversion of the incoming data into DateTime will cause a truncation of ${nanosOfSecond % NanosecondsPerTick}ns.");
            }

            var result = Epoch.AddSeconds(epochSeconds).AddTicks(nanosOfSecond / 100);
            if (result.Kind != kind)
            {
                result = new DateTime(result.Ticks, kind);
            }

            return result;
        }

        public static DateTime AddNanosOfSecond(this DateTime dateTime, int nanosOfSecond, bool throwOnTruncate = false)
        {
            if (throwOnTruncate && nanosOfSecond % NanosecondsPerTick != 0)
            {
                throw new TruncationException(
                    $"Conversion of the incoming data into DateTime will cause a truncation of ${nanosOfSecond % NanosecondsPerTick}ns.");
            }

            return dateTime.AddTicks(nanosOfSecond / 100);
        }

        public static TimeZoneInfo GetTimeZoneInfo(string zoneId)
        {
            return TZConvert.GetTimeZoneInfo(zoneId);
        }
    }
}
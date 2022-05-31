// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
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
using Neo4j.Driver.Internal.Temporal;
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver.Internal
{
    internal static class TemporalHelpers
    {
        public const int MinYear = -999_999_999;
        public const int MaxYear = 999_999_999;
        public const int MinMonth = 1;
        public const int MaxMonth = 12;
        public const int MinDay = 1;
        public const int MaxDay = 31;
        public const int MaxHour = 23;
        public const int MinHour = 0;
        public const int MaxMinute = 59;
        public const int MinMinute = 0;
        public const int MaxSecond = 59;
        public const int MinSecond = 0;
        public const int MaxNanosecond = 999_999_999;
        public const int MinNanosecond = 0;
        public const int MinOffset = -64_800;
        public const int MaxOffset = 64_800;

        public const long NanosPerMillisecond = 1_000_000;
        public const long NanosPerSecond = 1_000_000_000;
        public const long NanosPerDay = NanosPerHour * HoursPerDay;

        private const int HoursPerDay = 24;
        private const int MinutesPerHour = 60;
        private const int SecondsPerMinute = 60;
        private const int SecondsPerHour = SecondsPerMinute * MinutesPerHour;
        private const int SecondsPerDay = SecondsPerHour * HoursPerDay;
        private const long NanosPerMinute = NanosPerSecond * SecondsPerMinute;
        private const long NanosPerHour = NanosPerMinute * MinutesPerHour;

        private const long Days0000To1970 = (DaysPerCycle * 5L) - (30L * 365L + 7L);
        private const int DaysPerCycle = 146_097;
        private const int NanosecondsPerTick = 100;

        public static long ToNanoOfDay(this IHasTimeComponents time)
        {
            return (time.Hour * NanosPerHour) + (time.Minute * NanosPerMinute) + (time.Second * NanosPerSecond) +
                   time.Nanosecond;
        }

#if NET6_0_OR_GREATER
        public static long ToNanoOfDay(this TimeOnly time)
        {
            return (time.Hour * NanosPerHour) + (time.Minute * NanosPerMinute) + (time.Second * NanosPerSecond) +
                   (time.Millisecond * NanosPerMillisecond);
        }

        public static long ToEpochDays(this DateOnly date)
        {
            return ComputeEpochDays(date.Year, date.Month, date.Day);
        }
#endif

        public static long ToEpochSeconds(this IHasDateTimeComponents dateTime)
        {
            var epochDays = dateTime.ToEpochDays();
            var timeSeconds = dateTime.ToSecondsOfDay();

            return epochDays * SecondsPerDay + timeSeconds;
        }

        public static int ToSecondsOfDay(this IHasTimeComponents time)
        {
            return (time.Hour * SecondsPerHour) + (time.Minute * SecondsPerMinute) + time.Second;
        }

        public static long ToEpochDays(this IHasDateComponents date)
        {
            return ComputeEpochDays(date.Year, date.Month, date.Day);
        }


        public static long GetDays(this Duration duration)
        {
            return (duration.Months * 30) + duration.Days + FloorDiv(duration.Seconds, SecondsPerDay);
        }

        public static long GetNanos(this Duration duration)
        {
            return FloorMod(duration.Seconds, SecondsPerDay) * TemporalHelpers.NanosPerSecond + duration.Nanos;
        }

        public static LocalTime NanoOfDayToTime(long nanoOfDay)
        {
            ComponentsOfNanoOfDay(nanoOfDay, out var hour, out var minute, out var second, out var nanosecond);

            return new LocalTime(hour, minute, second, nanosecond);
        }

        public static LocalDate EpochDaysToDate(long epochDays)
        {
            ComponentsOfEpochDays(epochDays, out var year, out var month, out var day);

            return new LocalDate(year, month, day);
        }

        public static LocalDateTime EpochSecondsAndNanoToDateTime(long epochSeconds, int nano)
        {
            var epochDay = FloorDiv(epochSeconds, SecondsPerDay);
            var secondsOfDay = FloorMod(epochSeconds, SecondsPerDay);
            var nanoOfDay = secondsOfDay * NanosPerSecond + nano;

            ComponentsOfEpochDays(epochDay, out var year, out var month, out var day);
            ComponentsOfNanoOfDay(nanoOfDay, out var hour, out var minute, out var second, out var nanosecond);

            return new LocalDateTime(year, month, day, hour, minute, second, nanosecond);
        }

        private static long ComputeEpochDays(int year, int month, int day)
        {
            var y = (long) year;
            var m = (long) month;
            var total = 0L;

            total += y * 365;
            if (y >= 0)
            {
                total += (y + 3) / 4 - (y + 99) / 100 + (y + 399) / 400;
            }
            else
            {
                total -= y / -4 - y / -100 + y / -400;
            }

            total += ((367 * m - 362) / 12);
            total += day - 1;
            if (m > 2)
            {
                total -= 1;
                if (IsLeapYear(year) == false)
                {
                    total -= 1;
                }
            }

            return total - Days0000To1970;
        }

        private static void ComponentsOfNanoOfDay(long nanoOfDay, out int hour, out int minute, out int second, out int nanosecond)
        {
            hour = (int)(nanoOfDay / NanosPerHour);
            nanoOfDay -= hour * NanosPerHour;

            minute = (int)(nanoOfDay / NanosPerMinute);
            nanoOfDay -= minute * NanosPerMinute;

            second = (int)(nanoOfDay / NanosPerSecond);
            nanosecond = (int)(nanoOfDay - second * NanosPerSecond);
        }

        private static void ComponentsOfEpochDays(long epochDays, out int year, out int month, out int day)
        {
            var zeroDay = (epochDays + Days0000To1970) - 60;
            var adjust = 0L;
            if (zeroDay < 0)
            {
                var adjustCycles = (zeroDay + 1) / DaysPerCycle - 1;
                adjust = adjustCycles * 400;
                zeroDay -= adjustCycles * DaysPerCycle;
            }

            var yearEst = (400 * zeroDay + 591) / DaysPerCycle;
            var dayOfYearEst = zeroDay - (365 * yearEst + yearEst / 4 - yearEst / 100 + yearEst / 400);
            if (dayOfYearEst < 0)
            {
                yearEst -= 1;
                dayOfYearEst = zeroDay - (365 * yearEst + yearEst / 4 - yearEst / 100 + yearEst / 400);
            }
            yearEst += adjust;
            var marchDayOfYear0 = (int)dayOfYearEst;

            var marchMonth0 = (marchDayOfYear0 * 5 + 2) / 153;
            month = (marchMonth0 + 2) % 12 + 1;
            day = marchDayOfYear0 - (marchMonth0 * 306 + 5) / 10 + 1;
            yearEst += marchMonth0 / 10;
            year = (int)yearEst;
        }

        public static int ExtractNanosecondFromTicks(long ticks)
        {
            return (int)((ticks % TimeSpan.TicksPerSecond) * NanosecondsPerTick);
        }

        public static int ExtractTicksFromNanosecond(int nanosecond)
        {
            return nanosecond / NanosecondsPerTick;
        }

        public static int MaxDayOfMonth(int year, int month)
        {
            switch (month)
            {
                case 2:
                    return IsLeapYear(year) ? 29 : 28;
                case 4:
                case 6:
                case 9:
                case 11:
                    return 30;
                default:
                    return 31;
            }
        }

        public static void AssertNoOverflow(IHasDateComponents date, string target)
        {
            if (date.Year > DateTime.MaxValue.Year || date.Year < DateTime.MinValue.Year)
            {
                throw new ValueOverflowException($"Year component ({date.Year}) of this instance is not valid for a {target} instance.");
            }
        }
#if NET6_0_OR_GREATER
        public static void AssertValidDateOnly(IHasDateComponents date)
        {
            if (date.Year > DateOnly.MaxValue.Year || date.Year < DateOnly.MinValue.Year)
            {
                throw new ValueOverflowException($"Year component ({date.Year}) of this instance is not valid for a DateOnly instance.");
            }
        }
#endif
        public static void AssertNoOverflow(TimeSpan offset, string target)
        {
            if (Math.Abs(offset.TotalHours) > 14)
            {
                throw new ValueOverflowException($"{target} expects Offset values to be in range [-14, 14] hours.");
            }
        }

        public static void AssertNoTruncation(IHasTimeComponents time, string target)
        {
            if (time.Nanosecond % NanosecondsPerTick > 0)
            {
                throw new ValueTruncationException(
                    $"Conversion of this instance ({time}) into {target} will cause a truncation of {time.Nanosecond % TemporalHelpers.NanosecondsPerTick}ns.");
            }
        }

        public static void AssertNoTruncation(TimeSpan offset, string target)
        {
            if (offset.Ticks % TimeSpan.TicksPerMinute != 0)
            {
                throw new ValueTruncationException(
                    $"{target} expects Offset values to be in minutes precision. Use of this instance ({offset}) as an offset will cause a truncation of {offset.Ticks % TimeSpan.TicksPerMinute}ns.");
            }
        }

        public static string ToIsoDurationString(long months, long days, long seconds, int nanoseconds)
        {
            var timePart = string.Empty;

            if (seconds < 0 && nanoseconds > 0)
            {
                seconds = seconds + 1;
                nanoseconds = (int)NanosPerSecond - nanoseconds;

                if (seconds == 0)
                {
                    timePart = "-";
                }
            }

            if (nanoseconds == 0)
            {
                timePart = $"{timePart}{seconds}";
            }
            else
            {
                timePart = $"{timePart}{seconds}.{nanoseconds:D9}";
            }

            return $"P{months}M{days}DT{timePart}S";
        }

        public static string ToIsoDateString(int year, int month, int day)
        {
            return $"{year:D4}-{month:D2}-{day:D2}";
        }

        public static string ToIsoTimeString(int hour, int minute, int second, int nanosecond)
        {
            var nanosecondPart = string.Empty;
            if (nanosecond > 0)
            {
                nanosecondPart = $".{nanosecond:D9}";
            }

            return $"{hour:D2}:{minute:D2}:{second:D2}{nanosecondPart}";
        }

        public static string ToIsoTimeZoneId(string id)
        {
            return $"[{id}]";
        }

        public static string ToIsoTimeZoneOffset(int offsetSeconds)
        {
            if (offsetSeconds == 0)
            {
                return "Z";
            }

            var offset = TimeSpan.FromSeconds(offsetSeconds);

            var sign = "+";
            if (offsetSeconds < 0)
            {
                offset = offset.Negate();
                sign = "-";
            }

            return offset.Seconds != 0 ? $"{sign}{offset.Hours:D2}:{offset.Minutes:D2}:{offset.Seconds:D2}" : $"{sign}{offset.Hours:D2}:{offset.Minutes:D2}";
        }

        public static TimeZoneInfo GetTimeZoneInfo(string zoneId)
        {
            return TimeZoneMapping.Get(zoneId);
        }

        private static bool IsLeapYear(long year)
        {
            return ((year & 3) == 0) && ((year % 100) != 0 || (year % 400) == 0);
        }

        private static long FloorDiv(long a, long b)
        {
            return a >= 0 ? a / b : ((a + 1) / b) - 1;
        }

        private static long FloorMod(long a, long b)
        {
            return ((a % b) + b) % b;
        }

        public static int NanosecondToMillisecond(int nanosecond)
        {
            return (int)(nanosecond / NanosPerMillisecond);
        }
    }
}
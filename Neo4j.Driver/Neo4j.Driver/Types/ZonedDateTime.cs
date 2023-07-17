// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver;

/// <summary>Represents a date time value with a time zone, specified as a UTC offset</summary>
public sealed class ZonedDateTime : TemporalValue,
    IEquatable<ZonedDateTime>,
    IComparable,
    IComparable<ZonedDateTime>,
    IHasDateTimeComponents
{
    /// <summary>
    /// Used by the driver to explain the cause of a <see cref="ZonedDateTime"/>'s
    /// <see cref="ZonedDateTime.Ambiguous"/> being true.
    /// </summary>
    [Flags]
    public enum AmbiguityReason
    {
        /// <summary>No ambiguity.</summary>
        None,

        /// <summary>The lookup of the offset will be completed with a local time which may result an ambiguous value.</summary>
        ZoneIdLookUpWithLocalTime,

        /// <summary>The datetime kind is unspecified, it will be treated as local time.</summary>
        UnspecifiedDateTimeKind,

        /// <summary>
        /// The lookup of the offset will be completed with a value truncated to the range of BCL date/time types meaning
        /// that any rules that may apply outside of the BCL date/time type range are not applied.
        /// </summary>
        RuleLookupTruncatedToClrRange
    }

    /// <summary>Default comparer for <see cref="ZonedDateTime"/> values.</summary>
    public static readonly IComparer<ZonedDateTime> Comparer = new TemporalValueComparer<ZonedDateTime>();

    /// <summary>Used for storing offset in seconds.</summary>
    private readonly int? _offsetSeconds;

    /// <summary>
    /// Create a new instance of <see cref="ZonedDateTime"/> using delta from unix epoch (1970-1-1 00:00:00.00 UTC). <br/>
    /// Allows handling values in range for neo4j and outside of the range of BCL date/time types (<see cref="DateTime"/>,
    /// <see cref="DateTimeOffset"/>).
    /// <remarks>
    /// When <paramref name="utcSeconds"/> is outside of BCL date/time types range (-62_135_596_800, 253_402_300_799)
    /// and <paramref name="zone"/> is a <see cref="ZoneId"/> the <see cref="ZonedDateTime"/> instance will be marked as
    /// <see cref="Ambiguous"/>.
    /// </remarks>
    /// <remarks></remarks>
    /// </summary>
    /// <param name="utcSeconds">Seconds from unix epoch (1970-1-1 00:00:00.00 UTC).</param>
    /// <param name="nanos">Nanoseconds of the second.</param>
    /// <param name="zone">Zone for offsetting utc to local.</param>
    public ZonedDateTime(long utcSeconds, int nanos, Zone zone)
    {
        if (utcSeconds is > TemporalHelpers.MaxUtcForZonedDateTime or < TemporalHelpers.MinUtcForZonedDateTime)
        {
            throw new ArgumentOutOfRangeException(nameof(utcSeconds));
        }

        UtcSeconds = utcSeconds;
        Nanosecond = nanos;
        Zone = zone ?? throw new ArgumentNullException(nameof(zone));

        if (zone is ZoneOffset zo)
        {
            _offsetSeconds = zo.OffsetSeconds;
            var local = TemporalHelpers.EpochSecondsAndNanoToDateTime(utcSeconds + zo.OffsetSeconds, Nanosecond);
            Year = local.Year;
            Month = local.Month;
            Day = local.Day;
            Hour = local.Hour;
            Minute = local.Minute;
            Second = local.Second;
        }
        else
        {
            if (utcSeconds is < TemporalHelpers.DateTimeOffsetMinSeconds
                or > TemporalHelpers.DateTimeOffsetMaxSeconds)
            {
                SetAmbiguous(AmbiguityReason.RuleLookupTruncatedToClrRange);
            }

            var utc = TemporalHelpers.EpochSecondsAndNanoToDateTime(utcSeconds, Nanosecond);
            try
            {
                var offset = zone.OffsetSecondsAt(ClrFriendly(utc));
                _offsetSeconds = offset;
                var local = TemporalHelpers.EpochSecondsAndNanoToDateTime(utcSeconds + offset, Nanosecond);
                Year = local.Year;
                Month = local.Month;
                Day = local.Day;
                Hour = local.Hour;
                Minute = local.Minute;
                Second = local.Second;
            }
            catch (TimeZoneNotFoundException)
            {
                UnknownZoneInfo = true;
            }
        }
    }

    /// <summary>
    /// Create a new instance of <see cref="ZonedDateTime"/> using delta from unix epoch (1970-1-1 00:00:00.00 UTC) in ticks.
    /// <br/> Allows handling values in range for neo4j and outside of the range of BCL date types (<see cref="DateTime"/>,
    /// <see cref="DateTimeOffset"/>).
    /// <remarks>
    /// When <paramref name="ticks"/> is outside of BCL date ranges (-621_355_968_000_000_000,
    /// 2_534_023_009_990_000_000) and <paramref name="zone"/> is a <see cref="ZoneId"/> the <see cref="ZonedDateTime"/>
    /// instance will be marked as <see cref="Ambiguous"/>.
    /// </remarks>
    /// </summary>
    /// <param name="ticks">ticks from unix epoch (1970-1-1 00:00:00.00 UTC).</param>
    /// <param name="zone">Zone for offsetting utc to local.</param>
    public ZonedDateTime(long ticks, Zone zone) : this(
        ticks / TimeSpan.TicksPerSecond,
        (int)(ticks % TimeSpan.TicksPerSecond * 100),
        zone)
    {
    }

    /// <summary>Initializes a new instance of <see cref="ZonedDateTime"/> from given <see cref="DateTimeOffset"/> value.</summary>
    /// <param name="dateTimeOffset"></param>
    public ZonedDateTime(DateTimeOffset dateTimeOffset)
    {
        UtcSeconds = dateTimeOffset.ToUnixTimeSeconds();
        Nanosecond = TemporalHelpers.ExtractNanosecondFromTicks(dateTimeOffset.UtcTicks);
        Zone = Zone.Of((int)dateTimeOffset.Offset.TotalSeconds);

        _offsetSeconds = (int)dateTimeOffset.Offset.TotalSeconds;

        Year = dateTimeOffset.Year;
        Month = dateTimeOffset.Month;
        Day = dateTimeOffset.Day;
        Hour = dateTimeOffset.Hour;
        Minute = dateTimeOffset.Minute;
        Second = dateTimeOffset.Second;
    }

    /// <summary>Initializes a new instance of <see cref="ZonedDateTime"/> from given <see cref="DateTime"/> value.</summary>
    /// <param name="dateTime"></param>
    /// <param name="offset"></param>
    public ZonedDateTime(DateTime dateTime, TimeSpan offset)
    {
        Zone = Zone.Of((int)offset.TotalSeconds);
        Nanosecond = TemporalHelpers.ExtractNanosecondFromTicks(dateTime.Ticks);
        _offsetSeconds = (int)offset.TotalSeconds;

        if (dateTime.Kind == DateTimeKind.Utc)
        {
            var dto = new DateTimeOffset(dateTime);
            UtcSeconds = dto.ToUnixTimeSeconds();
            var local = dto.AddSeconds(_offsetSeconds.Value);
            Year = local.Year;
            Month = local.Month;
            Day = local.Day;
            Hour = local.Hour;
            Minute = local.Minute;
            Second = local.Second;
        }
        else
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                SetAmbiguous(AmbiguityReason.UnspecifiedDateTimeKind);
            }

            var dto = new DateTimeOffset(
                new DateTime(
                    dateTime.AddSeconds(-_offsetSeconds.Value).Ticks,
                    DateTimeKind.Utc));

            UtcSeconds = dto.ToUnixTimeSeconds();
            Year = dateTime.Year;
            Month = dateTime.Month;
            Day = dateTime.Day;
            Hour = dateTime.Hour;
            Minute = dateTime.Minute;
            Second = dateTime.Second;
        }
    }

    /// <summary>Initializes a new instance of <see cref="ZonedDateTime"/> from given <see cref="DateTime"/> value.</summary>
    /// <param name="dateTime"></param>
    /// <param name="offsetSeconds"></param>
    public ZonedDateTime(DateTime dateTime, int offsetSeconds)
    {
        Zone = Zone.Of(offsetSeconds);
        Nanosecond = TemporalHelpers.ExtractNanosecondFromTicks(dateTime.Ticks);
        _offsetSeconds = offsetSeconds;

        if (dateTime.Kind == DateTimeKind.Utc)
        {
            var dto = new DateTimeOffset(dateTime);
            UtcSeconds = dto.ToUnixTimeSeconds();
            var local = dto.AddSeconds(offsetSeconds);

            Year = local.Year;
            Month = local.Month;
            Day = local.Day;
            Hour = local.Hour;
            Minute = local.Minute;
            Second = local.Second;
        }
        else if (dateTime.Kind == DateTimeKind.Local)
        {
            var dto = new DateTimeOffset(new DateTime(dateTime.AddSeconds(-offsetSeconds).Ticks, DateTimeKind.Utc));
            UtcSeconds = dto.ToUnixTimeSeconds();
            Year = dateTime.Year;
            Month = dateTime.Month;
            Day = dateTime.Day;
            Hour = dateTime.Hour;
            Minute = dateTime.Minute;
            Second = dateTime.Second;
        }
        else
        {
            SetAmbiguous(AmbiguityReason.UnspecifiedDateTimeKind);
            var dto = new DateTimeOffset(new DateTime(dateTime.AddSeconds(-offsetSeconds).Ticks, DateTimeKind.Utc));
            UtcSeconds = dto.ToUnixTimeSeconds();
            Year = dateTime.Year;
            Month = dateTime.Month;
            Day = dateTime.Day;
            Hour = dateTime.Hour;
            Minute = dateTime.Minute;
            Second = dateTime.Second;
        }
    }

    /// <summary>Initializes a new instance of <see cref="ZonedDateTime"/> from given <see cref="DateTime"/> value.</summary>
    /// <param name="dateTime">Date time instance, should be local or utc.</param>
    /// <param name="zoneId">Zone </param>
    /// <exception cref="TimeZoneNotFoundException"></exception>
    public ZonedDateTime(DateTime dateTime, string zoneId)
    {
        Zone = zoneId != null ? Zone.Of(zoneId) : throw new ArgumentNullException(nameof(zoneId));
        Nanosecond = TemporalHelpers.ExtractNanosecondFromTicks(dateTime.Ticks);

        if (dateTime.Kind == DateTimeKind.Utc)
        {
            var dto = new DateTimeOffset(dateTime);
            UtcSeconds = dto.ToUnixTimeSeconds();
            try
            {
                var local = dto.ToOffset(LookupOffsetAt(dto.UtcDateTime));
                _offsetSeconds = (int)local.Offset.TotalSeconds;
                Year = local.Year;
                Month = local.Month;
                Day = local.Day;
                Hour = local.Hour;
                Minute = local.Minute;
                Second = local.Second;
            }
            catch (TimeZoneNotFoundException)
            {
                UnknownZoneInfo = true;
            }
        }
        else
        {
            SetAmbiguous(
                dateTime.Kind == DateTimeKind.Unspecified
                    ? AmbiguityReason.UnspecifiedDateTimeKind | AmbiguityReason.ZoneIdLookUpWithLocalTime
                    : AmbiguityReason.ZoneIdLookUpWithLocalTime);

            var dto = new DateTimeOffset(dateTime, LookupOffsetAt(dateTime));
            UtcSeconds = dto.ToUnixTimeSeconds();
            _offsetSeconds = (int)dto.Offset.TotalSeconds;
            var local = dateTime;
            Year = local.Year;
            Month = local.Month;
            Day = local.Day;
            Hour = local.Hour;
            Minute = local.Minute;
            Second = local.Second;
        }
    }

    /// <summary>Initializes a new instance of <see cref="ZonedDateTime"/> from individual local date time component values.</summary>
    /// <param name="year">Local year value.</param>
    /// <param name="month">Local month value.</param>
    /// <param name="day">Local day value.</param>
    /// <param name="hour">Local hour value.</param>
    /// <param name="minute">Local minute value.</param>
    /// <param name="second">Local second value.</param>
    /// <param name="zone"></param>
    [Obsolete("Deprecated, This constructor does not support a kind, so not known if utc or local.")]
    public ZonedDateTime(int year, int month, int day, int hour, int minute, int second, Zone zone)
        : this(year, month, day, hour, minute, second, 0, zone)
    {
    }

    /// <summary>Initializes a new instance of <see cref="ZonedDateTime"/> from individual local date time component values.</summary>
    /// <param name="year">Year in Locale.</param>
    /// <param name="month">Month in Locale.</param>
    /// <param name="day"></param>
    /// <param name="hour"></param>
    /// <param name="minute"></param>
    /// <param name="second"></param>
    /// <param name="nanosecond"></param>
    /// <param name="zone"></param>
    [Obsolete("Deprecated, This constructor does not support a kind, so not known if utc or local.")]
    public ZonedDateTime(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int nanosecond,
        Zone zone)
    {
        ValidateComponents(year, month, day, hour, minute, second, nanosecond);

        zone = zone ?? throw new ArgumentNullException(nameof(zone));

        SetAmbiguous(AmbiguityReason.UnspecifiedDateTimeKind);

        Zone = zone;
        Nanosecond = nanosecond;

        Year = year;
        Month = month;
        Day = day;
        Hour = hour;
        Minute = minute;
        Second = second;

        if (zone is ZoneOffset zo)
        {
            _offsetSeconds = zo.OffsetSeconds;
            var epoch = new LocalDateTime(year, month, day, hour, minute, second, nanosecond).ToEpochSeconds();
            UtcSeconds = epoch - _offsetSeconds.Value;
        }
        else
        {
            if (Year is > 9999 or < 1)
            {
                SetAmbiguous(
                    AmbiguityReason.UnspecifiedDateTimeKind |
                    AmbiguityReason.ZoneIdLookUpWithLocalTime |
                    AmbiguityReason.RuleLookupTruncatedToClrRange);
            }
            else
            {
                SetAmbiguous(AmbiguityReason.UnspecifiedDateTimeKind | AmbiguityReason.ZoneIdLookUpWithLocalTime);
            }

            var local = new LocalDateTime(year, month, day, hour, minute, second, nanosecond);
            var offset = LookupOffsetAt(ClrFriendly(local));
            _offsetSeconds = (int)offset.TotalSeconds;
            UtcSeconds = local.ToEpochSeconds() - _offsetSeconds.Value;
        }
    }

    /// <summary>
    /// Deprecated Constructor for internal use only. Can be removed when <see cref="ZonedDateTimeSerializer"/> is
    /// removed.
    /// </summary>
    /// <param name="dateTime"></param>
    /// <param name="zone"></param>
    internal ZonedDateTime(IHasDateTimeComponents dateTime, Zone zone)
    {
        zone = zone ?? throw new ArgumentNullException(nameof(zone));

        SetAmbiguous(AmbiguityReason.UnspecifiedDateTimeKind);

        Zone = zone;
        Nanosecond = dateTime.Nanosecond;

        Year = dateTime.Year;
        Month = dateTime.Month;
        Day = dateTime.Day;
        Hour = dateTime.Hour;
        Minute = dateTime.Minute;
        Second = dateTime.Second;
        ValidateComponents(Year, Month, Day, Hour, Minute, Second, Nanosecond);

        if (zone is ZoneOffset zo)
        {
            _offsetSeconds = zo.OffsetSeconds;
            var epoch = new LocalDateTime(Year, Month, Day, Hour, Minute, Second, Nanosecond).ToEpochSeconds();
            UtcSeconds = epoch - _offsetSeconds.Value;
        }
        else
        {
            if (Year is > 9999 or < 1)
            {
                SetAmbiguous(
                    AmbiguityReason.UnspecifiedDateTimeKind |
                    AmbiguityReason.ZoneIdLookUpWithLocalTime |
                    AmbiguityReason.RuleLookupTruncatedToClrRange);
            }
            else
            {
                SetAmbiguous(AmbiguityReason.UnspecifiedDateTimeKind | AmbiguityReason.ZoneIdLookUpWithLocalTime);
            }

            var local = new LocalDateTime(Year, Month, Day, Hour, Minute, Second, Nanosecond);
            var offset = LookupOffsetAt(ClrFriendly(local));
            _offsetSeconds = (int)offset.TotalSeconds;
            UtcSeconds = local.ToEpochSeconds() - _offsetSeconds.Value;
        }
    }

    public bool UnknownZoneInfo { get; set; }

    /// <summary>Reason why this instance is could be ambiguous.</summary>
    public AmbiguityReason Reason { get; private set; } = AmbiguityReason.None;

    /// <summary>Gets if this instance is has a possible ambiguity.</summary>
    public bool Ambiguous { get; private set; }

    /// <summary>
    /// Gets the number of seconds since the Unix Epoch (00:00:00 UTC, Thursday, 1 January 1970). <br/> Introduced in
    /// 4.4.1 a fix to a long standing issue of not having a monotonic datetime used on construction or transmission.
    /// </summary>
    public long UtcSeconds { get; }

    /// <summary>
    /// Gets the number of Ticks from the Unix Epoch (00:00:00 UTC, Thursday, 1 January 1970). Truncates
    /// <see cref="Nanosecond"/> to closest tick.
    /// </summary>
    public long EpochTicks =>
        UtcSeconds * TimeSpan.TicksPerSecond + TemporalHelpers.ExtractTicksFromNanosecond(Nanosecond);

    /// <summary>The time zone that this instance represents.</summary>
    public Zone Zone { get; }

    /// <summary>Gets a <see cref="DateTime"/> value that represents the local date and time of this instance.</summary>
    /// <exception cref="TimeZoneNotFoundException">If <see cref="Zone"/> is not known locally.</exception>
    /// <exception cref="ValueOverflowException">If the value cannot be represented with DateTime</exception>
    /// <exception cref="ValueTruncationException">If a truncation occurs during conversion</exception>
    public DateTime LocalDateTime
    {
        get
        {
            if (UnknownZoneInfo)
            {
                throw new TimeZoneNotFoundException();
            }

            TemporalHelpers.AssertNoTruncation(this, nameof(DateTime));
            TemporalHelpers.AssertNoOverflow(this, nameof(DateTime));

            return new DateTime(Year, Month, Day, Hour, Minute, Second, DateTimeKind.Local)
                .AddTicks(TemporalHelpers.ExtractTicksFromNanosecond(TruncatedNanos()));
        }
    }

    /// <summary>Gets a <see cref="DateTime"/> the UTC value that represents the date and time of this instance.</summary>
    /// <exception cref="ValueOverflowException">If the value cannot be represented with DateTime</exception>
    /// <exception cref="ValueTruncationException">If a truncation occurs during conversion</exception>
    public DateTime UtcDateTime
    {
        get
        {
            DateTimeOffset dto;
            try
            {
                dto = DateTimeOffset.FromUnixTimeSeconds(UtcSeconds);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ValueOverflowException(
                    $"The value of {nameof(UtcSeconds)} is too large or small to be represented by {nameof(DateTime)}");
            }

            TemporalHelpers.AssertNoTruncation(this, nameof(DateTime));

            dto = dto.AddTicks(TemporalHelpers.ExtractTicksFromNanosecond(Nanosecond));

            return dto.UtcDateTime;
        }
    }

    /// <summary></summary>
    /// <exception cref="TimeZoneNotFoundException">If <see cref="Zone"/> is not known locally.</exception>
    public int OffsetSeconds => _offsetSeconds ?? LookupOffsetFromZone();

    /// <summary>Gets a <see cref="TimeSpan"/> value that represents the offset of this instance.</summary>
    private TimeSpan Offset => TimeSpan.FromSeconds(OffsetSeconds);

    /// <summary>
    /// Compares the value of this instance to a specified object which is expected to be a
    /// <see cref="ZonedDateTime"/> value, and returns an integer that indicates whether this instance is earlier than, the
    /// same as, or later than the specified <see cref="ZonedDateTime"/> value.
    /// </summary>
    /// <param name="obj">The object to compare to the current instance.</param>
    /// <returns>A signed number indicating the relative values of this instance and the value parameter.</returns>
    public int CompareTo(object obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (ReferenceEquals(this, obj))
        {
            return 0;
        }

        if (obj is not ZonedDateTime time)
        {
            throw new ArgumentException($"Object must be of type {nameof(ZonedDateTime)}");
        }

        return CompareTo(time);
    }

    /// <summary>
    /// Compares the value of this instance to a specified <see cref="ZonedDateTime"/> value and returns an integer
    /// that indicates whether this instance is earlier than, the same as, or later than the specified DateTime value.
    /// </summary>
    /// <param name="other">The object to compare to the current instance.</param>
    /// <returns>A signed number indicating the relative values of this instance and the value parameter.</returns>
    public int CompareTo(ZonedDateTime other)
    {
        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        if (other is null)
        {
            return 1;
        }

        var epochComparison = UtcSeconds.CompareTo(other.UtcSeconds);
        return epochComparison != 0 ? epochComparison : Nanosecond.CompareTo(other.Nanosecond);
    }

    /// <summary>
    /// Returns a value indicating whether the value of this instance is equal to the value of the specified
    /// <see cref="ZonedDateTime"/> instance.
    /// </summary>
    /// <param name="other">The object to compare to this instance.</param>
    /// <returns>
    /// <code>true</code> if the <code>value</code> parameter equals the value of this instance; otherwise,
    /// <code>false</code>
    /// </returns>
    public bool Equals(ZonedDateTime other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetHashCode() == other.GetHashCode();
    }

    /// <summary>Gets the year component of this instance in the Locale of <see cref="Zone"/>.</summary>
    public int Year { get; }

    /// <summary>Gets the month component of this instance in the Locale of <see cref="Zone"/>.</summary>
    public int Month { get; }

    /// <summary>Gets the day of month component of this instance in the Locale of <see cref="Zone"/>.</summary>
    public int Day { get; }

    /// <summary>Gets the hour component of this instance in the Locale of <see cref="Zone"/>.</summary>
    public int Hour { get; }

    /// <summary>Gets the minute component of this instance in the Locale of <see cref="Zone"/>.</summary>
    public int Minute { get; }

    /// <summary>Gets the second component of this instance in the Locale of <see cref="Zone"/>.</summary>
    public int Second { get; }

    /// <summary>Gets the nanosecond component of this instance.</summary>
    public int Nanosecond { get; }

    private static void ValidateComponents(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int nanosecond)
    {
        Throw.ArgumentOutOfRangeException.IfValueNotBetween(
            year,
            TemporalHelpers.MinYear,
            TemporalHelpers.MaxYear,
            nameof(year));

        Throw.ArgumentOutOfRangeException.IfValueNotBetween(
            month,
            TemporalHelpers.MinMonth,
            TemporalHelpers.MaxMonth,
            nameof(month));

        Throw.ArgumentOutOfRangeException.IfValueNotBetween(
            day,
            TemporalHelpers.MinDay,
            TemporalHelpers.MaxDayOfMonth(year, month),
            nameof(day));

        Throw.ArgumentOutOfRangeException.IfValueNotBetween(
            hour,
            TemporalHelpers.MinHour,
            TemporalHelpers.MaxHour,
            nameof(hour));

        Throw.ArgumentOutOfRangeException.IfValueNotBetween(
            minute,
            TemporalHelpers.MinMinute,
            TemporalHelpers.MaxMinute,
            nameof(minute));

        Throw.ArgumentOutOfRangeException.IfValueNotBetween(
            second,
            TemporalHelpers.MinSecond,
            TemporalHelpers.MaxSecond,
            nameof(second));

        Throw.ArgumentOutOfRangeException.IfValueNotBetween(
            nanosecond,
            TemporalHelpers.MinNanosecond,
            TemporalHelpers.MaxNanosecond,
            nameof(nanosecond));
    }

    private void SetAmbiguous(AmbiguityReason reason)
    {
        Ambiguous = true;
        Reason = reason;
    }

    private DateTime ClrFriendly(LocalDateTime local)
    {
        var abs = Math.Abs(local.Year);
        // we can only offset years that are in the CLR range, so we need to offset the year
        var year = abs % 4 == 0 && (abs % 100 != 0 || abs % 400 == 0)
            ? local.Year < 1
                ? 4 // first leap year in CLR type range
                : 9996 // last leap year in CLR type range
            : local.Year < 1
                ? 2 // first year that can safely be offset in CLR range, non-leap year.
                : 9998; // last year that can safely be offset in CLR range, non-leap year.

        return new DateTime(
            year,
            local.Month,
            local.Day,
            local.Hour,
            local.Minute,
            local.Second).AddTicks(TruncatedNanos());
    }

    private int TruncatedNanos()
    {
        return Nanosecond / 1_000_000 * 1_000_000;
    }

    private int LookupOffsetFromZone()
    {
        if (Zone is ZoneOffset zo)
        {
            return zo.OffsetSeconds;
        }

        var utc = DateTimeOffset.FromUnixTimeSeconds(UtcSeconds)
            .AddTicks(TemporalHelpers.ExtractTicksFromNanosecond(Nanosecond));

        return Zone.OffsetSecondsAt(utc.UtcDateTime);
    }

    private TimeSpan LookupOffsetAt(DateTime dateTime)
    {
        return TimeSpan.FromSeconds(Zone.OffsetSecondsAt(dateTime));
    }

    /// <summary>Converts this instance to an equivalent <see cref="DateTimeOffset"/> value</summary>
    /// <returns>Equivalent <see cref="DateTimeOffset"/> value</returns>
    /// <exception cref="ValueOverflowException">If the value cannot be represented with DateTimeOffset</exception>
    /// <exception cref="ValueTruncationException">If a truncation occurs during conversion</exception>
    public DateTimeOffset ToDateTimeOffset()
    {
        if (UnknownZoneInfo)
        {
            throw new TimeZoneNotFoundException();
        }

        TemporalHelpers.AssertNoTruncation(this, nameof(DateTimeOffset));
        TemporalHelpers.AssertNoOverflow(this, nameof(DateTimeOffset));

        var dto = DateTimeOffset.FromUnixTimeSeconds(UtcSeconds)
            .AddTicks(TemporalHelpers.ExtractTicksFromNanosecond(Nanosecond));

        return dto.ToOffset(Offset);
    }

    /// <summary>Returns a value indicating whether this instance is equal to a specified object.</summary>
    /// <param name="obj">The object to compare to this instance.</param>
    /// <returns>
    /// <code>true</code> if <code>value</code> is an instance of <see cref="ZonedDateTime"/> and equals the value of
    /// this instance; otherwise, <code>false</code>
    /// </returns>
    public override bool Equals(object obj)
    {
        return obj is ZonedDateTime dateTime && Equals(dateTime);
    }

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Nanosecond;
            hashCode = (hashCode * 397) ^ Zone.GetHashCode();
            hashCode = (hashCode * 397) ^ UtcSeconds.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>Converts the value of the current <see cref="ZonedDateTime"/> object to its equivalent string representation.</summary>
    /// <returns>String representation of this Point.</returns>
    public override string ToString()
    {
        if (UnknownZoneInfo)
        {
            return @$"{{UtcSeconds: {UtcSeconds}, Nanoseconds: {Nanosecond}, Zone: {Zone}}}";
        }

        var isoDate = TemporalHelpers.ToIsoDateString(Year, Month, Day);
        var isoTime = TemporalHelpers.ToIsoTimeString(Hour, Minute, Second, Nanosecond);
        return $"{isoDate}T{isoTime}{Zone}";
    }

    /// <summary>
    /// Determines whether one specified <see cref="ZonedDateTime"/> is earlier than another specified
    /// <see cref="ZonedDateTime"/>.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns><code>true</code> if one is earlier than another, otherwise <code>false</code>.</returns>
    public static bool operator <(ZonedDateTime left, ZonedDateTime right)
    {
        return left.CompareTo(right) < 0;
    }

    /// <summary>
    /// Determines whether one specified <see cref="ZonedDateTime"/> is later than another specified
    /// <see cref="ZonedDateTime"/>.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns><code>true</code> if one is later than another, otherwise <code>false</code>.</returns>
    public static bool operator >(ZonedDateTime left, ZonedDateTime right)
    {
        return left.CompareTo(right) > 0;
    }

    /// <summary>
    /// Determines whether one specified <see cref="ZonedDateTime"/> represents a duration that is the same as or
    /// later than the other specified <see cref="ZonedDateTime"/>
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns><code>true</code> if one is the same as or later than another, otherwise <code>false</code>.</returns>
    public static bool operator <=(ZonedDateTime left, ZonedDateTime right)
    {
        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// Determines whether one specified <see cref="ZonedDateTime"/> represents a duration that is the same as or
    /// earlier than the other specified <see cref="ZonedDateTime"/>
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns><code>true</code> if one is the same as or earlier than another, otherwise <code>false</code>.</returns>
    public static bool operator >=(ZonedDateTime left, ZonedDateTime right)
    {
        return left.CompareTo(right) >= 0;
    }

    /// <inheritdoc cref="TemporalValue.ConvertToDateTimeOffset"/>
    protected override DateTimeOffset ConvertToDateTimeOffset()
    {
        return ToDateTimeOffset();
    }
}

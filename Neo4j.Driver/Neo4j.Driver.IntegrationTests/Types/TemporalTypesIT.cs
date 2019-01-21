// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Types
{
    public class TemporalTypesIT: DirectDriverTestBase
    {
        private const int NumberOfRandomSequences = 2000;
        private const int MinArrayLength = 5;
        private const int MaxArrayLength = 1000;

        private readonly IEnumerable<string> _tzNames = new[]
        {
            "Africa/Harare", "America/Aruba", "Africa/Nairobi", "America/Dawson", "Asia/Beirut", "Asia/Tashkent",
            "Canada/Eastern", "Europe/Malta", "Europe/Volgograd", "Indian/Kerguelen", "Etc/GMT+3"
        };
        private readonly Random _random = new Random();

        public TemporalTypesIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {

        }

        #region Receive Only Tests

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveDuration()
        {
            TestReceiveData("RETURN duration({ months: 16, days: 45, seconds: 120, nanoseconds: 187309812 })",
                new Duration(16, 45, 120, 187309812));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveDate()
        {
            TestReceiveData("RETURN date({ year: 1994, month: 11, day: 15 })",
                new LocalDate(1994, 11, 15));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveTime()
        {
            TestReceiveData("RETURN localtime({ hour: 23, minute: 49, second: 59, nanosecond: 999999999 })",
                new LocalTime(23, 49, 59, 999999999));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveTimeWithOffset()
        {
            TestReceiveData(
                "RETURN time({ hour: 23, minute: 49, second: 59, nanosecond: 999999999, timezone:'+03:00' })",
                new OffsetTime(23, 49, 59, 999999999, (int)TimeSpan.FromHours(3).TotalSeconds));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveDateTime()
        {
            TestReceiveData(
                "RETURN localdatetime({ year: 1859, month: 5, day: 31, hour: 23, minute: 49, second: 59, nanosecond: 999999999 })",
                new LocalDateTime(1859, 5, 31, 23, 49, 59, 999999999));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveDateTimeWithOffset()
        {
            TestReceiveData(
                "RETURN datetime({ year: 1859, month: 5, day: 31, hour: 23, minute: 49, second: 59, nanosecond: 999999999, timezone:'+02:30' })",
                new ZonedDateTime(1859, 5, 31, 23, 49, 59, 999999999, Zone.Of((int)TimeSpan.FromMinutes(150).TotalSeconds)));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveDateTimeWithZoneId()
        {
            TestReceiveData(
                "RETURN datetime({ year: 1959, month: 5, day: 31, hour: 23, minute: 49, second: 59, nanosecond: 999999999, timezone:'Europe/London' })",
                new ZonedDateTime(1959, 5, 31, 23, 49, 59, 999999999, Zone.Of("Europe/London")));
        }

        #endregion

        #region Send and Receive Tests

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveDuration()
        {
            var data = new Duration(14, 35, 75, 789012587);

            TestSendAndReceiveData(
                "CYPHER runtime=interpreted WITH $x AS x RETURN x, x.months, x.days, x.seconds, x.millisecondsOfSecond, x.microsecondsOfSecond, x.nanosecondsOfSecond",
                data,
                new object[]
                {
                    data,
                    14L,
                    35L,
                    75L,
                    789L,
                    789012L,
                    789012587L
                });
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveDate()
        {
            var data = new LocalDate(1976, 6, 13);

            TestSendAndReceiveData(
                "CYPHER runtime = interpreted WITH $x AS x RETURN x, x.year, x.month, x.day",
                data,
                new object[]
                {
                    data,
                    1976L,
                    6L,
                    13L
                });
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveTime()
        {
            var data = new LocalTime(12, 34, 56, 789012587);

            TestSendAndReceiveData(
                "CYPHER runtime=interpreted WITH $x AS x RETURN x, x.hour, x.minute, x.second, x.millisecond, x.microsecond, x.nanosecond",
                data,
                new object[]
                {
                    data,
                    12L,
                    34L,
                    56L,
                    789L,
                    789012L,
                    789012587L
                });
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveTimeWithOffset()
        {
            var data = new OffsetTime(12, 34, 56, 789012587, (int)TimeSpan.FromMinutes(90).TotalSeconds);

            TestSendAndReceiveData(
                "CYPHER runtime=interpreted WITH $x AS x RETURN x, x.hour, x.minute, x.second, x.millisecond, x.microsecond, x.nanosecond, x.offset",
                data,
                new object[]
                {
                    data,
                    12L,
                    34L,
                    56L,
                    789L,
                    789012L,
                    789012587L,
                    "+01:30"
                });
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveDateTime()
        {
            var data = new LocalDateTime(1976, 6, 13, 12, 34, 56, 789012587);

            TestSendAndReceiveData(
                "CYPHER runtime=interpreted WITH $x AS x RETURN x, x.year, x.month, x.day, x.hour, x.minute, x.second, x.millisecond, x.microsecond, x.nanosecond",
                data,
                new object[]
                {
                    data,
                    1976L,
                    6L,
                    13L,
                    12L,
                    34L,
                    56L,
                    789L,
                    789012L,
                    789012587L
                });
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveDateTimeWithOffset()
        {
            var data = new ZonedDateTime(1976, 6, 13, 12, 34, 56, 789012587, Zone.Of((int)TimeSpan.FromMinutes(-90).TotalSeconds));

            TestSendAndReceiveData(
                "CYPHER runtime=interpreted WITH $x AS x RETURN x, x.year, x.month, x.day, x.hour, x.minute, x.second, x.millisecond, x.microsecond, x.nanosecond, x.offset",
                data,
                new object[]
                {
                    data,
                    1976L,
                    6L,
                    13L,
                    12L,
                    34L,
                    56L,
                    789L,
                    789012L,
                    789012587L,
                    "-01:30"
                });
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveDateTimeWithZoneId()
        {
            var data = new ZonedDateTime(1959, 5, 31, 23, 49, 59, 999999999, Zone.Of("US/Pacific"));

            TestSendAndReceiveData(
                "CYPHER runtime=interpreted WITH $x AS x RETURN x, x.year, x.month, x.day, x.hour, x.minute, x.second, x.millisecond, x.microsecond, x.nanosecond, x.timezone",
                data,
                new object[]
                {
                    data,
                    1959L,
                    5L,
                    31L,
                    23L,
                    49L,
                    59L,
                    999L,
                    999999L,
                    999999999L,
                    "US/Pacific"
                });
        }

        #endregion

        #region Randomized Send and Receive Tests

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveRandomDuration()
        {
            Enumerable.Range(0, NumberOfRandomSequences).Select(i => RandomDuration()).AsParallel()
                .ForAll(TestSendAndReceive);
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveRandomLocalDate()
        {
            Enumerable.Range(0, NumberOfRandomSequences).Select(i => RandomLocalDate()).AsParallel()
                .ForAll(TestSendAndReceive);
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveRandomLocalDateTime()
        {
            Enumerable.Range(0, NumberOfRandomSequences).Select(i => RandomLocalDateTime()).AsParallel()
                .ForAll(TestSendAndReceive);
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveRandomLocalTime()
        {
            Enumerable.Range(0, NumberOfRandomSequences).Select(i => RandomLocalTime()).AsParallel()
                .ForAll(TestSendAndReceive);
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveRandomOffsetTime()
        {
            Enumerable.Range(0, NumberOfRandomSequences).Select(i => RandomOffsetTime()).AsParallel()
                .ForAll(TestSendAndReceive);
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveRandomOffsetDateTime()
        {
            Enumerable.Range(0, NumberOfRandomSequences).Select(i => RandomOffsetDateTime()).AsParallel()
                .ForAll(TestSendAndReceive);
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveRandomZonedDateTime()
        {
            Enumerable.Range(0, NumberOfRandomSequences).Select(i => RandomZonedDateTime()).AsParallel()
                .ForAll(TestSendAndReceive);
        }

        #endregion

        #region Randomized Send And Receive Array Tests

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveArrayOfDuration()
        {
            TestSendAndReceiveArray(Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomDuration()));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveArrayOfLocalDate()
        {
            TestSendAndReceiveArray(Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomLocalDate()));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveArrayOfLocalDateTime()
        {
            TestSendAndReceiveArray(Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomLocalDateTime()));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveArrayOfLocalTime()
        {
            TestSendAndReceiveArray(Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomLocalTime()));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveArrayOfOffsetTime()
        {
            TestSendAndReceiveArray(Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomOffsetTime()));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveArrayOfOffsetDateTime()
        {
            TestSendAndReceiveArray(Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomOffsetDateTime()));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveArrayOfZonedDateTime()
        {
            TestSendAndReceiveArray(Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomZonedDateTime()));
        }

        #endregion

        #region Receive System Types through As Methods

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveDateAsDateTime()
        {
            TestReceiveDataWithType("RETURN date({ year: 1994, month: 11, day: 15 })", new DateTime(1994, 11, 15));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveLocalTimeAsDateTimeMilliseconds()
        {
            TestReceiveDataWithType("RETURN localtime({ hour: 23, minute: 49, second: 59, nanosecond: 999000000 })",
                DateTime.Today.Add(new TimeSpan(0, 23, 49, 59, 999)));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveLocalTimeAsDateTimeTicks()
        {
            TestReceiveDataWithType("RETURN localtime({ hour: 23, minute: 49, second: 59, nanosecond: 999999900 })",
                DateTime.Today.Add(new TimeSpan(0, 23, 49, 59, 0).Add(TimeSpan.FromTicks(9999999))));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveLocalDateTimeAsDateTimeMilliseconds()
        {
            TestReceiveDataWithType(
                "RETURN localdatetime({ year: 1859, month: 5, day: 31, hour: 23, minute: 49, second: 59, nanosecond: 999000000 })",
                new DateTime(1859, 5, 31, 23, 49, 59, 999));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveLocalDateTimeAsDateTimeTicks()
        {
            TestReceiveDataWithType(
                "RETURN localdatetime({ year: 1859, month: 5, day: 31, hour: 23, minute: 49, second: 59, nanosecond: 999999900 })",
                new DateTime(1859, 5, 31, 23, 49, 59, 0).AddTicks(9999999));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveLocalTimeAsTimeSpanMilliseconds()
        {
            TestReceiveDataWithType("RETURN localtime({ hour: 23, minute: 49, second: 59, nanosecond: 999000000 })",
                new TimeSpan(0, 23, 49, 59, 999));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveLocalTimeAsTimeSpanTicks()
        {
            TestReceiveDataWithType("RETURN localtime({ hour: 23, minute: 49, second: 59, nanosecond: 999999900 })",
                new TimeSpan(0, 23, 49, 59, 0).Add(TimeSpan.FromTicks(9999999)));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveDateTimeAsDateTimeOffsetMilliseconds()
        {
            TestReceiveDataWithType(
                "RETURN datetime({ year: 1859, month: 5, day: 31, hour: 23, minute: 49, second: 59, nanosecond: 999000000, timezone:'+01:30' })",
                new DateTimeOffset(new DateTime(1859, 5, 31, 23, 49, 59, 999), TimeSpan.FromMinutes(90)));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceiveDateTimeAsDateTimeOffsetTicks()
        {
            TestReceiveDataWithType(
                "RETURN datetime({ year: 1859, month: 5, day: 31, hour: 23, minute: 49, second: 59, nanosecond: 999999900,  timezone:'-02:30' })",
                new DateTimeOffset(new DateTime(1859, 5, 31, 23, 49, 59, 0).AddTicks(9999999),
                    TimeSpan.FromMinutes(-150)));
        }

        #endregion

        #region Send and Receive System Types

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveSystemDateTimeMilliseconds()
        {
            var data = new DateTime(1979, 2, 15, 7, 5, 25, 748);

            TestSendAndReceiveWithType(data, new LocalDateTime(data));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveSystemDateTimeNanoseconds()
        {
            var data = new DateTime(1979, 2, 15, 7, 5, 25).AddTicks(748999900);

            TestSendAndReceiveWithType(data, new LocalDateTime(data));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveSystemDateTimeOffsetMilliseconds()
        {
            var data = new DateTimeOffset(new DateTime(1979, 2, 15, 7, 5, 25, 748), TimeSpan.FromMinutes(90));

            TestSendAndReceiveWithType(data, new ZonedDateTime(data));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveSystemDateTimeOffsetNanoseconds()
        {
            var data = new DateTimeOffset(new DateTime(1979, 2, 15, 7, 5, 25).AddTicks(748999900),
                TimeSpan.FromMinutes(-150));

            TestSendAndReceiveWithType(data, new ZonedDateTime(data));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveSystemTimeSpanMilliseconds()
        {
            var data = new TimeSpan(0, 7, 5, 25, 748);

            TestSendAndReceiveWithType(data, new LocalTime(data));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveSystemTimeSpanNanoseconds()
        {
            var data = new TimeSpan(0, 7, 5, 25).Add(TimeSpan.FromTicks(748999900));

            TestSendAndReceiveWithType(data, new LocalTime(data));
        }

        #endregion

        #region Send and Receive Arrays Of System Types

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveArrayOfSystemDateTime()
        {
            var array = Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomSystemDateTime()).ToList();
            var actual = array.Select(v => new LocalDateTime(v)).ToList();

            TestSendAndReceiveArrayWithType(array, actual);
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveArrayOfSystemDateTimeOffset()
        {
            var array = Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomSystemDateTimeOffset()).ToList();
            var actual = array.Select(v => new ZonedDateTime(v)).ToList();

            TestSendAndReceiveArrayWithType(array, actual);
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveArrayOfSystemTimeSpan()
        {
            var array = Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomSystemTime()).ToList();
            var actual = array.Select(v => new LocalTime(v)).ToList();

            TestSendAndReceiveArrayWithType(array, actual);
        }

        #endregion

        #region Helper Methods

        private void TestReceiveData(string query, object expected)
        {
            using (var session = Server.Driver.Session(AccessMode.Read))
            {
                var record = session.Run(query).Single();
                var received = record[0];

                received.Should().Be(expected);
            }
        }

        private void TestReceiveDataWithType<T>(string query, T expected)
        {
            using (var session = Server.Driver.Session(AccessMode.Read))
            {
                var record = session.Run(query).Single();
                var received = record[0].ValueAs<T>();

                received.Should().Be(expected);
            }
        }

        private void TestSendAndReceiveData(string query, object toBeSent, object[] expectedValues)
        {
            using (var session = Server.Driver.Session(AccessMode.Read))
            {
                var record = session.Run(query, new { x = toBeSent }).Single();

                record.Keys.Count.Should().Be(expectedValues.Length);
                for (var i = 0; i < expectedValues.Length; i++)
                {
                    record[i].Should().Be(expectedValues[i]);
                }
            }
        }

        private void TestSendAndReceive(object value)
        {
            using (var session = Server.Driver.Session(AccessMode.Read))
            {
                var record = session.Run("CREATE (n:Node {value: $value}) RETURN n.value", new { value }).Single();

                record.Keys.Should().HaveCount(1);
                record[0].Should().Be(value);
            }
        }

        private void TestSendAndReceiveWithType<TSent, TRecv>(TSent value, TRecv actual)
        {
            using (var session = Server.Driver.Session(AccessMode.Read))
            {
                var record = session.Run("CREATE (n:Node {value: $value}) RETURN n.value", new { value }).Single();

                record.Keys.Should().HaveCount(1);
                record[0].Should().Be(actual);
                record[0].ValueAs<TSent>().Should().Be(value);
            }
        }

        private void TestSendAndReceiveArray(IEnumerable<object> array)
        {
            var list = array.ToList();

            using (var session = Server.Driver.Session(AccessMode.Read))
            {
                var record = session.Run("CREATE (n:Node {value: $value}) RETURN n.value", new { value = list }).Single();

                record.Keys.Should().HaveCount(1);
                record[0].Should().BeAssignableTo<IEnumerable<object>>().Which.Should().BeEquivalentTo(list);
            }
        }

        private void TestSendAndReceiveArrayWithType<TSent, TRecv>(IEnumerable<TSent> value, IEnumerable<TRecv> actual)
        {
            var valueAsList = value.ToList();
            var actualAsList = actual.ToList();

            using (var session = Server.Driver.Session(AccessMode.Read))
            {
                var record = session.Run("CREATE (n:Node {value: $value}) RETURN n.value", new { value = valueAsList }).Single();

                record.Keys.Should().HaveCount(1);
                record[0].ShouldBeEquivalentTo(actualAsList);
                record[0].ValueAs<IList<TSent>>().Should().BeEquivalentTo(valueAsList);
            }
        }

        #endregion

        #region Random Temporal Value Generation

        private Duration RandomDuration()
        {
            var sign = _random.Next(0, 1) > 0 ? 1 : -1;

            return new Duration(
                sign * _random.Next(0, int.MaxValue),
                sign * _random.Next(0, int.MaxValue),
                sign * _random.Next(0, int.MaxValue),
                _random.Next(TemporalHelpers.MinNanosecond, TemporalHelpers.MaxNanosecond)
            );
        }

        private LocalDate RandomLocalDate()
        {
            return new LocalDate(
                _random.Next(TemporalHelpers.MinYear, TemporalHelpers.MaxYear),
                _random.Next(TemporalHelpers.MinMonth, TemporalHelpers.MaxMonth),
                _random.Next(TemporalHelpers.MinDay, 28)
            );
        }

        private DateTime RandomSystemDate()
        {
            return new DateTime(
                _random.Next(DateTime.MinValue.Year, DateTime.MaxValue.Year),
                _random.Next(TemporalHelpers.MinMonth, TemporalHelpers.MaxMonth),
                _random.Next(TemporalHelpers.MinDay, 28)
            );
        }

        private LocalDateTime RandomLocalDateTime(bool tzSafe = false)
        {
            return new LocalDateTime(
                tzSafe
                    ? _random.Next(1950, TemporalHelpers.MaxYear)
                    : _random.Next(TemporalHelpers.MinYear, TemporalHelpers.MaxYear),
                _random.Next(TemporalHelpers.MinMonth, TemporalHelpers.MaxMonth),
                _random.Next(TemporalHelpers.MinDay, 28),
                tzSafe
                    ? _random.Next(6, TemporalHelpers.MaxHour)
                    : _random.Next(TemporalHelpers.MinHour, TemporalHelpers.MaxHour),
                _random.Next(TemporalHelpers.MinMinute, TemporalHelpers.MaxMinute),
                _random.Next(TemporalHelpers.MinSecond, TemporalHelpers.MaxSecond),
                _random.Next(TemporalHelpers.MinNanosecond, TemporalHelpers.MaxNanosecond)
            );
        }

        private DateTime RandomSystemDateTime()
        {
            return RandomSystemDate().Add(RandomSystemTime());
        }

        private LocalTime RandomLocalTime()
        {
            return new LocalTime(
                _random.Next(TemporalHelpers.MinHour, TemporalHelpers.MaxHour),
                _random.Next(TemporalHelpers.MinMinute, TemporalHelpers.MaxMinute),
                _random.Next(TemporalHelpers.MinSecond, TemporalHelpers.MaxSecond),
                _random.Next(TemporalHelpers.MinNanosecond, TemporalHelpers.MaxNanosecond)
            );
        }

        private TimeSpan RandomSystemTime()
        {
            return new LocalTime(
                _random.Next(TemporalHelpers.MinHour, TemporalHelpers.MaxHour),
                _random.Next(TemporalHelpers.MinMinute, TemporalHelpers.MaxMinute),
                _random.Next(TemporalHelpers.MinSecond, TemporalHelpers.MaxSecond),
                (int)(_random.Next(TemporalHelpers.MinNanosecond, TemporalHelpers.MaxNanosecond) / 100) * 100
            ).ToTimeSpan();
        }

        private OffsetTime RandomOffsetTime()
        {
            return new OffsetTime(
                RandomLocalTime(),
                _random.Next(TemporalHelpers.MinOffset, TemporalHelpers.MaxOffset)
            );
        }

        private ZonedDateTime RandomOffsetDateTime()
        {
            return new ZonedDateTime(
                RandomLocalDateTime(true),
                Zone.Of(_random.Next(TemporalHelpers.MinOffset, TemporalHelpers.MaxOffset))
            );
        }

        private DateTimeOffset RandomSystemDateTimeOffset()
        {
            return new DateTimeOffset(
                RandomSystemDateTime(),
                TimeSpan.FromMinutes(_random.Next(-14, 14) * 60)
            );
        }

        private ZonedDateTime RandomZonedDateTime()
        {
            return new ZonedDateTime(
                RandomLocalDateTime(true),
                Zone.Of(RandomTZName())
            );
        }

        private string RandomTZName()
        {
            return _tzNames.ElementAt(_random.Next(0, _tzNames.Count()));
        }

        #endregion

    }
}
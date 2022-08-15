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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver;
using Neo4j.Driver.IntegrationTests.Direct;
using Xunit;
using Xunit.Abstractions;
using static Neo4j.Driver.IntegrationTests.VersionComparison;

namespace Neo4j.Driver.IntegrationTests.Types
{
    public class TemporalTypesIT : DirectDriverTestBase
    {
        private const int NumberOfRandomSequences = 100;
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

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldReceiveDuration()
        {
            await TestReceiveData("RETURN duration({ months: 16, days: 45, seconds: 120, nanoseconds: 187309812 })",
                new Duration(16, 45, 120, 187309812));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldReceiveDate()
        {
            await TestReceiveData("RETURN date({ year: 1994, month: 11, day: 15 })",
                new LocalDate(1994, 11, 15));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldReceiveTime()
        {
            await TestReceiveData("RETURN localtime({ hour: 23, minute: 49, second: 59, nanosecond: 999999999 })",
                new LocalTime(23, 49, 59, 999999999));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldReceiveTimeWithOffset()
        {
            await TestReceiveData(
                "RETURN time({ hour: 23, minute: 49, second: 59, nanosecond: 999999999, timezone:'+03:00' })",
                new OffsetTime(23, 49, 59, 999999999, (int) TimeSpan.FromHours(3).TotalSeconds));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldReceiveDateTime()
        {
            await TestReceiveData(
                "RETURN localdatetime({ year: 1859, month: 5, day: 31, hour: 23, minute: 49, second: 59, nanosecond: 999999999 })",
                new LocalDateTime(1859, 5, 31, 23, 49, 59, 999999999));
        }

        [RequireServerFact("3.4.0", "4.3.0", Between)]
        public async Task ShouldReceiveDateTimeWithOffset()
        {
            await TestReceiveData(
                "RETURN datetime({ year: 1859, month: 5, day: 31, hour: 23, minute: 49, second: 59, nanosecond: 999999999, timezone:'+02:30' })",
                new ZonedDateTime(1859, 5, 31, 23, 49, 59, 999999999,
                    Zone.Of((int) TimeSpan.FromMinutes(150).TotalSeconds)));
        }

        [RequireServerFact("3.4.0", "4.3.0", Between)]
        public async Task ShouldReceiveDateTimeWithZoneId()
        {
            await TestReceiveData(
                "RETURN datetime({ year: 1959, month: 5, day: 31, hour: 23, minute: 49, second: 59, nanosecond: 999999999, timezone:'Europe/London' })",
                new ZonedDateTime(1959, 5, 31, 23, 49, 59, 999999999, Zone.Of("Europe/London")));
        }

        #endregion

        #region Send and Receive Tests

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveDuration()
        {
            var data = new Duration(14, 35, 75, 789012587);

            await TestSendAndReceiveData(
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

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveDate()
        {
            var data = new LocalDate(1976, 6, 13);

            await TestSendAndReceiveData(
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

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveTime()
        {
            var data = new LocalTime(12, 34, 56, 789012587);

            await TestSendAndReceiveData(
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

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveTimeWithOffset()
        {
            var data = new OffsetTime(12, 34, 56, 789012587, (int) TimeSpan.FromMinutes(90).TotalSeconds);

            await TestSendAndReceiveData(
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

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveDateTime()
        {
            var data = new LocalDateTime(1976, 6, 13, 12, 34, 56, 789012587);

            await TestSendAndReceiveData(
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

        [RequireServerFact("3.4.0", "4.3.0", Between)]
        public async Task ShouldSendAndReceiveDateTimeWithOffset()
        {
            var data = new ZonedDateTime(1976, 6, 13, 12, 34, 56, 789012587,
                Zone.Of((int) TimeSpan.FromMinutes(-90).TotalSeconds));

            await TestSendAndReceiveData(
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

        [RequireServerFact("3.4.0", "4.3.0", Between)]
        public async Task ShouldSendAndReceiveDateTimeWithZoneId()
        {
            var data = new ZonedDateTime(1959, 5, 31, 23, 49, 59, 999999999, Zone.Of("US/Pacific"));

            await TestSendAndReceiveData(
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

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveRandomDuration()
        {
            await Task.WhenAll(
                Enumerable.Range(0, NumberOfRandomSequences).Select(i => RandomDuration())
                    .Select(TestSendAndReceive));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveRandomLocalDate()
        {
            await Task.WhenAll(
                Enumerable.Range(0, NumberOfRandomSequences).Select(i => RandomLocalDate())
                    .Select(TestSendAndReceive));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveRandomLocalDateTime()
        {
            await Task.WhenAll(
                Enumerable.Range(0, NumberOfRandomSequences).Select(i => RandomLocalDateTime())
                    .Select(TestSendAndReceive));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveRandomLocalTime()
        {
            await Task.WhenAll(
                Enumerable.Range(0, NumberOfRandomSequences).Select(i => RandomLocalTime())
                    .Select(TestSendAndReceive));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveRandomOffsetTime()
        {
            await Task.WhenAll(
                Enumerable.Range(0, NumberOfRandomSequences).Select(i => RandomOffsetTime())
                    .Select(TestSendAndReceive));
        }

        [RequireServerFact("3.4.0", "4.3.0", Between)]
        public async Task ShouldSendAndReceiveRandomOffsetDateTime()
        {
            await Task.WhenAll(
                Enumerable.Range(0, NumberOfRandomSequences).Select(i => RandomOffsetDateTime())
                    .Select(TestSendAndReceive));
        }

        [RequireServerFact("3.4.0", "4.3.0", Between)]
        public async Task ShouldSendAndReceiveRandomZonedDateTime()
        {
            await Task.WhenAll(
                Enumerable.Range(0, NumberOfRandomSequences).Select(i => RandomZonedDateTime())
                    .Select(TestSendAndReceive));
        }

        #endregion

        #region Randomized Send And Receive Array Tests

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveArrayOfDuration()
        {
            await TestSendAndReceiveArray(Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomDuration()));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveArrayOfLocalDate()
        {
            await TestSendAndReceiveArray(Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomLocalDate()));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveArrayOfLocalDateTime()
        {
            await TestSendAndReceiveArray(Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomLocalDateTime()));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveArrayOfLocalTime()
        {
            await TestSendAndReceiveArray(Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomLocalTime()));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveArrayOfOffsetTime()
        {
            await TestSendAndReceiveArray(Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomOffsetTime()));
        }

        [RequireServerFact("3.4.0", "4.3.0", Between)]
        public async Task ShouldSendAndReceiveArrayOfOffsetDateTime()
        {
            await TestSendAndReceiveArray(Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomOffsetDateTime()));
        }

        [RequireServerFact("3.4.0", "4.3.0", Between)]
        public async Task ShouldSendAndReceiveArrayOfZonedDateTime()
        {
            await TestSendAndReceiveArray(Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomZonedDateTime()));
        }

        #endregion

        #region Receive System Types through As Methods

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldReceiveDateAsDateTime()
        {
            await TestReceiveDataWithType("RETURN date({ year: 1994, month: 11, day: 15 })",
                new DateTime(1994, 11, 15));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldReceiveLocalTimeAsDateTimeMilliseconds()
        {
            await TestReceiveDataWithType(
                "RETURN localtime({ hour: 23, minute: 49, second: 59, nanosecond: 999000000 })",
                DateTime.Today.Add(new TimeSpan(0, 23, 49, 59, 999)));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldReceiveLocalTimeAsDateTimeTicks()
        {
            await TestReceiveDataWithType(
                "RETURN localtime({ hour: 23, minute: 49, second: 59, nanosecond: 999999900 })",
                DateTime.Today.Add(new TimeSpan(0, 23, 49, 59, 0).Add(TimeSpan.FromTicks(9999999))));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldReceiveLocalDateTimeAsDateTimeMilliseconds()
        {
            await TestReceiveDataWithType(
                "RETURN localdatetime({ year: 1859, month: 5, day: 31, hour: 23, minute: 49, second: 59, nanosecond: 999000000 })",
                new DateTime(1859, 5, 31, 23, 49, 59, 999));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldReceiveLocalDateTimeAsDateTimeTicks()
        {
            await TestReceiveDataWithType(
                "RETURN localdatetime({ year: 1859, month: 5, day: 31, hour: 23, minute: 49, second: 59, nanosecond: 999999900 })",
                new DateTime(1859, 5, 31, 23, 49, 59, 0).AddTicks(9999999));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldReceiveLocalTimeAsTimeSpanMilliseconds()
        {
            await TestReceiveDataWithType(
                "RETURN localtime({ hour: 23, minute: 49, second: 59, nanosecond: 999000000 })",
                new TimeSpan(0, 23, 49, 59, 999));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldReceiveLocalTimeAsTimeSpanTicks()
        {
            await TestReceiveDataWithType(
                "RETURN localtime({ hour: 23, minute: 49, second: 59, nanosecond: 999999900 })",
                new TimeSpan(0, 23, 49, 59, 0).Add(TimeSpan.FromTicks(9999999)));
        }

        [RequireServerFact("3.4.0", "4.3.0", Between)]
        public async Task ShouldReceiveDateTimeAsDateTimeOffsetMilliseconds()
        {
            await TestReceiveDataWithType(
                "RETURN datetime({ year: 1859, month: 5, day: 31, hour: 23, minute: 49, second: 59, nanosecond: 999000000, timezone:'+01:30' })",
                new DateTimeOffset(new DateTime(1859, 5, 31, 23, 49, 59, 999), TimeSpan.FromMinutes(90)));
        }

        [RequireServerFact("3.4.0", "4.3.0", Between)]
        public async Task ShouldReceiveDateTimeAsDateTimeOffsetTicks()
        {
            await TestReceiveDataWithType(
                "RETURN datetime({ year: 1859, month: 5, day: 31, hour: 23, minute: 49, second: 59, nanosecond: 999999900,  timezone:'-02:30' })",
                new DateTimeOffset(new DateTime(1859, 5, 31, 23, 49, 59, 0).AddTicks(9999999),
                    TimeSpan.FromMinutes(-150)));
        }

        #endregion

        #region Send and Receive System Types

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveSystemDateTimeMilliseconds()
        {
            var data = new DateTime(1979, 2, 15, 7, 5, 25, 748);

            await TestSendAndReceiveWithType(data, new LocalDateTime(data));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveSystemDateTimeNanoseconds()
        {
            var data = new DateTime(1979, 2, 15, 7, 5, 25).AddTicks(748999900);

            await TestSendAndReceiveWithType(data, new LocalDateTime(data));
        }

        [RequireServerFact("3.4.0", "4.3.0", Between)]
        public async Task ShouldSendAndReceiveSystemDateTimeOffsetMilliseconds()
        {
            var data = new DateTimeOffset(new DateTime(1979, 2, 15, 7, 5, 25, 748), TimeSpan.FromMinutes(90));

            await TestSendAndReceiveWithType(data, new ZonedDateTime(data));
        }

        [RequireServerFact("3.4.0", "4.3.0", Between)]
        public async Task ShouldSendAndReceiveSystemDateTimeOffsetNanoseconds()
        {
            var data = new DateTimeOffset(new DateTime(1979, 2, 15, 7, 5, 25).AddTicks(748999900),
                TimeSpan.FromMinutes(-150));

            await TestSendAndReceiveWithType(data, new ZonedDateTime(data));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveSystemTimeSpanMilliseconds()
        {
            var data = new TimeSpan(0, 7, 5, 25, 748);

            await TestSendAndReceiveWithType(data, new LocalTime(data));
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveSystemTimeSpanNanoseconds()
        {
            var data = new TimeSpan(0, 7, 5, 25).Add(TimeSpan.FromTicks(748999900));

            await TestSendAndReceiveWithType(data, new LocalTime(data));
        }

        #endregion

        #region Send and Receive Arrays Of System Types

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveArrayOfSystemDateTime()
        {
            var array = Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomSystemDateTime()).ToList();
            var actual = array.Select(v => new LocalDateTime(v)).ToList();

            await TestSendAndReceiveArrayWithType(array, actual);
        }

        [RequireServerFact("3.4.0", "4.3.0", Between)]
        public async Task ShouldSendAndReceiveArrayOfSystemDateTimeOffset()
        {
            var array = Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomSystemDateTimeOffset()).ToList();
            var actual = array.Select(v => new ZonedDateTime(v)).ToList();

            await TestSendAndReceiveArrayWithType(array, actual);
        }

        [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
        public async Task ShouldSendAndReceiveArrayOfSystemTimeSpan()
        {
            var array = Enumerable.Range(0, _random.Next(MinArrayLength, MaxArrayLength))
                .Select(i => RandomSystemTime()).ToList();
            var actual = array.Select(v => new LocalTime(v)).ToList();

            await TestSendAndReceiveArrayWithType(array, actual);
        }

        #endregion

        #region Helper Methods

        private async Task TestReceiveData(string query, object expected)
        {
            var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
            try
            {
                var cursor = await session.RunAsync(query);
                var record = await cursor.SingleAsync();
                var received = record[0];

                received.Should().Be(expected);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        private async Task TestReceiveDataWithType<T>(string query, T expected)
        {
            var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
            try
            {
                var cursor = await session.RunAsync(query);
                var record = await cursor.SingleAsync();
                var received = record[0].As<T>();

                received.Should().Be(expected);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        private async Task TestSendAndReceiveData(string query, object toBeSent, object[] expectedValues)
        {
            var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));
            try
            {
                var cursor = await session.RunAsync(query, new {x = toBeSent});
                var record = await cursor.SingleAsync();

                record.Keys.Count.Should().Be(expectedValues.Length);
                for (var i = 0; i < expectedValues.Length; i++)
                {
                    record[i].Should().Be(expectedValues[i]);
                }
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        private async Task TestSendAndReceive(object value)
        {
            var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));
            try
            {
                var cursor = await session.RunAsync("CREATE (n:Node {value: $value}) RETURN n.value", new {value});
                var record = await cursor.SingleAsync();

                record.Keys.Should().HaveCount(1);
                record[0].Should().Be(value);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        private async Task TestSendAndReceiveWithType<TSent, TRecv>(TSent value, TRecv actual)
        {
            var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));

            try
            {
                var cursor = await session.RunAsync("CREATE (n:Node {value: $value}) RETURN n.value", new {value});
                var record = await cursor.SingleAsync();

                record.Keys.Should().HaveCount(1);
                record[0].Should().Be(actual);
                record[0].As<TSent>().Should().Be(value);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        private async Task TestSendAndReceiveArray(IEnumerable<object> array)
        {
            var list = array.ToList();

            var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));
            try
            {
                var cursor =
                    await session.RunAsync("CREATE (n:Node {value: $value}) RETURN n.value", new {value = list});
                var record = await cursor.SingleAsync();

                record.Keys.Should().HaveCount(1);
                record[0].Should().BeAssignableTo<IEnumerable<object>>().Which.Should().BeEquivalentTo(list);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        private async Task TestSendAndReceiveArrayWithType<TSent, TRecv>(IEnumerable<TSent> value,
            IEnumerable<TRecv> actual)
        {
            var valueAsList = value.ToList();
            var actualAsList = actual.ToList();

            var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));
            try
            {
                var cursor = await session.RunAsync("CREATE (n:Node {value: $value}) RETURN n.value",
                    new {value = valueAsList});
                var record = await cursor.SingleAsync();

                record.Keys.Should().HaveCount(1);
                record[0].Should().BeEquivalentTo(actualAsList);
                record[0].As<IList<TSent>>().Should().BeEquivalentTo(valueAsList);
            }
            finally
            {
                await session.CloseAsync();
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
                _random.Next(TemporalHelpers.MinNanosecond, TemporalHelpers.MaxNanosecond) / 100 * 100
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
                Zone.Of(RandomTzName())
            );
        }

        private string RandomTzName()
        {
            return _tzNames.ElementAt(_random.Next(0, _tzNames.Count()));
        }

        #endregion
    }
}
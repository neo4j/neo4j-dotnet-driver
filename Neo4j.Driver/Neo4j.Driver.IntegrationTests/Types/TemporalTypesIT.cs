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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.V1;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Types
{
    public class TemporalTypesIT: DirectDriverTestBase
    {
        public TemporalTypesIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {

        }

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

        public void TestReceiveData(string query, object expected)
        {
            using (var session = Server.Driver.Session(AccessMode.Read))
            {
                var record = session.Run(query).Single();
                var received = record[0];

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


    }
}
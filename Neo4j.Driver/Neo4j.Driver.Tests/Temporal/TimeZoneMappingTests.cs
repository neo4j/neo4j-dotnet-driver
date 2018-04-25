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

using FluentAssertions;
using Neo4j.Driver.Internal.Temporal;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Temporal
{
    public class TimeZoneMappingTests
    {

        [WindowsTheory]
        [InlineData("America/Vancouver", "Pacific Standard Time")]
        [InlineData("America/Phoenix", "US Mountain Standard Time")]
        [InlineData("Etc/GMT+8", "UTC-08")]
        [InlineData("Pacific/Pitcairn", "UTC-08")]
        [InlineData("America/North_Dakota/New_Salem", "Central Standard Time")]
        [InlineData("America/Port-au-Prince", "Haiti Standard Time")]
        [InlineData("Etc/UTC", "UTC")]
        [InlineData("Etc/GMT", "UTC")]
        [InlineData("Etc/GMT-2", "South Africa Standard Time")]
        [InlineData("Europe/Istanbul", "Turkey Standard Time")]
        public void ShouldFindWindowsFromIana(string ianaId, string expectedWindowsId)
        {
            var tzInfo = TimeZoneMapping.Get(ianaId);

            tzInfo.Should().NotBeNull();
            tzInfo.Id.Should().Be(expectedWindowsId);
        }

    }
}
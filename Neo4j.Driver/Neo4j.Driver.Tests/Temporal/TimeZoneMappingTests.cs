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
using System.Globalization;
using FluentAssertions;
using Neo4j.Driver.Internal.Temporal;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Temporal;

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

    [UnixTheory]
    [InlineData("America/Vancouver")]
    [InlineData("America/Phoenix")]
    [InlineData("Etc/GMT+8")]
    [InlineData("Pacific/Pitcairn")]
    [InlineData("America/North_Dakota/New_Salem")]
    [InlineData("America/Port-au-Prince")]
    [InlineData("Etc/UTC", "UTC")]
    [InlineData("Etc/GMT", "UTC")]
    [InlineData("Etc/GMT-2")]
    [InlineData("Europe/Istanbul")]
    public void ShouldFindIana(string ianaId, string expectedValue = null)
    {
        var tzInfo = TimeZoneMapping.Get(ianaId);
        var testValue = expectedValue ?? ianaId;

        tzInfo.Should().NotBeNull();
        tzInfo.Id.Should().Be(testValue);
    }

    [UnixTheory]
    [InlineData("Pacific Standard Time", "en-CA", "America/Vancouver")]
    [InlineData("US Mountain Standard Time", null, "America/Phoenix")]
    [InlineData("US Mountain Standard Time", "", "America/Phoenix")]
    [InlineData("Central Standard Time", "en-US", "America/Chicago")]
    [InlineData("UTC", null, "UTC")]
    [InlineData("South Africa Standard Time", "en-ZA", "Africa/Johannesburg")]
    [InlineData("Turkey Standard Time", "tr-TR", "Europe/Istanbul")]
    [InlineData("Turkey Standard Time", "", "Europe/Istanbul")]
    [InlineData("Turkey Standard Time", null, "Europe/Istanbul")]
    public void ShouldFindIanaFromWindows(string windowsId, string cultureName, string ianaId)
    {
        ExecuteWithCulture(
            cultureName,
            () =>
            {
                var tzInfo = TimeZoneMapping.Get(windowsId);

                tzInfo.Should().NotBeNull();
                tzInfo.Id.Should().Be(ianaId);
            });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tr-TR")]
    public void ShouldThrowExceptionWhenNonExistent(string culture)
    {
        var exc = Record.Exception(
            () =>
                ExecuteWithCulture(culture, () => TimeZoneMapping.Get("Some non-existent time zone id")));

        // System.TimeZoneNotFoundException is not public in .net standard 1.3
        exc.Should().NotBeNull();
        exc.GetType().Name.Should().Be("TimeZoneNotFoundException");
    }

    private static void ExecuteWithCulture(string cultureName, Action action)
    {
        var cInfo = cultureName == null
            ? CultureInfo.CurrentCulture
            : new CultureInfo(cultureName);

        var original = CultureInfo.CurrentCulture;

        try
        {
            SetCulture(cInfo);

            action();
        }
        finally
        {
            SetCulture(original);
        }
    }

    private static void SetCulture(CultureInfo culture)
    {
        #if NET452
            Thread.CurrentThread.CurrentCulture = culture;
        #else
        CultureInfo.CurrentCulture = culture;
        #endif
    }
}

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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Neo4j.Driver.Internal.Temporal
{
    internal static class TimeZoneMapping
    {
        private static readonly IDictionary<string, string> IANAToWindows;
        private static readonly IDictionary<string, string> WindowsToIANA;
        private static readonly IDictionary<string, TimeZoneInfo> SystemToTZInfo;

        static TimeZoneMapping()
        {
            try
            {
                var typeInfo = typeof(TimeZoneMapping).GetTypeInfo();
                var resource =
                    typeInfo.Assembly.GetManifestResourceStream("Neo4j.Driver.Internal.Temporal.windowsZones.xml");

                Load(resource, out var ianaToWindows, out var windowsToIana);

                IANAToWindows = new ReadOnlyDictionary<string, string>(ianaToWindows);
                WindowsToIANA = new ReadOnlyDictionary<string, string>(windowsToIana);
            }
            catch (Exception)
            {
                // We don't want this to break the program
                IANAToWindows = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
                WindowsToIANA = IANAToWindows;
            }
            finally
            {
                SystemToTZInfo =
                    new ReadOnlyDictionary<string, TimeZoneInfo>(TimeZoneInfo.GetSystemTimeZones()
                        .ToDictionary(tz => tz.Id, tz => tz));
            }
        }

        public static TimeZoneInfo Get(string zoneId)
        {
            Throw.ArgumentNullException.If(() => string.IsNullOrWhiteSpace(zoneId), nameof(zoneId));

            if (SystemToTZInfo.TryGetValue(zoneId, out var tzInfo))
            {
                return tzInfo;
            }

            // First guess, is it already IANA?
            if (IANAToWindows.TryGetValue(zoneId, out var windowsId))
            {
                if (SystemToTZInfo.TryGetValue(windowsId, out tzInfo))
                {
                    return tzInfo;
                }
            }

            // Try with windows to IANA with territory first
            var territory = GetCurrentTerritory() ?? "001";
            if (WindowsToIANA.TryGetValue($"{zoneId}_{territory}", out var ianaId))
            {
                if (SystemToTZInfo.TryGetValue(ianaId, out tzInfo))
                {
                    return tzInfo;
                }
            }

            // Try with windows to IANA with 001, if we did not already
            if (!territory.Equals("001") && WindowsToIANA.TryGetValue($"{zoneId}_001", out ianaId))
            {
                if (SystemToTZInfo.TryGetValue(ianaId, out tzInfo))
                {
                    return tzInfo;
                }
            }

            // This is solely to get an exception of 'TimeZoneNotFoundException'
            return TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        }

        private static string GetCurrentTerritory()
        {
            var currentCulture = CultureInfo.CurrentCulture;
            if (string.IsNullOrEmpty(currentCulture.Name) || currentCulture.IsNeutralCulture)
            {
                return null;
            }

            try
            {
                var regionInfo = new RegionInfo(currentCulture.Name);
                return regionInfo.TwoLetterISORegionName;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void Load(Stream source, out IDictionary<string, string> ianaToWindows,
            out IDictionary<string, string> windowsToIana)
        {
            var mapZoneName = XName.Get("mapZone");
            var otherName = XName.Get("other");
            var territoryName = XName.Get("territory");
            var typeName = XName.Get("type");

            using (var reader = new StreamReader(source, Encoding.UTF8))
            {
                var doc = XDocument.Load(reader);
                var allMappedZones = doc.Descendants(mapZoneName).ToArray();

                windowsToIana = allMappedZones.SelectMany(zone => GetAttribute(zone, typeName)
                        .Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries).Take(1).Select(ianaName =>
                            new KeyValuePair<string, string>(
                                $"{GetAttribute(zone, otherName)}_{GetAttribute(zone, territoryName)}", ianaName)))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                ianaToWindows = allMappedZones.SelectMany(zone => GetAttribute(zone, typeName)
                        .Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries).Select(ianaName =>
                            new KeyValuePair<string, string>($"{ianaName}", GetAttribute(zone, otherName))))
                    .Distinct(new MappingEqualityComparer())
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }

        private static string GetAttribute(XElement element, XName name, string defaultValue = "")
        {
            var attribute = element.Attribute(name);

            return attribute != null ? attribute.Value : defaultValue;
        }

        private class MappingEqualityComparer: IEqualityComparer<KeyValuePair<string, string>>
        {

            public bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
            {
                return x.Key.Equals(y.Key) && x.Value.Equals(y.Value);
            }

            public int GetHashCode(KeyValuePair<string, string> obj)
            {
                unchecked
                {
                    var hashCode = obj.Key.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.Value.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}
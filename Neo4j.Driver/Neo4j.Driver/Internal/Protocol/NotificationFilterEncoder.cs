// Copyright (c) 2002-2022 "Neo4j,"
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
using System.Linq;

namespace Neo4j.Driver.Internal.Protocol;

internal static class NotificationFilterEncoder
{
    public static string[] EncodeNotificationFilters(INotificationFilterConfig[] filters)
    {
        if (filters is null or { Length: 0 })
            return null;

        if (filters.Length == 1 && filters[0] == INotificationFilterConfig.None)
            return Array.Empty<string>();

        return filters.Select(MapFilterToString).ToArray();
    }

    public static string MapFilterToString(INotificationFilterConfig arg)
    {
        return arg switch
        {
            INotificationFilterConfig.None => 
                throw new ArgumentException(
                    $"Attempted to use {nameof(INotificationFilterConfig.None)} with another {nameof(INotificationFilterConfig)}."),
            INotificationFilterConfig.All => "*.*",
            INotificationFilterConfig.AllQuery => "*.QUERY",
            INotificationFilterConfig.WarningAll => "WARNING.*",
            INotificationFilterConfig.WarningDeprecation => "WARNING.DEPRECATION",
            INotificationFilterConfig.WarningHint => "WARNING.HINT",
            INotificationFilterConfig.WarningQuery => "WARNING.QUERY",
            INotificationFilterConfig.WarningUnsupported => "WARNING.UNSUPPORTED",
            INotificationFilterConfig.InformationAll => "INFORMATION.*",
            INotificationFilterConfig.InformationRuntime => "INFORMATION.RUNTIME",
            INotificationFilterConfig.InformationQuery => "INFORMATION.QUERY",
            INotificationFilterConfig.InformationPerformance => "INFORMATION.PERFORMANCE",
            _ => throw new ArgumentOutOfRangeException(nameof(arg), arg, null)
        };
    }
}
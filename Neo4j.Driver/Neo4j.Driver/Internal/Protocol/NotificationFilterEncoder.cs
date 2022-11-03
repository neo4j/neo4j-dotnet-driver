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
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver.Internal.Protocol;

internal static class NotificationFilterEncoder
{
    public static string[] EncodeNotificationFilters(INotificationFilterConfig filters)
    {
        return filters switch
        {
            NoNotificationFilterConfig => Array.Empty<string>(),
            ServerDefaultNotificationFilterConfig => null,
            NotificationFilterSetConfig set => set.Filters.Select(x => MapFilterToString(x).ToUpper()).ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(filters), filters, null)
        };
    }

    public static string MapFilterToString((Severity Severity, Category Category) pair)
    {
        return pair.Severity == Severity.All ? $"*.{pair.Category:G}" : $"{pair.Severity:G}.{pair.Category:G}";
    }
}
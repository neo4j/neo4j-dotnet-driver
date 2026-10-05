// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal;

internal class LoggingContext : ILoggingContext
{
    public LoggingContext(string key, object value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; }
    public object Value { get; }
}

internal static class LoggingContextExtensions
{
    public static KeyValuePair<string, object?> AsMicrosoftStateItem(this ILoggingContext loggingContext)
    {
        return new KeyValuePair<string, object?>(loggingContext.Key, loggingContext.Value);
    }

    public static IEnumerable<KeyValuePair<string, object?>> AsMicrosoftStateItems(
        this IEnumerable<ILoggingContext> loggingContexts)
    {
        return loggingContexts.Select(AsMicrosoftStateItem);
    }
}

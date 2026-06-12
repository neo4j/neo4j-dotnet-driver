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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Neo4j.Driver.Internal;

internal static class LoggingHelpers
{
    private const string OriginalFormatStringKey = "{OriginalFormat}";

    public static bool ExtractFormatAndArguments<TState>(
        TState state, 
        [NotNullWhen(true)] out string? format, 
        [NotNullWhen(true)] out object?[]? args)
    {
        if (state is not IReadOnlyList<KeyValuePair<string, object>> list)
        {
            format = null;
            args = null;
            return false;
        }

        var dict = list.ToDictionary(kv => kv.Key, object? (kv) => kv.Value);
        args = list.Where(kv => kv.Key != OriginalFormatStringKey).Select(kv => kv.Value).ToArray();
        format = dict.GetValueOrDefault(OriginalFormatStringKey)?.ToString() ?? "";
        return true;
    }
}

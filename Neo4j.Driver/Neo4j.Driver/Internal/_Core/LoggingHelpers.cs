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

    public static bool TryBuildScopePrefix<TState>(TState state, [NotNullWhen(true)] out string? prefix)
        where TState : notnull
    {
        if (state is LogParams || state is not IEnumerable<KeyValuePair<string, object?>> contexts)
        {
            prefix = null;
            return false;
        }

        prefix = string.Concat(contexts.Select(kvp => $"[{kvp.Key}:{kvp.Value}] "));
        return true;
    }

    public static bool ExtractFormatAndArguments<TState>(
        TState state,
        [NotNullWhen(true)] out string? format,
        [NotNullWhen(true)] out object?[]? args)
    {
        if (state is not LogParams logParams)
        {
            format = null;
            args = null;
            return false;
        }

        format = "";
        var extractedArgs = new List<object?>(logParams.Count);
        foreach (var kv in logParams)
        {
            if (kv.Key == OriginalFormatStringKey)
            {
                format = kv.Value?.ToString() ?? "";
            }
            else
            {
                extractedArgs.Add(kv.Value);
            }
        }

        args = extractedArgs.ToArray();
        return true;
    }
}

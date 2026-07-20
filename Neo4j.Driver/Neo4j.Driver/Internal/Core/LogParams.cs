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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Neo4j.Driver.Internal;

internal partial class LogParams : IReadOnlyList<KeyValuePair<string, object?>>
{
    // Single source of truth for message-template placeholders ({name}, {name,alignment}, {name:format}).
    // Group 1 is the parameter name; group 2 is the optional alignment/format suffix.
    [GeneratedRegex(@"\{(\w+)([,:][^}]*)?\}", RegexOptions.Compiled)]
    private static partial Regex GeneratePlaceholderRegex();
    internal static readonly Regex PlaceholderRegex = GeneratePlaceholderRegex();
    private readonly List<KeyValuePair<string, object?>> _extractedList;

    public LogParams(string messageTemplate, object?[] args)
    {
        _extractedList = CreateLogFormat(messageTemplate, args);
    }

    private static List<KeyValuePair<string, object?>> CreateLogFormat(string messageTemplate, object?[] args)
    {
        var result = new List<KeyValuePair<string, object?>>
        {
            new("{OriginalFormat}", messageTemplate)
        };

        var parameterNames = ExtractParameterNames(messageTemplate);
        for (var i = 0; i < Math.Min(parameterNames.Count, args.Length); i++)
        {
            result.Add(new(parameterNames[i], args[i]));
        }

        return result;
    }

    private static List<string> ExtractParameterNames(string messageTemplate)
    {
        var matches = PlaceholderRegex.Matches(messageTemplate);
        return [..matches.Select(m => m.Groups[1].Value)];
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        return _extractedList.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_extractedList).GetEnumerator();
    }

    public int Count => _extractedList.Count;

    public KeyValuePair<string, object?> this[int index] => _extractedList[index];
}

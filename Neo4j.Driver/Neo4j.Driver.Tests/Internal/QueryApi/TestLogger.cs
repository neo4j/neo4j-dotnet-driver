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
using System.Text.RegularExpressions;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

internal class TestLogger(ITestOutputHelper output, Type subjectType) : ILogger
{
    private readonly string _prefix = $"[{subjectType.Name}]";
    private static readonly Regex Placeholders = new(@"\{[^}]+\}");

    private void WriteFormatted(string level, string messageTemplate, object?[] args, Exception? exception = null)
    {
        var index = 0;
        var indexed = Placeholders.Replace(messageTemplate, _ => $"{{{index++}}}");
        try
        {
            var message = args.Length > 0 
                ? string.Format(indexed, args) 
                : indexed;
            
            output.WriteLine($"{level} {_prefix} {message}");
        }
        catch
        {
            // best effort
            output.WriteLine($"{level} {_prefix} {indexed} [{string.Join(", ", args)}]");
        }

        if (exception != null)
        {
            output.WriteLine($"{exception}");
        }
    }

    public void Debug(string messageTemplate, params object?[] args) =>
        WriteFormatted("DBG", messageTemplate, args);

    public void Info(string messageTemplate, params object?[] args) =>
        WriteFormatted("INF", messageTemplate, args);

    public void Warn(string messageTemplate, params object?[] args) =>
        WriteFormatted("WRN", messageTemplate, args);

    public void Error(string messageTemplate, params object?[] args) =>
        WriteFormatted("ERR", messageTemplate, args);

    public void Error(Exception exception, string messageTemplate, params object?[] args) =>
        WriteFormatted("ERR", messageTemplate, args, exception);
}

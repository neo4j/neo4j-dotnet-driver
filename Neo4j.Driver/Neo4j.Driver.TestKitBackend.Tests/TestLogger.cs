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

using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Util;
using Xunit;
using EventId = Microsoft.Extensions.Logging.EventId;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Neo4j.Driver.TestKitBackend.Tests;

file record UnitTest;

internal class TestLogger : ILogger
{
    private readonly string _suffix;
    private string _scopeSuffix = "";

    public TestLogger(Type? subjectType = null) : this((subjectType ?? typeof(UnitTest)).Name)
    {
    }

    public TestLogger(string categoryName)
    {
        _suffix = $"[{categoryName}]";
    }

    private void WriteFormatted(string level, string messageTemplate, object?[] args, Exception? exception = null)
    {
        var output = TestContext.Current.TestOutputHelper;
        if (output is null)
        {
            return;
        }

        var index = 0;
        var indexed = LogParams.PlaceholderRegex.Replace(messageTemplate, _ => $"{{{index++}}}");
        try
        {
            var message = args.Length > 0
                ? string.Format(indexed, args)
                : messageTemplate;

            output.WriteLine($"{level} {message} {_scopeSuffix}{_suffix}");
        }
        catch
        {
            output.WriteLine($"{level}{indexed} [{string.Join(", ", args)}] {_scopeSuffix}{_suffix}");
        }

        if (exception != null)
            output.WriteLine($"{exception}");
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var level = logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "???"
        };

        if (LoggingHelpers.ExtractFormatAndArguments(state, out var template, out var args))
            WriteFormatted(level, template, args, exception);
        else
            WriteFormatted(level, formatter(state, exception), [], exception);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        if (!LoggingHelpers.TryBuildScopePrefix(state, out var prefix))
        {
            return null;
        }

        var previous = _scopeSuffix;
        _scopeSuffix = prefix;
        return new ActionDisposable(() => _scopeSuffix = previous);
    }
}

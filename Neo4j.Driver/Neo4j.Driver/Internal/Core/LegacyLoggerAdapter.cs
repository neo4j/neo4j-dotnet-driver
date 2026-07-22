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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal;

internal class LegacyLoggerAdapter : ILogger
{
    private readonly INeo4jLogger _legacyLogger;
    private readonly Type _loggingType;
    private readonly AsyncLocal<string?> _scopePrefix = new();

    public LegacyLoggerAdapter(INeo4jLogger legacyLogger, Type loggingType)
    {
        _legacyLogger = legacyLogger;
        _loggingType = loggingType;
    }

    private string GetModifiedFormat(string messageTemplate, int argCount)
    {
        var format = new StringBuilder();
        format.Append('[').Append(_loggingType.Name).Append("] ");
        AppendEscaped(format, _scopePrefix.Value);

        var index = 0;
        var position = 0;
        foreach (Match match in LogParams.PlaceholderRegex.Matches(messageTemplate))
        {
            AppendEscaped(format, messageTemplate[position..match.Index]);
            if (index < argCount)
            {
                var suffix = match.Groups["suffix"].Value; // includes its leading ':' or ',' if present
                format.Append('{').Append(index++).Append(suffix).Append('}');
            }
            else
            {
                // No arg for this placeholder: render it literally.
                AppendEscaped(format, match.Value);
            }

            position = match.Index + match.Length;
        }

        AppendEscaped(format, messageTemplate[position..]);
        return format.ToString();
    }

    private static void AppendEscaped(StringBuilder builder, string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        foreach (var ch in text)
        {
            switch (ch)
            {
                case '{':
                    builder.Append("{{");
                    break;
                case '}':
                    builder.Append("}}");
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if(!IsEnabled(logLevel))
        {
            return;
        }

        if (!LoggingHelpers.ExtractFormatAndArguments(state, out var format, out var args))
        {
            return;
        }

        var template = GetModifiedFormat(format, args.Length);
        switch (logLevel)
        {
            case LogLevel.Trace:
                _legacyLogger.Trace(template, args);
                break;

            case LogLevel.Debug:
                _legacyLogger.Debug(template, args);
                break;

            case LogLevel.Information:
                _legacyLogger.Info(template, args);
                break;

            case LogLevel.Warning:
                _legacyLogger.Warn(exception, template, args);
                break;

            case LogLevel.Error:
            case LogLevel.Critical:
                _legacyLogger.Error(exception, template, args);
                break;
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => _legacyLogger.IsTraceEnabled(),
            LogLevel.Debug => _legacyLogger.IsDebugEnabled(),
            _ => true
        };
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        if (!LoggingHelpers.TryBuildScopePrefix(state, out var prefix))
        {
            return null;
        }

        var previous = _scopePrefix.Value;
        _scopePrefix.Value = prefix;
        return new ActionDisposable(() => _scopePrefix.Value = previous);
    }
}

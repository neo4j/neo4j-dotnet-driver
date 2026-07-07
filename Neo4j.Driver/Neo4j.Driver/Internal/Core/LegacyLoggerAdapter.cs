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
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal;

internal class LegacyLoggerAdapter : ILogger
{
    private readonly INeo4jLogger _legacyLogger;
    private readonly Type _loggingType;
    private string _scopePrefix = "";

    public LegacyLoggerAdapter(INeo4jLogger legacyLogger, Type loggingType)
    {
        _legacyLogger = legacyLogger;
        _loggingType = loggingType;
    }

    private string GetModifiedFormat(string messageTemplate)
    {
        // replace "{id}, {name}" with "{0}, {1}""
        var index = 0;
        var indexedFormat = Regex.Replace(messageTemplate, @"\{[^}]+\}", _ => $"{{{index++}}}");

        // add the name of the type that's doing the logging
        var typeName = _loggingType.Name;
        var finalFormat = $"[{typeName}] {_scopePrefix}{indexedFormat}";

        return finalFormat;
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

        var template = GetModifiedFormat(format);
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

        var previous = _scopePrefix;
        _scopePrefix = prefix;
        return new ActionDisposable(() => _scopePrefix = previous);
    }
}

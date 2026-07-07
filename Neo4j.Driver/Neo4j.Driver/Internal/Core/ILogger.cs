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

internal interface ILogger
{
    void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter);

    bool IsEnabled(LogLevel logLevel);

    IDisposable? BeginScope<TState>(TState state) where TState : notnull;
}

internal enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
}

internal record EventId(int Id, string Name);

internal static class LoggerExtensions
{
    private static readonly Func<LogParams, Exception?, string> DefaultFormatter =
        static (state, _) => state?.ToString() ?? "";

    public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        => logger.Log(LogLevel.Debug, eventId, exception, message, args);

    public static void LogDebug(this ILogger logger, EventId eventId, string? message, params object?[] args)
        => logger.Log(LogLevel.Debug, eventId, message, args);

    public static void LogDebug(this ILogger logger, Exception? exception, string? message, params object?[] args)
        => logger.Log(LogLevel.Debug, exception, message, args);

    public static void LogDebug(this ILogger logger, string? message, params object?[] args)
        => logger.Log(LogLevel.Debug, message, args);

    public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        => logger.Log(LogLevel.Trace, eventId, exception, message, args);

    public static void LogTrace(this ILogger logger, EventId eventId, string? message, params object?[] args)
        => logger.Log(LogLevel.Trace, eventId, message, args);

    public static void LogTrace(this ILogger logger, Exception? exception, string? message, params object?[] args)
        => logger.Log(LogLevel.Trace, exception, message, args);

    public static void LogTrace(this ILogger logger, string? message, params object?[] args)
        => logger.Log(LogLevel.Trace, message, args);

    public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        => logger.Log(LogLevel.Information, eventId, exception, message, args);

    public static void LogInformation(this ILogger logger, EventId eventId, string? message, params object?[] args)
        => logger.Log(LogLevel.Information, eventId, message, args);

    public static void LogInformation(this ILogger logger, Exception? exception, string? message, params object?[] args)
        => logger.Log(LogLevel.Information, exception, message, args);

    public static void LogInformation(this ILogger logger, string? message, params object?[] args)
        => logger.Log(LogLevel.Information, message, args);

    public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        => logger.Log(LogLevel.Warning, eventId, exception, message, args);

    public static void LogWarning(this ILogger logger, EventId eventId, string? message, params object?[] args)
        => logger.Log(LogLevel.Warning, eventId, message, args);

    public static void LogWarning(this ILogger logger, Exception? exception, string? message, params object?[] args)
        => logger.Log(LogLevel.Warning, exception, message, args);

    public static void LogWarning(this ILogger logger, string? message, params object?[] args)
        => logger.Log(LogLevel.Warning, message, args);

    public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        => logger.Log(LogLevel.Error, eventId, exception, message, args);

    public static void LogError(this ILogger logger, EventId eventId, string? message, params object?[] args)
        => logger.Log(LogLevel.Error, eventId, message, args);

    public static void LogError(this ILogger logger, Exception? exception, string? message, params object?[] args)
        => logger.Log(LogLevel.Error, exception, message, args);

    public static void LogError(this ILogger logger, string? message, params object?[] args)
        => logger.Log(LogLevel.Error, message, args);

    public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        => logger.Log(LogLevel.Critical, eventId, exception, message, args);

    public static void LogCritical(this ILogger logger, EventId eventId, string? message, params object?[] args)
        => logger.Log(LogLevel.Critical, eventId, message, args);

    public static void LogCritical(this ILogger logger, Exception? exception, string? message, params object?[] args)
        => logger.Log(LogLevel.Critical, exception, message, args);

    public static void LogCritical(this ILogger logger, string? message, params object?[] args)
        => logger.Log(LogLevel.Critical, message, args);

    public static void Log(this ILogger logger, LogLevel logLevel, string? message, params object?[] args)
        => logger.Log(logLevel, new EventId(0, ""), null, message, args);

    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string? message, params object?[] args)
        => logger.Log(logLevel, eventId, null, message, args);

    public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string? message, params object?[] args)
        => logger.Log(logLevel, new EventId(0, ""), exception, message, args);

    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.Log(logLevel, eventId, new LogParams(message ?? "", args), exception, DefaultFormatter);
    }
}

internal class LogParams : IReadOnlyList<KeyValuePair<string, object?>>
{
    private List<KeyValuePair<string,object?>> _extractedList;

    public LogParams(
        string messageTemplate, 
        object?[] args)
    {
        _extractedList = CreateLogFormat(messageTemplate, args);
    }

    private List<KeyValuePair<string, object?>> CreateLogFormat(string messageTemplate, object?[] args)
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
        var matches = Regex.Matches(messageTemplate, @"\{(\w+)\}");
        return matches.Select(m => m.Groups[1].Value).ToList();
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

    public KeyValuePair<string, object?> this[int index]
    {
        get
        {
            return _extractedList
                .Skip(index)
                .First();
        }
    }
}

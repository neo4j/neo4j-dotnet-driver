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

namespace Neo4j.Driver.Internal;

internal class LegacyLoggerAdapter : ILogger
{
    private readonly INeo4jLogger _legacyLogger;
    private readonly Type _loggingType;

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
        var typeName = _loggingType?.Name ?? "Unknown";
        var finalFormat = $"[{typeName}] {indexedFormat}";
        
        return finalFormat;
    }

    public void Trace(string messageTemplate, params object?[] args)
    {
        var format = GetModifiedFormat(messageTemplate);
        _legacyLogger.Trace(format, args);
    }

    public void Debug(string messageTemplate, params object?[] args)
    {
        var format = GetModifiedFormat(messageTemplate);
        _legacyLogger.Debug(format, args);
    }

    public void Info(string messageTemplate, params object?[] args)
    {
        var format = GetModifiedFormat(messageTemplate);
        _legacyLogger.Info(format, args);
    }

    public void Warn(string messageTemplate, params object?[] args)
    {
        var format = GetModifiedFormat(messageTemplate);
        _legacyLogger.Warn(null, format, args);
    }

    public void Error(string messageTemplate, params object?[] args)
    {
        var format = GetModifiedFormat(messageTemplate);
        _legacyLogger.Error(null, format, args);
    }

    public void Error(Exception exception, string messageTemplate, params object?[] args)
    {
        var format = GetModifiedFormat(messageTemplate);
        _legacyLogger.Error(exception, format, args);
    }
}

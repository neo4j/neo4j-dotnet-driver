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

using Microsoft.Extensions.Logging;

namespace Neo4j.Driver.TestKitBackend.Logging;

[RegistrationLifetime(RegistrationLifetime.PerLifetimeScope)]
internal class DriverLoggerAdapter : INeo4jLogger
{
    private const string CategoryName = "Neo4j.Driver";
    private readonly ILogger _logger;

    public DriverLoggerAdapter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(CategoryName);
    }

    public void Error(Exception cause, string message, params object[] args)
    {
        _logger.LogError(cause, message, args);
    }

    public void Warn(Exception cause, string message, params object[] args)
    {
        _logger.LogWarning(cause, message, args);
    }

    public void Info(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    public void Debug(string message, params object[] args)
    {
        _logger.LogDebug(message, args);
    }

    public void Trace(string message, params object[] args)
    {
        _logger.LogTrace(message, args);
    }

    public bool IsTraceEnabled()
    {
        return _logger.IsEnabled(LogLevel.Trace);
    }

    public bool IsDebugEnabled()
    {
        return _logger.IsEnabled(LogLevel.Debug);
    }
}

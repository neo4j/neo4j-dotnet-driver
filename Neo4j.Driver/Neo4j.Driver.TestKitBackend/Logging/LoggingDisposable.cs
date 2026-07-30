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

internal interface ILoggingDisposableFactory
{
    ILoggingDisposable GetLoggingDisposable(string category, string logMsg);
}

internal class LoggingDisposableFactory : ILoggingDisposableFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public LoggingDisposableFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public ILoggingDisposable GetLoggingDisposable(string category, string logMsg)
    {
        return new LoggingDisposable(_loggerFactory, category, logMsg);
    }
}

internal interface ILoggingDisposable : IDisposable;

internal class LoggingDisposable : ILoggingDisposable
{
    private readonly ILogger _logger;
    private readonly string _logMsg;

    public LoggingDisposable(ILoggerFactory loggerFactory, string category, string logMsg)
    {
        _logger = loggerFactory.CreateLogger(category);
        _logMsg = logMsg;
    }

    public void Dispose()
    {
        _logger.LogDebug("{logMsg}", _logMsg);        
    }
}

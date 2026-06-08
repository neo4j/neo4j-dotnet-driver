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

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver.Bolt.Extensions;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.Extensions;

[TestFixture]
internal class LoggerExtensionsTests
{
    [Test]
    public void LogIfDoesNotInvokeArgsWhenLevelDisabled()
    {
        var logger = new MockLogger();
        var argsInvoked = false;

        logger.LogIf(LogLevel.Trace, "Message", () => { argsInvoked = true; return []; });

        argsInvoked.Should().BeFalse();
        logger.LoggingCalls.Should().BeEmpty();
    }

    [Test]
    public void LogIfInvokesArgsAndLogsWhenLevelEnabled()
    {
        Func<object[]> args = () => [42];
        var logger = new MockLogger();
        
        logger.LogIf(LogLevel.Debug, "Message {A}", args);
        
        logger.LoggingCalls.Count.Should().Be(1);
        logger.LoggingCalls[0].LogMessage.Should().Be("Message 42");
    }

    private class MockLogger : ILogger
    {
        private readonly List<LoggingCall> _loggingCalls = [];
        public IReadOnlyList<LoggingCall> LoggingCalls => _loggingCalls;
        
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _loggingCalls.Add(new LoggingCall(logLevel, eventId, state!, exception, formatter(state!, exception)));
            Console.WriteLine($"Log lv: '{logLevel}'; ev: '{eventId}'; st: '{state}'; ex: '{exception?.Message}';");
        }

        public record LoggingCall(
            LogLevel LogLevel,
            EventId EventId,
            object State,
            Exception? Exception,
            string LogMessage);

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
            throw new NotImplementedException();
    }
}

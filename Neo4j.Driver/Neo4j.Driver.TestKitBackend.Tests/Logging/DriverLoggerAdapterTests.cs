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
using Neo4j.Driver.TestKitBackend.Logging;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Logging;

public class DriverLoggerAdapterTests
{
    private readonly RecordingLoggerFactory _loggerFactory = new();
    private readonly DriverLoggerAdapter _adapter;

    public DriverLoggerAdapterTests()
    {
        _adapter = new DriverLoggerAdapter(_loggerFactory);
    }

    [Fact]
    public void Creates_its_logger_under_the_driver_category()
    {
        _loggerFactory.RequestedCategory.Should().Be("DRIVER");
    }

    [Fact]
    public void Error_forwards_the_cause_and_formatted_message_at_error_level()
    {
        var cause = new InvalidOperationException("boom");

        _adapter.Error(cause, "Failed for {0}", "Alice");

        var entry = _loggerFactory.Logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Error);
        entry.Exception.Should().BeSameAs(cause);
        entry.Message.Should().Be("Failed for Alice");
    }

    [Fact]
    public void Warn_forwards_the_cause_and_formatted_message_at_warning_level()
    {
        var cause = new InvalidOperationException("boom");

        _adapter.Warn(cause, "Retrying {0}", "Bob");

        var entry = _loggerFactory.Logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Exception.Should().BeSameAs(cause);
        entry.Message.Should().Be("Retrying Bob");
    }

    [Fact]
    public void Info_forwards_the_formatted_message_at_information_level()
    {
        _adapter.Info("Hello {0}, {1}", "Alice", "Bob");

        var entry = _loggerFactory.Logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Information);
        entry.Exception.Should().BeNull();
        entry.Message.Should().Be("Hello Alice, Bob");
    }

    [Fact]
    public void Debug_forwards_the_formatted_message_at_debug_level()
    {
        _adapter.Debug("Connected to {0}", "server-1");

        var entry = _loggerFactory.Logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Debug);
        entry.Message.Should().Be("Connected to server-1");
    }

    [Fact]
    public void Trace_forwards_the_formatted_message_at_trace_level()
    {
        _adapter.Trace("Raw bytes: {0}", "ff01");

        var entry = _loggerFactory.Logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Trace);
        entry.Message.Should().Be("Raw bytes: ff01");
    }

    [Fact]
    public void IsTraceEnabled_reflects_the_underlying_logger()
    {
        _loggerFactory.Logger.Enabled = false;

        _adapter.IsTraceEnabled().Should().BeFalse();
        _loggerFactory.Logger.EnabledQueries.Should().Equal(LogLevel.Trace);
    }

    [Fact]
    public void IsDebugEnabled_reflects_the_underlying_logger()
    {
        _loggerFactory.Logger.Enabled = false;

        _adapter.IsDebugEnabled().Should().BeFalse();
        _loggerFactory.Logger.EnabledQueries.Should().Equal(LogLevel.Debug);
    }

    private class RecordingLoggerFactory : ILoggerFactory
    {
        public string? RequestedCategory { get; private set; }
        public RecordingLogger Logger { get; } = new();

        public ILogger CreateLogger(string categoryName)
        {
            RequestedCategory = categoryName;
            return Logger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }

    private class RecordingLogger : ILogger
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];
        public List<LogLevel> EnabledQueries { get; } = [];
        public bool Enabled { get; set; } = true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            EnabledQueries.Add(logLevel);
            return Enabled;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }
    }
}

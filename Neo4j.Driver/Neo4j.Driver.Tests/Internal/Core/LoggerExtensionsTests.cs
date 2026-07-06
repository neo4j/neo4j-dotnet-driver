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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Core;

public class LoggerExtensionsTests
{
    private readonly Mock<ILogger> _logger = new();
    private LogParams? _capturedState;

    public LoggerExtensionsTests()
    {
        _logger
            .Setup(
                l => l.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<LogParams>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<LogParams, Exception?, string>>()))
            .Callback<LogLevel, EventId, LogParams, Exception?, Func<LogParams, Exception?, string>>(
                (_, _, state, _, _) => _capturedState = state);
    }

    [Fact]
    public void LogDebug_BuildsLogParamsFromMessageTemplateAndArgs()
    {
        _logger.Object.LogDebug("value is {x}", 42);

        _logger.Verify(
            l => l.Log(
                LogLevel.Debug,
                new EventId(0, ""),
                It.IsAny<LogParams>(),
                null,
                It.IsAny<Func<LogParams, Exception?, string>>()));

        _capturedState.Should().NotBeNull();
        _capturedState!.Should().Equal(
            new System.Collections.Generic.KeyValuePair<string, object?>("{OriginalFormat}", "value is {x}"),
            new System.Collections.Generic.KeyValuePair<string, object?>("x", 42));
    }

    [Fact]
    public void LogWarning_WithException_PassesEventIdAndException()
    {
        var exception = new Exception("boom");
        var eventId = new EventId(7, "custom");

        _logger.Object.LogWarning(eventId, exception, "status {code}", 404);

        _logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                eventId,
                It.IsAny<LogParams>(),
                exception,
                It.IsAny<Func<LogParams, Exception?, string>>()));
    }

    [Fact]
    public void Log_ThrowsWhenLoggerIsNull()
    {
        ILogger? logger = null;

        var act = () => logger!.LogError("boom");

        act.Should().Throw<ArgumentNullException>();
    }
}

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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Core;

public class ContextualLoggerTests
{
    private readonly Mock<ILogger> _downstream = new();
    private readonly LoggingContextTracker _tracker = new();

    private ContextualLogger CreateSubject() => new(_tracker, _downstream.Object);

    [Fact]
    public void IsEnabled_DelegatesToDownstream()
    {
        _downstream.Setup(d => d.IsEnabled(LogLevel.Warning)).Returns(true);
        var subject = CreateSubject();

        subject.IsEnabled(LogLevel.Warning).Should().BeTrue();
    }

    [Fact]
    public void BeginScope_DelegatesToDownstreamAndReturnsSameHandle()
    {
        var handle = Mock.Of<IDisposable>();
        var state = new object();
        _downstream.Setup(d => d.BeginScope(state)).Returns(handle);
        var subject = CreateSubject();

        subject.BeginScope(state).Should().BeSameAs(handle);
    }

    [Fact]
    public void Log_PassesStateExceptionAndFormatterThroughUnchanged()
    {
        var state = new LogParams("value is {x}", [42]);
        var exception = new Exception("boom");
        Func<LogParams, Exception?, string> formatter = (_, _) => "unused";
        _downstream.Setup(d => d.BeginScope(It.IsAny<List<KeyValuePair<string, object?>>>())).Returns(Mock.Of<IDisposable>());
        var subject = CreateSubject();

        subject.Log(LogLevel.Debug, new EventId(1, "test"), state, exception, formatter);

        _downstream.Verify(d => d.Log(LogLevel.Debug, new EventId(1, "test"), state, exception, formatter));
    }

    [Fact]
    public void Log_BeginsDownstreamScopeWithTrackedContextsBeforeLogging()
    {
        _tracker.Add("sid", 456);
        List<KeyValuePair<string, object?>>? capturedScope = null;
        _downstream
            .Setup(d => d.BeginScope(It.IsAny<List<KeyValuePair<string, object?>>>()))
            .Callback<List<KeyValuePair<string, object?>>>(s => capturedScope = s)
            .Returns(Mock.Of<IDisposable>());
        var subject = CreateSubject();

        subject.Log(LogLevel.Debug, new EventId(0, ""), "state", null, (_, _) => "unused");

        capturedScope.Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, object?>("sid", 456));
    }

    [Fact]
    public void Log_DisposesDownstreamScopeAfterLogging()
    {
        var scopeMock = new Mock<IDisposable>();
        _downstream.Setup(d => d.BeginScope(It.IsAny<List<KeyValuePair<string, object?>>>())).Returns(scopeMock.Object);
        var subject = CreateSubject();

        subject.Log(LogLevel.Debug, new EventId(0, ""), "state", null, (_, _) => "unused");

        scopeMock.Verify(s => s.Dispose());
    }
}

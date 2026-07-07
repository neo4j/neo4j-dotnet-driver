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

using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.DependencyInjection;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Core;

public class LoggingInterceptorTests
{
    private readonly Mock<ILoggerFactory> _loggerFactory = new();
    private readonly Mock<IServiceResolver> _resolver = new();
    private readonly LoggingInterceptor _subject;

    public LoggingInterceptorTests()
    {
        _subject = new LoggingInterceptor(_loggerFactory.Object);
    }

    [Fact]
    public void TryResolve_ForNonLoggerType_ReturnsFalse()
    {
        var result = _subject.TryResolve(typeof(string), typeof(LoggingInterceptorTests), _resolver.Object, out var service);

        result.Should().BeFalse();
        service.Should().BeNull();
    }

    [Fact]
    public void TryResolve_ForLoggerType_ResolvesTrackerAndReturnsLoggerFromFactory()
    {
        var tracker = Mock.Of<ILoggingContextTracker>();
        var logger = Mock.Of<ILogger>();
        _resolver.Setup(r => r.Resolve<ILoggingContextTracker>()).Returns(tracker);
        _loggerFactory
            .Setup(f => f.GetLoggerForType(typeof(LoggingInterceptorTests), tracker))
            .Returns(logger);

        var result = _subject.TryResolve(
            typeof(ILogger),
            typeof(LoggingInterceptorTests),
            _resolver.Object,
            out var service);

        result.Should().BeTrue();
        service.Should().BeSameAs(logger);
    }
}

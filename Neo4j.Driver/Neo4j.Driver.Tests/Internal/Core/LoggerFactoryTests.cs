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
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Core;

public class LoggerFactoryTests
{
    private readonly Mock<INeo4jLogger> _legacyLogger;
    private readonly LoggerFactory _subject;

    public LoggerFactoryTests()
    {
        _legacyLogger = new Mock<INeo4jLogger>();
        _legacyLogger.Setup(x => x.IsDebugEnabled()).Returns(true);
        _subject = new LoggerFactory(_legacyLogger.Object);
    }

    [Fact]
    public void GetLoggerForType_LogsWithTypeNamePrefix()
    {
        var logger = _subject.GetLoggerForType(typeof(LoggerFactoryTests), new LoggingContextTracker());

        logger.LogDebug("value is {x}", 42);

        _legacyLogger.Verify(x => x.Debug("[LoggerFactoryTests] value is {0}", It.Is<object[]>(a => a[0].Equals(42))));
    }

    [Fact]
    public void GetLoggerForType_WithTrackedContext_PrefixesMessageWithContext()
    {
        var tracker = new LoggingContextTracker();
        tracker.Add("sid", 456);
        var logger = _subject.GetLoggerForType(typeof(LoggerFactoryTests), tracker);

        logger.LogDebug("value is {x}", 42);

        _legacyLogger.Verify(
            x => x.Debug("[LoggerFactoryTests] [sid:456] value is {0}", It.Is<object[]>(a => a[0].Equals(42))));
    }

    [Fact]
    public void GetLoggerForType_ChildTrackerContext_IncludesParentAndOwnContext()
    {
        var parentTracker = new LoggingContextTracker();
        parentTracker.Add("sid", 456);
        var childTracker = parentTracker.CreateChild();
        childTracker.Add("txId", "tx-1");
        var logger = _subject.GetLoggerForType(typeof(LoggerFactoryTests), childTracker);

        logger.LogDebug("value is {x}", 42);

        _legacyLogger.Verify(
            x => x.Debug(
                "[LoggerFactoryTests] [sid:456] [txId:tx-1] value is {0}",
                It.Is<object[]>(a => a[0].Equals(42))));
    }
}

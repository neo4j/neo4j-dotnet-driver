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
using Neo4j.Driver.TestKitBackend.Logging;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Logging;

public class ShortSourceContextEnricherTests
{
    private static LogEvent NewLogEvent(string? sourceContext)
    {
        var properties = sourceContext is null
            ? Array.Empty<LogEventProperty>()
            : [new LogEventProperty("SourceContext", new ScalarValue(sourceContext))];

        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Debug,
            null,
            new MessageTemplateParser().Parse("test"),
            properties);
    }

    [Fact]
    public void Adds_the_type_name_without_namespace_bracketed_with_a_leading_space()
    {
        var enricher = new ShortSourceContextEnricher();
        var logEvent = NewLogEvent("Neo4j.Driver.TestKitBackend.TestkitConnectionHandler");

        enricher.Enrich(logEvent, null!);

        logEvent.Properties["SourceContextShort"].Should().BeOfType<ScalarValue>()
            .Which.Value.Should().Be(" [TestkitConnectionHandler]");
    }

    [Fact]
    public void Uses_the_whole_name_when_there_is_no_namespace()
    {
        var enricher = new ShortSourceContextEnricher();
        var logEvent = NewLogEvent("TestkitBackend");

        enricher.Enrich(logEvent, null!);

        logEvent.Properties["SourceContextShort"].Should().BeOfType<ScalarValue>()
            .Which.Value.Should().Be(" [TestkitBackend]");
    }

    [Fact]
    public void Adds_no_property_when_there_is_no_SourceContext()
    {
        var enricher = new ShortSourceContextEnricher();
        var logEvent = NewLogEvent(null);

        enricher.Enrich(logEvent, null!);

        logEvent.Properties.ContainsKey("SourceContextShort").Should().BeFalse();
    }
}

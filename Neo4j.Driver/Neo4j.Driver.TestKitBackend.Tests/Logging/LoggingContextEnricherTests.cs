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

using System.Text.Json;
using FluentAssertions;
using Neo4j.Driver.TestKitBackend.Logging;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Logging;

public class LoggingContextEnricherTests
{
    private static LogEvent NewLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Debug,
            null,
            new MessageTemplateParser().Parse("test"),
            []);
    }

    [Fact]
    public void Adds_no_property_when_no_context_is_published()
    {
        var enricher = new LoggingContextEnricher(new LoggingContextAccessor());
        var logEvent = NewLogEvent();

        enricher.Enrich(logEvent, null!);

        logEvent.Properties.ContainsKey("LoggingContext").Should().BeFalse();
    }

    [Fact]
    public void Adds_no_property_when_the_published_context_is_empty()
    {
        var accessor = new LoggingContextAccessor();
        accessor.Publish(new LoggingContext());
        var enricher = new LoggingContextEnricher(accessor);
        var logEvent = NewLogEvent();

        enricher.Enrich(logEvent, null!);

        logEvent.Properties.ContainsKey("LoggingContext").Should().BeFalse();
    }

    [Fact]
    public void Adds_the_published_context_entries_as_a_json_object_with_a_leading_space()
    {
        var accessor = new LoggingContextAccessor();
        var context = new LoggingContext();
        context.Set("ConnectionId", "testkit-1");
        context.Set("test", "some.test");
        accessor.Publish(context);
        var enricher = new LoggingContextEnricher(accessor);
        var logEvent = NewLogEvent();

        enricher.Enrich(logEvent, null!);

        var value = logEvent.Properties["LoggingContext"].Should().BeOfType<ScalarValue>().Which.Value.Should()
            .BeOfType<string>().Which;
        value.Should().StartWith(" ");
        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(value);
        parsed.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["ConnectionId"] = "testkit-1",
            ["test"] = "some.test"
        });
    }
}

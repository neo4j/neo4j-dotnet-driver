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
using Serilog.Formatting.Display;
using Serilog.Parsing;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Logging;

public class TruncatingTextFormatterTests
{
    // Bare message so the rendered length is predictable and driven by the payload.
    private const string OutputTemplate = "{Message:l}";

    [Fact]
    public void Truncates_rendered_output_over_the_limit_and_keeps_a_trailing_newline()
    {
        const int maxLength = 64;
        var payload = new string('x', 5000);
        var formatter = new TruncatingTextFormatter(OutputTemplate, maxLength);
        var output = new StringWriter();

        formatter.Format(EventWithMessage(payload), output);

        var result = output.ToString();
        result.Should().StartWith(new string('x', maxLength));
        result.Should().EndWith(Environment.NewLine);
        result.Length.Should().BeLessThan(payload.Length);
    }

    [Fact]
    public void Passes_rendered_output_within_the_limit_through_unchanged()
    {
        var formatter = new TruncatingTextFormatter(OutputTemplate, maxLength: 2048);
        var logEvent = EventWithMessage("hello");
        var output = new StringWriter();

        formatter.Format(logEvent, output);

        output.ToString().Should().Be(Render(logEvent, OutputTemplate));
    }

    private static LogEvent EventWithMessage(string message)
    {
        return new LogEvent(
            DateTimeOffset.UnixEpoch,
            LogEventLevel.Information,
            exception: null,
            new MessageTemplateParser().Parse("{Payload}"),
            [new LogEventProperty("Payload", new ScalarValue(message))]);
    }

    private static string Render(LogEvent logEvent, string outputTemplate)
    {
        var buffer = new StringWriter();
        new MessageTemplateTextFormatter(outputTemplate).Format(logEvent, buffer);
        return buffer.ToString();
    }
}

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
using Serilog.Formatting;
using Serilog.Parsing;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class TruncatingTextFormatterTests
{
    [Fact]
    public void Truncates_rendered_output_over_the_limit_and_keeps_a_trailing_newline()
    {
        var rendered = new string('x', 5000);
        var formatter = new TruncatingTextFormatter(new StubFormatter(rendered), maxLength: 2048);
        var output = new StringWriter();

        formatter.Format(AnyEvent(), output);

        var result = output.ToString();
        result.Should().StartWith(new string('x', 2048));
        result.Should().EndWith(Environment.NewLine);
        result.TrimEnd(Environment.NewLine.ToCharArray()).Length.Should().BeLessThan(rendered.Length);
    }

    [Fact]
    public void Passes_rendered_output_within_the_limit_through_unchanged()
    {
        const string rendered = "[00:00:00 INF] hello\n";
        var formatter = new TruncatingTextFormatter(new StubFormatter(rendered), maxLength: 2048);
        var output = new StringWriter();

        formatter.Format(AnyEvent(), output);

        output.ToString().Should().Be(rendered);
    }

    private static LogEvent AnyEvent() =>
        new(
            DateTimeOffset.UnixEpoch,
            LogEventLevel.Information,
            exception: null,
            new MessageTemplateParser().Parse("irrelevant"),
            []);

    private class StubFormatter : ITextFormatter
    {
        private readonly string _output;

        public StubFormatter(string output) => _output = output;

        public void Format(LogEvent logEvent, TextWriter output) => output.Write(_output);
    }
}

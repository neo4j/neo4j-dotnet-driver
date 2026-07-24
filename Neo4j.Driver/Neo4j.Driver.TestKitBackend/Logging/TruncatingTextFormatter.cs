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

using Serilog.Events;
using Serilog.Formatting;

namespace Neo4j.Driver.TestKitBackend.Logging;

internal class TruncatingTextFormatter : ITextFormatter
{
    private readonly ITextFormatter _inner;
    private readonly int _maxLength;

    public TruncatingTextFormatter(ITextFormatter inner, int maxLength)
    {
        _inner = inner;
        _maxLength = maxLength;
    }

    public void Format(LogEvent logEvent, TextWriter output)
    {
        var buffer = new StringWriter();
        _inner.Format(logEvent, buffer);
        var rendered = buffer.ToString();

        if (rendered.Length <= _maxLength)
        {
            output.Write(rendered);
            return;
        }

        output.Write(rendered[.._maxLength]);

        var remaining = rendered.Length - _maxLength;
        output.Write($"\\TRUNCATED ({remaining} chars remaining)");
        output.Write(Environment.NewLine);
    }
}

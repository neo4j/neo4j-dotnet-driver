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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Core;

public class LegacyLoggerAdapterTests
{
    private const string TypeName = nameof(LegacyLoggerAdapterTests);
    private readonly Mock<INeo4jLogger> _mockLegacyLogger;
    private readonly LegacyLoggerAdapter _subject;

    public LegacyLoggerAdapterTests()
    {
        _mockLegacyLogger = new Mock<INeo4jLogger>();
        _mockLegacyLogger
            .Setup(x => x.IsDebugEnabled())
            .Returns(true);

        _subject = new LegacyLoggerAdapter(_mockLegacyLogger.Object, typeof(LegacyLoggerAdapterTests));
    }

    [Fact]
    public void Debug_TranslatesNamedPlaceholdersAndDelegates()
    {
        _subject.LogDebug("tx {txId} query {query}", "tx-1", "RETURN 1");

        _mockLegacyLogger
            .Verify(l => l.Debug(
                $$"""[{{TypeName}}] tx {0} query {1}""",
                It.Is<object[]>(a => a[0].Equals("tx-1") && a[1].Equals("RETURN 1"))));
    }

    [Fact]
    public void Info_TranslatesNamedPlaceholdersAndDelegates()
    {
        _subject.LogInformation("version {version}", "5.0");

        _mockLegacyLogger
            .Verify(l => l.Info(
                $$"""[{{TypeName}}] version {0}""",
                It.Is<object[]>(a => a[0].Equals("5.0"))));
    }

    [Fact]
    public void Warn_TranslatesNamedPlaceholdersAndDelegatesWithNullException()
    {
        _subject.LogWarning("status {code}", 404);

        _mockLegacyLogger
            .Verify(l => l.Warn(
                null,
                $$"""[{{TypeName}}] status {0}""",
                It.Is<object[]>(a => a[0].Equals(404))));
    }

    [Fact]
    public void Error_WithoutException_TranslatesAndDelegatesWithNullException()
    {
        _subject.LogError("failed {reason}", "timeout");

        _mockLegacyLogger
            .Verify(l => l.Error(
                null,
                $$"""[{{TypeName}}] failed {0}""",
                It.Is<object[]>(a => a[0].Equals("timeout"))));
    }

    [Fact]
    public void Error_WithException_TranslatesAndDelegatesWithException()
    {
        var ex = new Exception("boom");

        _subject.LogError(ex, "failed {reason}", "timeout");

        _mockLegacyLogger
            .Verify(l => l.Error(
                ex,
                $$"""[{{TypeName}}] failed {0}""",
                It.Is<object[]>(a => a[0].Equals("timeout"))));
    }

    [Fact]
    public void Debug_TemplateWithNoPlaceholders_PassesThroughUnchanged()
    {
        _subject.LogDebug("no placeholders here");

        _mockLegacyLogger
            .Verify(l => l.Debug(
                $$"""[{{TypeName}}] no placeholders here""",
                It.IsAny<object[]>()));
    }

    [Fact]
    public void Debug_PlaceholderWithFormatSpecifier_PairsArgAndPreservesSpecifier()
    {
        _subject.LogDebug("took {ms:D3}ms", 42);

        _mockLegacyLogger
            .Verify(l => l.Debug(
                $$"""[{{TypeName}}] took {0:D3}ms""",
                It.Is<object[]>(a => a.Length == 1 && a[0].Equals(42))));
    }

    [Fact]
    public void Debug_PlaceholderWithAlignment_PairsArgAndPreservesAlignment()
    {
        _subject.LogDebug("value {value,10}", "x");

        _mockLegacyLogger
            .Verify(l => l.Debug(
                $$"""[{{TypeName}}] value {0,10}""",
                It.Is<object[]>(a => a.Length == 1 && a[0].Equals("x"))));
    }

    [Fact]
    public void Debug_WithFewerArgsThanPlaceholders_EscapesUnmatchedPlaceholdersInsteadOfThrowing()
    {
        _subject.LogDebug("{a} then {b}", 1);

        _mockLegacyLogger
            .Verify(l => l.Debug(
                $$$"""[{{{TypeName}}}] {0} then {{b}}""",
                It.Is<object[]>(a => a.Length == 1 && a[0].Equals(1))));
    }

    // The invariant for the whole adapter: whatever template it hands to the legacy logger
    // must survive String.Format with the args it supplies, no matter how hostile the input.
    [Theory]
    [InlineData("plain {name} placeholder", "x")]
    [InlineData("specifier {ms:D3} and alignment {v,10}", 42, "x")]
    [InlineData("stray { open brace")]
    [InlineData("stray } close brace")]
    [InlineData("{not a placeholder!}")]
    [InlineData("mel-style {{escaped}} braces")]
    [InlineData("mel-style {{name}} escaped placeholder", "x")]
    [InlineData("json {\"a\": {\"b\": 1}} payload")]
    [InlineData("{}")]
    [InlineData("under-supplied {a} then {b}", 1)]
    [InlineData("over-supplied {a}", 1, 2)]
    [InlineData("braces in {arg} value", "{value}")]
    public void Debug_AlwaysProducesTemplateThatSurvivesStringFormat(string template, params object[] args)
    {
        string? capturedTemplate = null;
        object[]? capturedArgs = null;
        _mockLegacyLogger
            .Setup(l => l.Debug(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback<string, object[]>((t, a) =>
            {
                capturedTemplate = t;
                capturedArgs = a;
            });

        _subject.LogDebug(template, args);

        capturedTemplate.Should().NotBeNull();
        var act = () => string.Format(capturedTemplate!, capturedArgs!);
        act.Should().NotThrow();
    }

    [Fact]
    public void Debug_WithBracesInScopeContextValue_ProducesTemplateThatSurvivesStringFormat()
    {
        string? capturedTemplate = null;
        object[]? capturedArgs = null;
        _mockLegacyLogger
            .Setup(l => l.Debug(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback<string, object[]>((t, a) =>
            {
                capturedTemplate = t;
                capturedArgs = a;
            });
        var scopeState = new[] { new KeyValuePair<string, object?>("db", "{graph}") };

        using (_subject.BeginScope(scopeState))
        {
            _subject.LogDebug("hello {name}", "x");
        }

        capturedTemplate.Should().NotBeNull();
        var act = () => string.Format(capturedTemplate!, capturedArgs!);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task BeginScope_InOneAsyncFlow_DoesNotLeakIntoAnother()
    {
        var scopeState = new[] { new KeyValuePair<string, object?>("tx", "tx-1") };

        await Task.Run(() =>
        {
            using var scope = _subject.BeginScope(scopeState);
            _subject.LogDebug("inside");
        });

        _subject.LogDebug("outside");

        _mockLegacyLogger.Verify(l => l.Debug($$"""[{{TypeName}}] [tx:tx-1] inside""", It.IsAny<object[]>()));
        _mockLegacyLogger.Verify(l => l.Debug($$"""[{{TypeName}}] outside""", It.IsAny<object[]>()));
    }
}

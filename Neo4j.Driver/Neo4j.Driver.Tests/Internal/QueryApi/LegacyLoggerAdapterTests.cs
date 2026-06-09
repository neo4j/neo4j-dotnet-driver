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
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class LegacyLoggerAdapterTests
{
    private const string TypeName = nameof(LegacyLoggerAdapterTests);
    private readonly Mock<INeo4jLogger> _mockLegacyLogger;
    private readonly LegacyLoggerAdapter _subject;

    public LegacyLoggerAdapterTests()
    {
        _mockLegacyLogger = new Mock<INeo4jLogger>();
        _subject = new LegacyLoggerAdapter(_mockLegacyLogger.Object, typeof(LegacyLoggerAdapterTests));
    }

    [Fact]
    public void Debug_TranslatesNamedPlaceholdersAndDelegates()
    {
        _subject.Debug("tx {txId} query {query}", "tx-1", "RETURN 1");

        _mockLegacyLogger
            .Verify(l => l.Debug(
                $$"""[{{TypeName}}] tx {0} query {1}""",
                It.Is<object[]>(a => a[0].Equals("tx-1") && a[1].Equals("RETURN 1"))));
    }

    [Fact]
    public void Info_TranslatesNamedPlaceholdersAndDelegates()
    {
        _subject.Info("version {version}", "5.0");

        _mockLegacyLogger
            .Verify(l => l.Info(
                $$"""[{{TypeName}}] version {0}""",
                It.Is<object[]>(a => a[0].Equals("5.0"))));
    }

    [Fact]
    public void Warn_TranslatesNamedPlaceholdersAndDelegatesWithNullException()
    {
        _subject.Warn("status {code}", 404);

        _mockLegacyLogger
            .Verify(l => l.Warn(
                null,
                $$"""[{{TypeName}}] status {0}""",
                It.Is<object[]>(a => a[0].Equals(404))));
    }

    [Fact]
    public void Error_WithoutException_TranslatesAndDelegatesWithNullException()
    {
        _subject.Error("failed {reason}", "timeout");

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

        _subject.Error(ex, "failed {reason}", "timeout");

        _mockLegacyLogger
            .Verify(l => l.Error(
                ex,
                $$"""[{{TypeName}}] failed {0}""",
                It.Is<object[]>(a => a[0].Equals("timeout"))));
    }

    [Fact]
    public void Debug_TemplateWithNoPlaceholders_PassesThroughUnchanged()
    {
        _subject.Debug("no placeholders here");

        _mockLegacyLogger
            .Verify(l => l.Debug(
                $$"""[{{TypeName}}] no placeholders here""",
                It.IsAny<object[]>()));
    }
}

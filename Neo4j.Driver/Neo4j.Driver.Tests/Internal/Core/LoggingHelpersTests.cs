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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Core;

public class LoggingHelpersTests
{
    [Fact]
    public void TryBuildScopePrefix_WithContextState_BuildsBracketedPrefix()
    {
        var state = new[]
        {
            new KeyValuePair<string, object?>("txId", "tx-1"),
            new KeyValuePair<string, object?>("dbName", "neo4j")
        };

        var result = LoggingHelpers.TryBuildScopePrefix(state, out var prefix);

        result.Should().BeTrue();
        prefix.Should().Be("[txId:tx-1] [dbName:neo4j] ");
    }

    [Fact]
    public void TryBuildScopePrefix_WithNonContextState_ReturnsFalse()
    {
        var result = LoggingHelpers.TryBuildScopePrefix("not a context", out var prefix);

        result.Should().BeFalse();
        prefix.Should().BeNull();
    }

    [Fact]
    public void ExtractFormatAndArguments_WithLogParamsState_ExtractsFormatAndOrderedArgs()
    {
        var state = new LogParams("hello {name}, you are {age}", ["Bob", 30]);

        var result = LoggingHelpers.ExtractFormatAndArguments(state, out var format, out var args);

        result.Should().BeTrue();
        format.Should().Be("hello {name}, you are {age}");
        args.Should().Equal("Bob", 30);
    }

    [Fact]
    public void ExtractFormatAndArguments_WithNoOriginalFormatKey_DefaultsToEmptyString()
    {
        var state = new[] { new KeyValuePair<string, object>("key", "value") };

        var result = LoggingHelpers.ExtractFormatAndArguments(state, out var format, out var args);

        result.Should().BeTrue();
        format.Should().Be("");
        args.Should().Equal("value");
    }

    [Fact]
    public void ExtractFormatAndArguments_WithNonContextState_ReturnsFalse()
    {
        var result = LoggingHelpers.ExtractFormatAndArguments("not a context", out var format, out var args);

        result.Should().BeFalse();
        format.Should().BeNull();
        args.Should().BeNull();
    }
}

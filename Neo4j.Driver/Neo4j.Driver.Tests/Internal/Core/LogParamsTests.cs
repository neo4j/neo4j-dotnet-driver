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
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Core;

public class LogParamsTests
{
    [Fact]
    public void Constructor_AlwaysIncludesOriginalFormatFirst()
    {
        var subject = new LogParams("tx {txId} query {query}", ["tx-1", "RETURN 1"]);

        subject.Should().HaveCount(3);
        subject[0].Should().Be(new KeyValuePair<string, object?>("{OriginalFormat}", "tx {txId} query {query}"));
        subject[1].Should().Be(new KeyValuePair<string, object?>("txId", "tx-1"));
        subject[2].Should().Be(new KeyValuePair<string, object?>("query", "RETURN 1"));
    }

    [Fact]
    public void Constructor_WithNoPlaceholders_OnlyContainsOriginalFormat()
    {
        var subject = new LogParams("no placeholders here", []);

        subject.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, object?>("{OriginalFormat}", "no placeholders here"));
    }

    [Fact]
    public void Constructor_WithFewerArgsThanPlaceholders_OnlyPairsUpAvailableArgs()
    {
        var subject = new LogParams("a {one} b {two} c {three}", ["1"]);

        subject.Should().HaveCount(2);
        subject[1].Should().Be(new KeyValuePair<string, object?>("one", "1"));
    }

    [Fact]
    public void GetEnumerator_YieldsSameOrderAsIndexer()
    {
        var subject = new LogParams("x {a} y {b}", [1, 2]);

        subject.ToList().Should().Equal(subject[0], subject[1], subject[2]);
    }
}

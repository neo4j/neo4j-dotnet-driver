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
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Core;

public class LoggingContextTrackerTests
{
    [Fact]
    public void Contexts_WhenEmpty_IsEmpty()
    {
        var subject = new LoggingContextTracker();

        subject.Contexts.Should().BeEmpty();
    }

    [Fact]
    public void Add_AddsContextToTracker()
    {
        var subject = new LoggingContextTracker();

        subject.Add("txId", "tx-1");

        subject.Contexts.Should().ContainSingle(c => c.Key == "txId" && c.Value.Equals("tx-1"));
    }

    [Fact]
    public void Add_DisposingHandle_RemovesContext()
    {
        var subject = new LoggingContextTracker();

        var handle = subject.Add("txId", "tx-1");
        handle.Dispose();

        subject.Contexts.Should().BeEmpty();
    }

    [Fact]
    public void CreateChild_IncludesParentContextsBeforeOwnContexts()
    {
        var parent = new LoggingContextTracker();
        parent.Add("dbName", "neo4j");
        var child = parent.CreateChild();

        child.Add("txId", "tx-1");

        child.Contexts.Should().Equal(parent.Contexts[0], child.Contexts[1]);
        child.Contexts.Should().HaveCount(2);
        child.Contexts[0].Key.Should().Be("dbName");
        child.Contexts[1].Key.Should().Be("txId");
    }

    [Fact]
    public void CreateChild_RemovingChildContext_DoesNotAffectParent()
    {
        var parent = new LoggingContextTracker();
        parent.Add("dbName", "neo4j");
        var child = parent.CreateChild();

        var childHandle = child.Add("txId", "tx-1");
        childHandle.Dispose();

        child.Contexts.Should().ContainSingle(c => c.Key == "dbName");
        parent.Contexts.Should().ContainSingle(c => c.Key == "dbName");
    }
}

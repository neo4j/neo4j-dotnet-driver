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

using System;
using FluentAssertions;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

// ReSharper disable once ClassNeverInstantiated.Global
public class NodeWithGuid
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UuidMappingTests
{
    [Fact]
    public void Record_IndexerAccess_ReturnsNativeGuid()
    {
        var guid = Guid.NewGuid();
        var record = TestRecord.Create(["id"], [guid]);

        record["id"].Should().Be(guid);
    }

    [Fact]
    public void Record_TypedGet_ReturnsNativeGuid()
    {
        var guid = Guid.NewGuid();
        var record = TestRecord.Create(["id"], [guid]);

        record.Get<Guid>("id").Should().Be(guid);
    }

    [Fact]
    public void Record_TypedGet_WithKnownValue_ReturnsExpectedGuid()
    {
        var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var record = TestRecord.Create(["id"], [guid]);

        record.Get<Guid>("id").Should().Be(guid);
    }

    [Fact]
    public void ObjectMapping_NativeGuid_MapsToGuidProperty()
    {
        var guid = Guid.NewGuid();
        var record = TestRecord.Create(("Id", guid), ("Name", "Alice"));

        var node = record.AsObject<NodeWithGuid>();

        node.Id.Should().Be(guid);
        node.Name.Should().Be("Alice");
    }

    [Fact]
    public void ObjectMapping_NativeGuid_DoesNotCorruptOtherFields()
    {
        var guid = Guid.NewGuid();
        var record = TestRecord.Create(("Id", guid), ("Name", "Bob"));

        var node = record.AsObject<NodeWithGuid>();

        node.Name.Should().Be("Bob");
    }

    [Fact]
    public void ObjectMapping_StringGuid_MapsToGuidProperty()
    {
        var guid = Guid.NewGuid();
        var record = TestRecord.Create(("Id", guid.ToString()), ("Name", "Carol"));

        var node = record.AsObject<NodeWithGuid>();

        node.Id.Should().Be(guid);
        node.Name.Should().Be("Carol");
    }

    [Fact]
    public void ObjectMapping_StringGuid_UpperCase_MapsToGuidProperty()
    {
        var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var record = TestRecord.Create(("Id", guid.ToString("D").ToUpperInvariant()), ("Name", "Dave"));

        var node = record.AsObject<NodeWithGuid>();

        node.Id.Should().Be(guid);
    }

    // string-to-guid and native-guid mappings should not interfere 

    [Fact]
    public void StringGuidAndNativeGuid_BothMap_WithoutInterfering()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();

        var nativeRecord = TestRecord.Create(("Id", guid1), ("Name", "Native"));
        var stringRecord = TestRecord.Create(("Id", guid2.ToString()), ("Name", "String"));

        var nativeNode = nativeRecord.AsObject<NodeWithGuid>();
        var stringNode = stringRecord.AsObject<NodeWithGuid>();

        nativeNode.Id.Should().Be(guid1);
        stringNode.Id.Should().Be(guid2);
        nativeNode.Id.Should().NotBe(stringNode.Id);
    }

    [Fact]
    public void StringGuidMapping_StillWorksAfterNativeGuidMappingHasBeenUsed()
    {
        var nativeGuid = Guid.NewGuid();
        var stringGuid = Guid.NewGuid();

        // native path first
        var nativeRecord = TestRecord.Create(("Id", nativeGuid), ("Name", "First"));
        nativeRecord.AsObject<NodeWithGuid>();

        // string path must still produce the right result
        var stringRecord = TestRecord.Create(("Id", stringGuid.ToString()), ("Name", "Second"));
        var result = stringRecord.AsObject<NodeWithGuid>();

        result.Id.Should().Be(stringGuid);
    }
}

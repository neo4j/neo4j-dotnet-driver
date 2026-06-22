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
        var guid = Guid.NewGuid();
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
    public void AsGuid_FromNativeGuid_ReturnsCorrectValue()
    {
        var guid = Guid.NewGuid();
        guid.As<Guid>().Should().Be(guid);
    }

    [Fact]
    public void AsGuid_FromString_ParsesSuccessfully()
    {
        var guid = Guid.NewGuid();
        var str = guid.ToString();
        str.As<Guid>().Should().Be(guid);
    }

    [Fact]
    public void AsGuid_FromUpperCaseString_ParsesSuccessfully()
    {
        var guid = Guid.NewGuid();
        guid.ToString("D").ToUpperInvariant().As<Guid>().Should().Be(guid);
    }

    [Fact]
    public void AsGuid_FromRecord_WithStringValue_ReturnsGuid()
    {
        var guid = Guid.NewGuid();
        var record = TestRecord.Create(["id"], [guid.ToString()]);
        record.Get<Guid>("id").Should().Be(guid);
    }

    [Fact]
    public void AsNullableGuid_FromNativeGuid_ReturnsValue()
    {
        var guid = Guid.NewGuid();
        guid.As<Guid?>().Should().Be(guid);
    }

    [Fact]
    public void AsNullableGuid_FromNull_ReturnsNull()
    {
        ((object)null).As<Guid?>(null).Should().BeNull();
    }

    [Fact]
    public void AsString_FromNativeGuid_ReturnsStandardFormatString()
    {
        var guid = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
        guid.As<string>().Should().Be("01234567-89ab-cdef-0123-456789abcdef");
    }

    [Fact]
    public void Record_TypedGet_NullableGuid_FromNativeGuid_ReturnsValue()
    {
        var guid = Guid.NewGuid();
        var record = TestRecord.Create(["id"], [guid]);

        record.Get<Guid?>("id").Should().Be(guid);
    }

    [Fact]
    public void Record_TryGet_NativeGuid_ReturnsTrueAndValue()
    {
        var guid = Guid.NewGuid();
        var record = TestRecord.Create(["id"], [guid]);

        record.TryGet<Guid>("id", out var value).Should().BeTrue();
        value.Should().Be(guid);
    }

    [Fact]
    public void Record_TryGet_NullableNativeGuid_ReturnsTrueAndValue()
    {
        var guid = Guid.NewGuid();
        var record = TestRecord.Create(["id"], [guid]);

        record.TryGet<Guid?>("id", out var value).Should().BeTrue();
        value.Should().Be(guid);
    }

    [Fact]
    public void Record_GetCaseInsensitive_NativeGuid_ReturnsValue()
    {
        var guid = Guid.NewGuid();
        var record = TestRecord.Create(["id"], [guid]);

        record.GetCaseInsensitive<Guid>("ID").Should().Be(guid);
    }

    [Fact]
    public void Record_GetCaseInsensitive_NullableNativeGuid_ReturnsValue()
    {
        var guid = Guid.NewGuid();
        var record = TestRecord.Create(["id"], [guid]);

        record.GetCaseInsensitive<Guid?>("ID").Should().Be(guid);
    }

    [Fact]
    public void AsNullableGuid_FromString_ParsesSuccessfully()
    {
        var guid = Guid.NewGuid();
        guid.ToString().As<Guid?>().Should().Be(guid);
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

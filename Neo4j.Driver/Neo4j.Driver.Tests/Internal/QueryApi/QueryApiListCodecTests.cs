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
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

using static Neo4j.Driver.Tests.Internal.QueryApi.QueryApiCodecAssert;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiListCodecTests
{
    private readonly QueryApiListCodec _subject = new();

    [Fact]
    public void CanRead_CorrectTypes() => CanRead(_subject, "List");

    [Fact]
    public void CanWrite_CorrectTypes() => CanWrite(_subject, typeof(List<object?>), typeof(object[]));

    [Fact]
    public void Write_EmptyList_ReturnsTypedEnvelopeWithEmptyArray()
    {
        var encoder = Mock.Of<IJsonValueEncoder>();

        var result = _subject.Write(new List<object?>(), encoder)!;

        result["$type"]!.GetValue<string>().Should().Be("List");
        result["_value"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public void Write_ElementNodes_AppearInValueArray()
    {
        var items = new List<object?> { 1L, "hello" };
        var encodedInt = new JsonObject { ["$type"] = "Integer", ["_value"] = "1" };
        var encodedStr = new JsonObject { ["$type"] = "String", ["_value"] = "hello" };

        var encoder = new Mock<IJsonValueEncoder>();
        encoder.Setup(e => e.Encode(1L)).Returns(encodedInt);
        encoder.Setup(e => e.Encode("hello")).Returns(encodedStr);

        var result = _subject.Write(items, encoder.Object)!;
        var array = result["_value"]!.AsArray();

        array.Should().HaveCount(2);
        array[0]!.Should().BeTypedEnvelope("Integer", "1");
        array[1]!.Should().BeTypedEnvelope("String", "hello");
    }

    [Fact]
    public void Write_NestedList_RecursesViaEncoder()
    {
        var inner = new List<object?> { 42L };
        var outer = new List<object?> { inner };
        var innerNode = new JsonObject
        {
            ["$type"] = "List",
            ["_value"] = new JsonArray { new JsonObject { ["$type"] = "Integer", ["_value"] = "42" } }
        };

        var encoder = new Mock<IJsonValueEncoder>();
        encoder.Setup(e => e.Encode(inner)).Returns(innerNode);

        var result = _subject.Write(outer, encoder.Object)!;
        var array = result["_value"]!.AsArray();

        array.Should().HaveCount(1);
        array[0].Should().BeSameAs(innerNode);
    }

    [Fact]
    public void Read_EmptyList_ReturnsEmptyList()
    {
        using var doc = JsonDocument.Parse("""{"$type":"List","_value":[]}""");
        var decoder = Mock.Of<IJsonValueDecoder>();

        var result = _subject.Read(doc.RootElement, decoder);

        result.Should().BeEquivalentTo(new List<object?>());
    }

    [Fact]
    public void Read_ReturnsDecodedElements_InOrder()
    {
        using var doc = JsonDocument.Parse("""{"$type":"List","_value":[{"$type":"Integer","_value":"1"},{"$type":"String","_value":"hi"}]}""");
        var decoder = new Mock<IJsonValueDecoder>();
        var elements = doc.RootElement.GetProperty("_value").EnumerateArray().ToList();
        decoder.Setup(d => d.Decode(It.Is<JsonElement>(e => e.GetRawText() == elements[0].GetRawText()))).Returns(1L);
        decoder.Setup(d => d.Decode(It.Is<JsonElement>(e => e.GetRawText() == elements[1].GetRawText()))).Returns("hi");

        var result = (List<object?>)_subject.Read(doc.RootElement, decoder.Object)!;

        result.Should().ContainInOrder(1L, "hi");
    }

    [Fact]
    public void Read_NestedList_RecursesViaDecoder()
    {
        using var doc = JsonDocument.Parse("""{"$type":"List","_value":[{"$type":"List","_value":[{"$type":"Integer","_value":"1"}]}]}""");
        var inner = new List<object?> { 1L };
        var decoder = new Mock<IJsonValueDecoder>();
        decoder.Setup(d => d.Decode(It.IsAny<JsonElement>())).Returns(inner);

        var result = (List<object?>)_subject.Read(doc.RootElement, decoder.Object)!;

        result.Should().HaveCount(1);
        result[0].Should().BeSameAs(inner);
    }

    [Fact]
    public void Read_NullElement_PreservesNull()
    {
        using var doc = JsonDocument.Parse("""{"$type":"List","_value":[{"$type":"Null","_value":null}]}""");
        var decoder = new Mock<IJsonValueDecoder>();
        decoder.Setup(d => d.Decode(It.IsAny<JsonElement>())).Returns((object?)null);

        var result = (List<object?>)_subject.Read(doc.RootElement, decoder.Object)!;

        result.Should().ContainSingle().Which.Should().BeNull();
    }
}

file static class JsonElementExtensions
{
    public static List<JsonElement> ToList(this JsonElement.ArrayEnumerator enumerator)
    {
        var list = new List<JsonElement>();
        foreach (var item in enumerator) list.Add(item);
        return list;
    }
}

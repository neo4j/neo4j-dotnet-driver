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

/// <summary>
/// Unit tests for <see cref="QueryApiMapCodec"/>.
///
/// Wire format (HTTP Query API v1.0):
/// <code>{"$type":"Map", "_value":{"key": &lt;typed-envelope&gt;, ...}}</code>
/// where each value is a complete typed envelope (e.g. <c>{"$type":"Integer","_value":"1"}</c>).
/// Values are encoded/decoded via the injected <see cref="IJsonValueEncoder"/>/<see cref="IJsonValueDecoder"/>
/// so that nested maps and lists are handled recursively. Keys are always plain strings.
/// </summary>
public class QueryApiMapCodecTests
{
    private readonly QueryApiMapCodec _subject = new();

    [Fact]
    public void CanRead_CorrectTypes() => CanRead(_subject, "Map");

    [Fact]
    public void CanWrite_CorrectTypes() => CanWrite(_subject, typeof(IDictionary<string, object?>));

    [Fact]
    public void Write_EmptyMap_ReturnsTypedEnvelopeWithEmptyObject()
    {
        var encoder = Mock.Of<IJsonValueEncoder>();

        var result = _subject.Write(new Dictionary<string, object?>(), encoder)!;

        result["$type"]!.GetValue<string>().Should().Be("Map");
        result["_value"]!.AsObject().Should().BeEmpty();
    }

    [Fact]
    public void Write_EncodesEachValue_UnderItsKey()
    {
        var map = new Dictionary<string, object?> { ["a"] = 1L, ["b"] = "hi" };
        var encodedInt = new JsonObject { ["$type"] = "Integer", ["_value"] = "1" };
        var encodedStr = new JsonObject { ["$type"] = "String", ["_value"] = "hi" };

        var encoder = new Mock<IJsonValueEncoder>();
        encoder.Setup(e => e.Encode(1L)).Returns(encodedInt);
        encoder.Setup(e => e.Encode("hi")).Returns(encodedStr);

        var result = _subject.Write(map, encoder.Object)!;
        var obj = result["_value"]!.AsObject();

        obj.Should().HaveCount(2);
        obj["a"]!.Should().BeTypedEnvelope("Integer", "1");
        obj["b"]!.Should().BeTypedEnvelope("String", "hi");
    }

    [Fact]
    public void Write_NestedMap_RecursesViaEncoder()
    {
        var inner = new Dictionary<string, object?> { ["x"] = 42L };
        var outer = new Dictionary<string, object?> { ["nested"] = inner };
        var innerNode = new JsonObject
        {
            ["$type"] = "Map",
            ["_value"] = new JsonObject { ["x"] = new JsonObject { ["$type"] = "Integer", ["_value"] = "42" } }
        };

        var encoder = new Mock<IJsonValueEncoder>();
        encoder.Setup(e => e.Encode(inner)).Returns(innerNode);

        var result = _subject.Write(outer, encoder.Object)!;
        var obj = result["_value"]!.AsObject();

        obj.Should().HaveCount(1);
        obj["nested"].Should().BeSameAs(innerNode);
    }

    [Fact]
    public void Read_EmptyMap_ReturnsEmptyDictionary()
    {
        using var doc = JsonDocument.Parse("""{"$type":"Map","_value":{}}""");
        var decoder = Mock.Of<IJsonValueDecoder>();

        var result = (Dictionary<string, object?>)_subject.Read(doc.RootElement, decoder)!;

        result.Should().BeEmpty();
    }

    [Fact]
    public void Read_ReturnsDecodedValues_UnderTheirKeys()
    {
        using var doc = JsonDocument.Parse("""{"$type":"Map","_value":{"a":{"$type":"Integer","_value":"1"},"b":{"$type":"String","_value":"hi"}}}""");
        var valueElements = doc.RootElement.GetProperty("_value");
        var aElement = valueElements.GetProperty("a");
        var bElement = valueElements.GetProperty("b");

        var decoder = new Mock<IJsonValueDecoder>();
        decoder.Setup(d => d.Decode(It.Is<JsonElement>(e => e.GetRawText() == aElement.GetRawText()))).Returns(1L);
        decoder.Setup(d => d.Decode(It.Is<JsonElement>(e => e.GetRawText() == bElement.GetRawText()))).Returns("hi");

        var result = (Dictionary<string, object?>)_subject.Read(doc.RootElement, decoder.Object)!;

        result.Should().HaveCount(2);
        result["a"].Should().Be(1L);
        result["b"].Should().Be("hi");
    }

    [Fact]
    public void Read_NestedMap_RecursesViaDecoder()
    {
        using var doc = JsonDocument.Parse("""{"$type":"Map","_value":{"inner":{"$type":"Map","_value":{"x":{"$type":"Integer","_value":"1"}}}}}""");
        var inner = new Dictionary<string, object?> { ["x"] = 1L };
        var decoder = new Mock<IJsonValueDecoder>();
        decoder.Setup(d => d.Decode(It.IsAny<JsonElement>())).Returns(inner);

        var result = (Dictionary<string, object?>)_subject.Read(doc.RootElement, decoder.Object)!;

        result.Should().HaveCount(1);
        result["inner"].Should().BeSameAs(inner);
    }

    [Fact]
    public void Read_NullValue_PreservesNull()
    {
        using var doc = JsonDocument.Parse("""{"$type":"Map","_value":{"key":{"$type":"Null","_value":null}}}""");
        var decoder = new Mock<IJsonValueDecoder>();
        decoder.Setup(d => d.Decode(It.IsAny<JsonElement>())).Returns((object?)null);

        var result = (Dictionary<string, object?>)_subject.Read(doc.RootElement, decoder.Object)!;

        result["key"].Should().BeNull();
    }
}

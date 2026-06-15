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
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Verifies that <see cref="TypedJsonObjectSerializer"/> serializes query parameters in the typed JSON format
/// required by the HTTP Query API v1.0: {"$type": "...", "_value": ...}.
/// </summary>
public class QueryApiJsonSerializerTests
{
    private readonly TypedJsonObjectSerializer _subject = new();

    private string SerializeParams(IDictionary<string, object?> parameters) =>
        _subject.Serialize(new { parameters });

    [Fact]
    public void Serialize_NullParameter_WritesTypedNull()
    {
        var result = SerializeParams(new Dictionary<string, object?> { ["x"] = null });
        result.Should().Contain("""{"$type":"Null","_value":null}""");
    }

    [Theory]
    [InlineData(true, """{"$type":"Boolean","_value":true}""")]
    [InlineData(false, """{"$type":"Boolean","_value":false}""")]
    public void Serialize_BoolParameter_WritesTypedBoolean(bool value, string expected)
    {
        var result = SerializeParams(new Dictionary<string, object?> { ["x"] = value });
        result.Should().Contain(expected);
    }

    [Theory]
    [InlineData(0L, """{"$type":"Integer","_value":"0"}""")]
    [InlineData(42L, """{"$type":"Integer","_value":"42"}""")]
    [InlineData(-1L, """{"$type":"Integer","_value":"-1"}""")]
    public void Serialize_LongParameter_WritesTypedInteger(long value, string expected)
    {
        var result = SerializeParams(new Dictionary<string, object?> { ["x"] = value });
        result.Should().Contain(expected);
    }

    [Fact]
    public void Serialize_StringParameter_WritesTypedString()
    {
        var result = SerializeParams(new Dictionary<string, object?> { ["x"] = "hello" });
        result.Should().Contain("""{"$type":"String","_value":"hello"}""");
    }

    [Fact]
    public void Serialize_FloatParameter_WritesTypedFloat()
    {
        var result = SerializeParams(new Dictionary<string, object?> { ["x"] = 3.14 });
        result.Should().Contain("""{"$type":"Float","_value":""");
    }

    [Fact]
    public void Serialize_ListParameter_WritesTypedList()
    {
        var result = SerializeParams(new Dictionary<string, object?>
        {
            ["x"] = new List<object?> { 1L, "a" }
        });

        result.Should().Contain("""{"$type":"List","_value":[""");
        result.Should().Contain("""{"$type":"Integer","_value":"1"}""");
        result.Should().Contain("""{"$type":"String","_value":"a"}""");
    }

    [Fact]
    public void Serialize_MapParameter_WritesTypedMap()
    {
        var result = SerializeParams(new Dictionary<string, object?>
        {
            ["x"] = new Dictionary<string, object?> { ["k"] = 1L }
        });

        result.Should().Contain("""{"$type":"Map","_value":""");
        result.Should().Contain("""{"$type":"Integer","_value":"1"}""");
    }

    [Fact]
    public void Serialize_MultipleParameters_WritesAllTyped()
    {
        var result = SerializeParams(new Dictionary<string, object?>
        {
            ["n"] = 1L,
            ["s"] = "hello",
            ["b"] = true
        });

        result.Should().Contain("""{"$type":"Integer","_value":"1"}""");
        result.Should().Contain("""{"$type":"String","_value":"hello"}""");
        result.Should().Contain("""{"$type":"Boolean","_value":true}""");
    }
}

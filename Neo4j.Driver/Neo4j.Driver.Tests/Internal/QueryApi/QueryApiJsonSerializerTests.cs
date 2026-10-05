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
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Tests for the concern-A plain (de)serializer. Concern-A owns HTTP-message serialization:
/// plain STJ round-trips of DTOs like RequestBody and QueryApiResultSet, with no knowledge of
/// Neo4j typed envelopes.
/// </summary>
public class QueryApiJsonSerializerTests
{
    private class TestConverter(object? expected) : JsonConverter<object?>, IQueryApiJsonConverter
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return expected!;
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            value.Should().Be(expected);
        }
    }

    private readonly Mock<IRequiredMediaVersionCalculator> _calculator = new();

    private QueryApiJsonSerializer Subject() => new([new TestConverter(0)], _calculator.Object);

    private record Body(string Statement, string? AccessMode = null) : IQueryApiRequestBody
    {
        public IReadOnlyCollection<object?> ParameterValues { private get; init; } = [];

        public IReadOnlyCollection<object?> GetParameterValues() => ParameterValues;
    }

    [Fact]
    public void Serialize_WritesPropertiesAsCamelCase()
    {
        var result = Subject().Serialize(new Body("MATCH (n) RETURN n", "READ"));

        result.Json.Should().Be("""{"statement":"MATCH (n) RETURN n","accessMode":"READ"}""");
    }

    [Fact]
    public void Serialize_OmitsNullProperties()
    {
        var result = Subject().Serialize(new Body("RETURN 1"));

        result.Json.Should().Be("""{"statement":"RETURN 1"}""");
    }

    [Fact]
    public void Serialize_ReportsVersionFromCalculatorOverParameterValues()
    {
        var values = new object?[] { 0 };
        _calculator.Setup(c => c.Calculate(values)).Returns(QueryApiMediaVersion.V1_1);

        var result = Subject().Serialize(new Body("RETURN $v") { ParameterValues = values });

        result.Version.Should().Be(QueryApiMediaVersion.V1_1);
    }
}

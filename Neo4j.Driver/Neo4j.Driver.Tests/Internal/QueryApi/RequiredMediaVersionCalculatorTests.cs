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
using System.Text.Json.Nodes;
using FluentAssertions;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class RequiredMediaVersionCalculatorTests
{
    private static readonly object ClassicValue1_0 = new();
    private static readonly object ValueThatRequires1_1 = new();

    private static IRequiredMediaVersionCalculator Calculator()
    {
        return new RequiredMediaVersionCalculator(
        [
            new FakeCodec(ClassicValue1_0, QueryApiMediaVersion.V1_0),
            new FakeCodec(ValueThatRequires1_1, QueryApiMediaVersion.V1_1)
        ]);
    }

    [Fact]
    public void Empty_ReturnsV1_0()
    {
        Calculator().Calculate([]).Should().Be(QueryApiMediaVersion.V1_0);
    }

    [Fact]
    public void OnlyV1_0Values_ReturnV1_0()
    {
        Calculator().Calculate([ClassicValue1_0, ClassicValue1_0]).Should().Be(QueryApiMediaVersion.V1_0);
    }

    [Fact]
    public void TopLevelV1_1Value_ReturnsV1_1()
    {
        Calculator().Calculate([ClassicValue1_0, ValueThatRequires1_1]).Should().Be(QueryApiMediaVersion.V1_1);
    }

    [Fact]
    public void V1_1ValueNestedInList_ReturnsV1_1()
    {
        var listWithV1_1 = new List<object?> { ClassicValue1_0, ValueThatRequires1_1 };

        Calculator().Calculate([listWithV1_1]).Should().Be(QueryApiMediaVersion.V1_1);
    }

    [Fact]
    public void V1_1ValueNestedInMap_ReturnsV1_1()
    {
        var mapWithV1_1 = new Dictionary<string, object?> { ["key"] = ValueThatRequires1_1 };

        Calculator().Calculate([mapWithV1_1]).Should().Be(QueryApiMediaVersion.V1_1);
    }

    [Fact]
    public void V1_1ValueNestedSeveralLevelsDeep_ReturnsV1_1()
    {
        var deeplyNested = new Dictionary<string, object?>
        {
            ["outer"] = new List<object?>
            {
                ClassicValue1_0,
                new Dictionary<string, object?> { ["inner"] = ValueThatRequires1_1 }
            }
        };

        Calculator().Calculate([deeplyNested]).Should().Be(QueryApiMediaVersion.V1_1);
    }

    [Fact]
    public void DeeplyNestedV1_0Values_ReturnV1_0()
    {
        var deeplyNested = new Dictionary<string, object?>
        {
            ["outer"] = new List<object?>
            {
                ClassicValue1_0,
                new Dictionary<string, object?> { ["inner"] = ClassicValue1_0 }
            }
        };

        Calculator().Calculate([deeplyNested]).Should().Be(QueryApiMediaVersion.V1_0);
    }

    private sealed class FakeCodec : IQueryApiTypeCodec
    {
        private readonly object? _writableValue;
        private readonly QueryApiMediaVersion _requiredVersion;

        public FakeCodec(object? writableValue, QueryApiMediaVersion requiredVersion)
        {
            _writableValue = writableValue;
            _requiredVersion = requiredVersion;
        }

        public QueryApiMediaVersion RequiredVersion => _requiredVersion;

        public bool CanWrite(object? value) => ReferenceEquals(value, _writableValue);

        public bool CanRead(string typeName) => false;

        public object? Read(JsonElement element, IJsonValueDecoder recurse) => throw new NotSupportedException();

        public JsonNode? Write(object? value, IJsonValueEncoder recurse) => throw new NotSupportedException();
    }
}

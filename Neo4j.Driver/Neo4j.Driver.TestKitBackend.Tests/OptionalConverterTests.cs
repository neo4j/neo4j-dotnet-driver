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

using System.Text.Json;
using FluentAssertions;
using Neo4j.Driver.TestKitBackend.Protocol;
using Neo4j.Driver.TestKitBackend.Types;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class OptionalConverterTests
{
    private static JsonSerializerOptions Options()
    {
        return new JsonSerializerOptions { Converters = { new OptionalConverterFactory() } };
    }

    [Fact]
    public void Reads_a_present_value_as_Specified()
    {
        var opt = JsonSerializer.Deserialize<IOptional<string>>("\"hello\"", Options());

        opt.Should().BeOfType<Specified<string>>().Which.Value.Should().Be("hello");
    }

    [Fact]
    public void Reads_a_present_null_as_Specified_with_a_null_value()
    {
        var opt = JsonSerializer.Deserialize<IOptional<string>>("null", Options());

        opt.Should().BeOfType<Specified<string>>().Which.Value.Should().BeNull();
    }

    [Fact]
    public void Reads_a_present_null_for_a_nullable_value_type_as_Specified_null()
    {
        var opt = JsonSerializer.Deserialize<IOptional<int?>>("null", Options());

        opt.Should().BeOfType<Specified<int?>>().Which.Value.Should().BeNull();
    }

    [Fact]
    public void Rejects_a_present_null_for_a_non_nullable_value_type()
    {
        var read = () => JsonSerializer.Deserialize<IOptional<int>>("null", Options());

        read.Should().Throw<JsonException>();
    }
}

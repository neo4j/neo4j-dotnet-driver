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
using Neo4j.Driver.TestKitBackend.Serialization;
using Neo4j.Driver.TestKitBackend.Types;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Serialization;

public class OptionalConverterTests
{
    private static JsonSerializerOptions Options()
    {
        return new JsonSerializerOptions { Converters = { new OptionalConverterFactory() } };
    }

    [Fact]
    public void Reads_a_present_value_as_specified()
    {
        var opt = JsonSerializer.Deserialize<Optional<long?>>("5", Options());

        opt.IsSpecified(out var value).Should().BeTrue();
        value.Should().Be(5L);
    }

    [Fact]
    public void Reads_a_present_null_as_specified_with_a_null_value()
    {
        var opt = JsonSerializer.Deserialize<Optional<long?>>("null", Options());

        opt.IsSpecified(out var value).Should().BeTrue();
        value.Should().BeNull();
    }

    [Fact]
    public void Rejects_a_present_null_for_a_non_nullable_value_type()
    {
        var read = () => JsonSerializer.Deserialize<Optional<long>>("null", Options());

        read.Should().Throw<JsonException>();
    }

    [Fact]
    public void An_absent_optional_property_is_not_specified()
    {
        var message = JsonSerializer.Deserialize<Message>("{}", Options());

        message!.Timeout.IsSpecified(out _).Should().BeFalse();
    }

    [Fact]
    public void A_present_optional_property_is_specified()
    {
        var message = JsonSerializer.Deserialize<Message>("""{"Timeout":5}""", Options());

        message!.Timeout.IsSpecified(out var value).Should().BeTrue();
        value.Should().Be(5L);
    }

    private record Message
    {
        public Optional<long?> Timeout { get; init; }
    }
}

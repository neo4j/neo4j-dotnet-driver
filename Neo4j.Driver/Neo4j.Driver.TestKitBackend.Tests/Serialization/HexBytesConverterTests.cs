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

public class HexBytesConverterTests
{
    private static JsonSerializerOptions Options()
    {
        return new JsonSerializerOptions { Converters = { new HexBytesConverter() } };
    }

    [Fact]
    public void Reads_a_plain_hex_string()
    {
        var hex = JsonSerializer.Deserialize<HexBytes>("\"deadbeef\"", Options());

        hex.Value.Should().Equal(0xDE, 0xAD, 0xBE, 0xEF);
    }

    [Fact]
    public void Reads_a_space_separated_hex_string()
    {
        var hex = JsonSerializer.Deserialize<HexBytes>("\"de ad be ef\"", Options());

        hex.Value.Should().Equal(0xDE, 0xAD, 0xBE, 0xEF);
    }

    [Fact]
    public void Reads_an_empty_string_as_empty_bytes()
    {
        var hex = JsonSerializer.Deserialize<HexBytes>("\"\"", Options());

        hex.Value.Should().BeEmpty();
    }

    [Fact]
    public void Writes_lowercase_hex_without_spaces()
    {
        var json = JsonSerializer.Serialize(new HexBytes([0xDE, 0xAD, 0xBE, 0xEF]), Options());

        json.Should().Be("\"deadbeef\"");
    }
}

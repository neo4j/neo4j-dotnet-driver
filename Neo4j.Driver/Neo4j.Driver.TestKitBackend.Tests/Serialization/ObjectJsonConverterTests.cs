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
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Serialization;

public class ObjectJsonConverterTests
{
    private static JsonSerializerOptions Options()
    {
        return new JsonSerializerOptions { Converters = { new ObjectJsonConverter() } };
    }

    [Fact]
    public void Reads_untyped_dictionary_values_as_native_scalars()
    {
        const string json = """{"sky?":"no","my eyes":0.1,"da be dee da be daa?":true}""";

        var values = JsonSerializer.Deserialize<Dictionary<string, object>>(json, Options());

        values.Should().Equal(
            new Dictionary<string, object>
            {
                ["sky?"] = "no",
                ["my eyes"] = 0.1,
                ["da be dee da be daa?"] = true
            });
    }

    [Fact]
    public void Reads_a_null_value_as_null()
    {
        var value = JsonSerializer.Deserialize<object>("null", Options());

        value.Should().BeNull();
    }
}

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

using FluentAssertions;
using Neo4j.Driver.TestKitBackend.Cypher;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Cypher;

public class CypherToNativeMapperTests
{
    private readonly CypherToNativeMapper _mapper = new();

    [Fact]
    public void Maps_cypher_null_to_null()
    {
        _mapper.Map(new CypherNull()).Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Maps_cypher_bool_to_bool(bool value)
    {
        _mapper.Map(new CypherBool(value)).Should().Be(value);
    }

    [Fact]
    public void Maps_cypher_int_to_long()
    {
        _mapper.Map(new CypherInt(42)).Should().Be(42L);
    }

    [Fact]
    public void Maps_cypher_float_to_double()
    {
        _mapper.Map(new CypherFloat(1.5)).Should().Be(1.5);
    }

    [Fact]
    public void Maps_cypher_string_to_string()
    {
        _mapper.Map(new CypherString("hello")).Should().Be("hello");
    }

    [Fact]
    public void Throws_for_an_unmapped_cypher_type_naming_the_type()
    {
        var act = () => _mapper.Map(new CypherList([]));

        act.Should().Throw<NotSupportedException>().WithMessage("*CypherList*");
    }
}

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
using Neo4j.Driver.TestKitBackend.Types;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class OptionalTests
{
    [Fact]
    public void Missing_pattern_returns_Missing()
    {
        var opt = Optional.Missing<int>();
        opt.Should().BeAssignableTo<Missing>();
    }

    [Fact]
    public void Specified_carries_its_value()
    {
        var opt = Optional.Specified(42);
        opt.Should().BeOfType<Specified<int>>().Which.Value.Should().Be(42);
    }

    [Fact]
    public void Specified_with_a_null_value_is_still_Specified_not_Missing()
    {
        var opt = Optional.Specified<string?>(null);
        opt.Should().BeOfType<Specified<string?>>().Which.Value.Should().BeNull();
    }
}

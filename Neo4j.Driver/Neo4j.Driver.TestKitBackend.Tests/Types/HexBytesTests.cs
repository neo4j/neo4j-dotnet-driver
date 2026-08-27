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

namespace Neo4j.Driver.TestKitBackend.Tests.Types;

public class HexBytesTests
{
    [Fact]
    public void Equal_when_the_underlying_bytes_match_by_content()
    {
        var a = new HexBytes([1, 2, 3]);
        var b = new HexBytes([1, 2, 3]);

        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Not_equal_when_the_underlying_bytes_differ()
    {
        var a = new HexBytes([1, 2, 3]);
        var b = new HexBytes([1, 2, 4]);

        a.Equals(b).Should().BeFalse();
    }
}

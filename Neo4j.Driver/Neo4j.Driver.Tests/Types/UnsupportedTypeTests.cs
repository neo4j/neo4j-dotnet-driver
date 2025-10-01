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
using Xunit;

namespace Neo4j.Driver.Tests.Types;

public class UnsupportedTypeTests
{
    [Theory]
    [InlineData("QuantumEntity", 9, 1, "QuantumEntity type not supported", "9.1")]
    [InlineData("TemporalVortex", 7, 3, "TemporalVortex type not supported", "7.3")]
    [InlineData("HyperEdge", 8, 0, "HyperEdge type not supported", "8.0")]
    [InlineData("MetaNode", 10, 2, "MetaNode type not supported", "10.2")]
    public void ShouldCreate(
        string name,
        int minimumProtocolMajor,
        int minimumProtocolMinor,
        string message,
        string expectedMinimumProtocolVersion)
    {
        var unsupportedType = new UnsupportedType(name, minimumProtocolMajor, minimumProtocolMinor, message);
        unsupportedType.Message.Should().Be(message);
        unsupportedType.Name.Should().Be(name);
        unsupportedType.MinimumProtocolVersion.Should().Be(expectedMinimumProtocolVersion);
    }
    
    [Fact]
    public void ShouldGiveCorrectStringRepresentation()
    {
        var unsupportedType = new UnsupportedType("QuantumEntity", 9, 1, "QuantumEntity type not supported");
        unsupportedType.ToString().Should().Be("UnsupportedType(QuantumEntity)");
    }
}

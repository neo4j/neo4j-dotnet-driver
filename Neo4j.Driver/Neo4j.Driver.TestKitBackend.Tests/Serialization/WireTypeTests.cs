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

using Neo4j.Driver.TestKitBackend.Serialization;
namespace Neo4j.Driver.TestKitBackend.Tests.Serialization;

public class WireTypeTests
{
    private readonly WireTypeNameProvider _nameProvider = new();

    [Fact]
    public void InboundTypeName_strips_the_Request_suffix()
    {
        _nameProvider.GetInboundTypeName(typeof(PingRequest)).Should().Be("Ping");
    }

    [Fact]
    public void InboundTypeName_leaves_an_unsuffixed_name_unchanged()
    {
        _nameProvider.GetInboundTypeName(typeof(Plain)).Should().Be("Plain");
    }

    [Fact]
    public void An_overridden_name_wins_over_the_default()
    {
        _nameProvider.GetInboundTypeName(typeof(RenamedRequest)).Should().Be("InboundName");
    }

    private record PingRequest : IWireType;

    private record Plain : IWireType;

    private record RenamedRequest : IWireType
    {
        public string InboundTypeName => "InboundName";
    }
}

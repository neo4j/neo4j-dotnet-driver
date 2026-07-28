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
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class WireNameProviderTests
{
    private readonly WireNameProvider _provider = new();

    [Fact]
    public void GetRequestWireName_strips_the_Request_suffix()
    {
        _provider.GetRequestWireName(typeof(PingRequest)).Should().Be("Ping");
    }

    [Fact]
    public void GetRequestWireName_leaves_an_unsuffixed_name_unchanged()
    {
        _provider.GetRequestWireName(typeof(Plain)).Should().Be("Plain");
    }

    [Fact]
    public void GetResponseWireName_strips_the_Response_suffix()
    {
        _provider.GetResponseWireName(typeof(PongResponse)).Should().Be("Pong");
    }

    [Fact]
    public void GetResponseWireName_leaves_an_unsuffixed_name_unchanged()
    {
        _provider.GetResponseWireName(typeof(Plain)).Should().Be("Plain");
    }

    private record PingRequest;

    private record PongResponse;

    private record Plain;
}

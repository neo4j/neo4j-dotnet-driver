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
using Neo4j.Driver.Tests.TestBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.Tests.TestBackend.Tests;

public class MessageRegistryTests
{
    [Fact]
    public void FromAssembly_discovers_every_concrete_message_type_by_name()
    {
        var registry = MessageRegistry.FromAssembly(typeof(MessageRegistryTests).Assembly);

        registry.Resolve(nameof(FirstSampleMessage)).Should().Be(typeof(FirstSampleMessage));
        registry.Resolve(nameof(SecondSampleMessage)).Should().Be(typeof(SecondSampleMessage));
    }

    [Fact]
    public void FromAssembly_ignores_types_that_do_not_implement_the_marker()
    {
        var registry = MessageRegistry.FromAssembly(typeof(MessageRegistryTests).Assembly);

        var resolve = () => registry.Resolve(nameof(NotAMessage));

        resolve.Should().Throw<TestKitProtocolException>();
    }

    private record FirstSampleMessage : IProtocolMessage;

    private record SecondSampleMessage : IProtocolMessage;

    private class NotAMessage;
}

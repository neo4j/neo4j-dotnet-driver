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

#nullable enable

using System;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class EncryptionProfileRegistryTests
{
    private static IEncryptionProfile Profile(string name)
    {
        return Mock.Of<IEncryptionProfile>(p => p.Name == name);
    }

    [Fact]
    public void Get_ReturnsTheProfileWithTheGivenName()
    {
        var wanted = Profile("b");
        var registry = new EncryptionProfileRegistry([Profile("a"), wanted, Profile("c")]);

        registry.Get("b").Should().BeSameAs(wanted);
    }

    [Fact]
    public void Get_ThrowsWhenTheNamedProfileIsUnknown()
    {
        var registry = new EncryptionProfileRegistry([Profile("a")]);

        var act = () => registry.Get("missing");

        act.Should().Throw<EncryptionProfileNotFoundException>();
    }

    [Fact]
    public void Get_WithNullName_ReturnsTheSoleProfile()
    {
        var only = Profile("a");
        var registry = new EncryptionProfileRegistry([only]);

        registry.Get(null).Should().BeSameAs(only);
    }

    [Fact]
    public void Get_WithNullName_ThrowsWhenNoProfilesAreConfigured()
    {
        var registry = new EncryptionProfileRegistry([]);

        var act = () => registry.Get(null);

        act.Should().Throw<DefaultEncryptionProfileNotFoundException>();
    }

    [Fact]
    public void Get_WithNullName_ThrowsWhenMultipleProfilesAreConfigured()
    {
        var registry = new EncryptionProfileRegistry([Profile("a"), Profile("b")]);

        var act = () => registry.Get(null);

        act.Should().Throw<AmbiguousEncryptionProfileException>();
    }

    [Fact]
    public void Construction_ThrowsWhenProfileNamesAreDuplicated()
    {
        var act = () => new EncryptionProfileRegistry([Profile("dup"), Profile("dup")]);

        act.Should().Throw<ArgumentException>();
    }
}

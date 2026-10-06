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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Preview.Encryption;
using Xunit;
using static Neo4j.Driver.Tests.Internal.Encryption.EncryptionTestHelpers;

namespace Neo4j.Driver.Tests.Public.Preview.Encryption;

public class KeyEncapsulationServicesTests
{
    private static readonly byte[] MasterKey = Sequence(32, seed: 0x50);

    private record NoOptions : IKeyEncapsulationOptions
    {
        public IReadOnlyDictionary<string, string> ToMap()
        {
            return new Dictionary<string, string>();
        }
    }

    [Fact]
    public async Task Local_ReturnsAServiceThatUnwrapsWhatItWrapped()
    {
        var subject = KeyEncapsulationServices.Local(MasterKey);

        var encapsulated = await subject.EncapsulateAsync(
            new NoOptions(),
            TestContext.Current.CancellationToken);

        var unwrapped = await subject.DecapsulateAsync(
            encapsulated.Encapsulation,
            encapsulated.Metadata,
            TestContext.Current.CancellationToken);

        unwrapped.Should().Equal(encapsulated.Key);
    }

    [Fact]
    public async Task Local_GeneratesADistinctDataKeyPerCall()
    {
        var subject = KeyEncapsulationServices.Local(MasterKey);

        var first = await subject.EncapsulateAsync(new NoOptions(), TestContext.Current.CancellationToken);
        var second = await subject.EncapsulateAsync(new NoOptions(), TestContext.Current.CancellationToken);

        first.Key.Should().NotEqual(second.Key);
    }

    [Fact]
    public async Task Local_CannotUnwrapAnEncapsulationMadeUnderADifferentMasterKey()
    {
        var encapsulated = await KeyEncapsulationServices.Local(MasterKey)
            .EncapsulateAsync(new NoOptions(), TestContext.Current.CancellationToken);

        var otherService = KeyEncapsulationServices.Local(Sequence(32, seed: 0x90));
        var act = async () => await otherService.DecapsulateAsync(
            encapsulated.Encapsulation,
            encapsulated.Metadata,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Local_IsUnaffectedByLaterChangesToTheCallersMasterKey()
    {
        var token = TestContext.Current.CancellationToken;
        var masterKey = Sequence(32, seed: 0x50);
        var subject = KeyEncapsulationServices.Local(masterKey);
        var encapsulated = await subject.EncapsulateAsync(new NoOptions(), token);

        Array.Clear(masterKey);

        var unwrapped = await subject.DecapsulateAsync(encapsulated.Encapsulation, encapsulated.Metadata, token);
        unwrapped.Should().Equal(encapsulated.Key);
    }

    [Fact]
    public void Local_WithANullMasterKey_Throws()
    {
        var act = () => KeyEncapsulationServices.Local(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(16)]
    [InlineData(31)]
    [InlineData(33)]
    public void Local_WithAMasterKeyThatIsNotAes256_Throws(int length)
    {
        var act = () => KeyEncapsulationServices.Local(Sequence((byte)length));

        act.Should().Throw<ArgumentException>().WithMessage("*32*");
    }
}

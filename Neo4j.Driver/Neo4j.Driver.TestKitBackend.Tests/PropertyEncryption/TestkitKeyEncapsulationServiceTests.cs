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
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.PropertyEncryption;

public class TestkitKeyEncapsulationServiceTests
{
    private static readonly byte[] Kek = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
    private static readonly byte[] OtherKek = Enumerable.Range(100, 32).Select(i => (byte)i).ToArray();

    private static readonly IKeyEncapsulationOptions NoOptions = new EmptyOptions();

    private static TestkitKeyEncapsulationService Service(byte[]? kek = null)
    {
        return new TestkitKeyEncapsulationService(kek ?? Kek);
    }

    private static Task<EncapsulationResult> Encapsulate(TestkitKeyEncapsulationService service)
    {
        return service.EncapsulateAsync(NoOptions, TestContext.Current.CancellationToken);
    }

    private static Task<byte[]> Decapsulate(TestkitKeyEncapsulationService service, EncapsulationResult result)
    {
        return service.DecapsulateAsync(
            result.Encapsulation,
            result.Options.ToMap(),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Round_trips_the_data_key()
    {
        var service = Service();
        var encapsulated = await Encapsulate(service);

        var decapsulated = await Decapsulate(service, encapsulated);

        decapsulated.Should().Equal(encapsulated.Key);
    }

    [Fact]
    public async Task Wraps_the_data_key_rather_than_storing_it()
    {
        var encapsulated = await Encapsulate(Service());

        var wrappedKey = encapsulated.Encapsulation[..encapsulated.Key.Length];

        wrappedKey.Should().NotEqual(encapsulated.Key);
    }

    [Fact]
    public async Task Generates_a_distinct_data_key_each_time()
    {
        var service = Service();

        var first = await Encapsulate(service);
        var second = await Encapsulate(service);

        second.Key.Should().NotEqual(first.Key);
    }

    [Fact]
    public async Task Records_the_wrap_iv_in_the_options()
    {
        var encapsulated = await Encapsulate(Service());

        var iv = Convert.FromBase64String(encapsulated.Options.ToMap()["iv"]);

        iv.Should().HaveCount(12);
    }

    [Fact]
    public async Task Cannot_decapsulate_a_key_wrapped_under_a_different_kek()
    {
        var encapsulated = await Encapsulate(Service());

        var act = () => Decapsulate(Service(OtherKek), encapsulated);

        await act.Should().ThrowAsync<System.Security.Cryptography.CryptographicException>();
    }

    [Fact]
    public async Task Shares_keys_between_services_holding_the_same_kek()
    {
        var encapsulated = await Encapsulate(Service());

        var decapsulated = await Decapsulate(Service(), encapsulated);

        decapsulated.Should().Equal(encapsulated.Key);
    }

    [Fact]
    public async Task Generates_its_own_kek_when_none_is_supplied()
    {
        var encapsulated = await Encapsulate(new TestkitKeyEncapsulationService(null));

        var act = () => Decapsulate(new TestkitKeyEncapsulationService(null), encapsulated);

        await act.Should().ThrowAsync<System.Security.Cryptography.CryptographicException>();
    }

    private record EmptyOptions : IKeyEncapsulationOptions
    {
        public IReadOnlyDictionary<string, string> ToMap()
        {
            return new Dictionary<string, string>();
        }
    }
}

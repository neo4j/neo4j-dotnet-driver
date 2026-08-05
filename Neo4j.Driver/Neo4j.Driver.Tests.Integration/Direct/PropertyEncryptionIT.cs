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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.IntegrationTests.Direct;

public sealed class PropertyEncryptionIT : DirectDriverTestBase
{
    private static readonly byte[] Kek = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

    public PropertyEncryptionIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
        : base(output, fixture)
    {
    }

    [RequireServerFact]
    public async Task EncryptedBytes_RoundTripThroughARealDatabase()
    {
        var token = TestContext.Current.CancellationToken;

        var kes = new LocalKeyEncapsulationService(
            Kek,
            new AesGcmCipher(),
            new CryptoRandomProvider(),
            new Base64Codec());

        var repository = new InMemoryEncapsulatedKeyRepository(new KeyIdGenerator());
        var profile = PropertyEncryptionProfile.Envelope("integration-test", kes, repository);

        await using var driver = GraphDatabase.Driver(
            ServerEndPoint,
            AuthToken,
            builder => builder.WithPropertyEncryptionProfiles([profile]));

        var propertyEncryption = driver.PropertyEncryption();
        await propertyEncryption.KeyManager().CreateAsync("main", token);

        var encrypted = await propertyEncryption.EncryptRequest()
            .FromValue("hello from a real database")
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        await using var session = driver.AsyncSession();

        await session.RunAsync(
            "CREATE (a:PropertyEncryptionIT {value: $value})",
            new Dictionary<string, object> { { "value", encrypted } });

        var cursor = await session.RunAsync("MATCH (a:PropertyEncryptionIT) RETURN a.value AS value");
        var storedBytes = await cursor.SingleAsync(r => r["value"].As<byte[]>());

        storedBytes.Should().Equal(encrypted);

        var decrypted = await propertyEncryption.DecryptRequest()
            .FromValue(storedBytes)
            .WithPersistedAad()
            .DecryptAsync(token);

        decrypted.Should().Be("hello from a real database");
    }
}

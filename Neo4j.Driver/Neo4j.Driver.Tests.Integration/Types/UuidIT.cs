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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Direct;
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;
using static Neo4j.Driver.IntegrationTests.Internals.VersionComparison;

namespace Neo4j.Driver.IntegrationTests.Types;

// Native UUID support requires Bolt 6.1, available from server 2026.5.0.
// [uuid-preview] search tag for removal of UUID preview workarounds
public sealed class UuidIT : DirectDriverTestBase
{
    private const string MinUuidServerVersion = "2026.5.0";

    public UuidIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
        : base(output, fixture)
    {
    }

    [RequireServerFact(MinUuidServerVersion, GreaterThanOrEqualTo)]
    public async Task ShouldSendAndReceive()
    {
        await TestSendAndReceive(Guid.Empty);
        await TestSendAndReceive(new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"));
        await TestSendAndReceive(new Guid("01020304-0506-0708-090a-0b0c0d0e0f12"));
        await TestSendAndReceive(Guid.NewGuid());
    }

    [RequireServerFact(MinUuidServerVersion, GreaterThanOrEqualTo)]
    public async Task ShouldSendAndReceiveInList()
    {
        var uuids = new List<Guid>
        {
            Guid.Empty,
            new("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            new("01020304-0506-0708-090a-0b0c0d0e0f12"),
            Guid.NewGuid()
        };

        var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
        try
        {
            var cursor = await session.RunAsync("RETURN $uuids", new { uuids });
            var record = await cursor.SingleAsync();

            record[0].Should().BeEquivalentTo(uuids);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireServerFact(MinUuidServerVersion, GreaterThanOrEqualTo)]
    public async Task ShouldSendAndReceiveInMap()
    {
        var uuids = new Dictionary<string, Guid>
        {
            ["a"] = Guid.NewGuid(),
            ["b"] = Guid.Empty
        };

        var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
        try
        {
            var cursor = await session.RunAsync("RETURN $uuids", new { uuids });
            var record = await cursor.SingleAsync();

            record[0].Should().BeEquivalentTo(uuids);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireEnterpriseEditionFact(MinUuidServerVersion, GreaterThanOrEqualTo)]
    public async Task ShouldStoreAndRetrieveUuidOnNode()
    {
        var uuid = Guid.NewGuid();

        var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));
        try
        {
            var cursor = await session.RunAsync(
                "CREATE (n:Node { id: $uuid }) RETURN n.id",
                new { uuid });

            var record = await cursor.SingleAsync();

            record[0].As<Guid>().Should().Be(uuid);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireServerFact(MinUuidServerVersion, GreaterThanOrEqualTo)]
    public async Task ShouldReceiveServerGeneratedUuid()
    {
        var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
        try
        {
            var cursor = await session.RunAsync("RETURN uuid()");
            var record = await cursor.SingleAsync();

            record[0].Should().BeOfType<Guid>();
            record[0].As<Guid>().Should().NotBe(Guid.Empty);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireServerFact(MinUuidServerVersion, GreaterThanOrEqualTo)]
    public async Task ShouldReceiveServerCreatedUuid()
    {
        // Construct UUIDs server-side from known strings so we verify the
        // driver decodes the wire format (16 big-endian bytes) to the exact
        // expected value, independent of the driver's own encoding.
        await TestServerCreatedUuid("00000000-0000-0000-0000-000000000000");
        await TestServerCreatedUuid("ffffffff-ffff-ffff-ffff-ffffffffffff");
        await TestServerCreatedUuid("01020304-0506-0708-090a-0b0c0d0e0f12");
    }

    private async Task TestServerCreatedUuid(string uuidString)
    {
        var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
        try
        {
            var cursor = await session.RunAsync("RETURN uuid($s)", new { s = uuidString });
            var record = await cursor.SingleAsync();

            record[0].As<Guid>().Should().Be(new Guid(uuidString));
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    private async Task TestSendAndReceive(Guid uuid)
    {
        var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
        try
        {
            var cursor = await session.RunAsync("RETURN $uuid", new { uuid });
            var record = await cursor.SingleAsync();

            record[0].As<Guid>().Should().Be(uuid);
        }
        finally
        {
            await session.CloseAsync();
        }
    }
}

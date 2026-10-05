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

using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Direct;
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;
using static Neo4j.Driver.IntegrationTests.Internals.VersionComparison;

namespace Neo4j.Driver.IntegrationTests.Types;

public sealed class VectorIT : DirectDriverTestBase
{
    private const string MinVectorServerVersion = "2025.11.0";

    public VectorIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
        : base(output, fixture)
    {
    }

    [RequireEnterpriseEditionFact(MinVectorServerVersion, GreaterThanOrEqualTo)]
    public async Task ShouldSendAndReceiveInt8()
    {
        await TestSendAndReceive(Vector.Create(new sbyte[] { 1, -2, 127 }));
    }

    [RequireEnterpriseEditionFact(MinVectorServerVersion, GreaterThanOrEqualTo)]
    public async Task ShouldSendAndReceiveInt16()
    {
        await TestSendAndReceive(Vector.Create(new short[] { 0, 100, -32768 }));
    }

    [RequireEnterpriseEditionFact(MinVectorServerVersion, GreaterThanOrEqualTo)]
    public async Task ShouldSendAndReceiveInt32()
    {
        await TestSendAndReceive(Vector.Create(new[] { 42, -1000 }));
    }

    [RequireEnterpriseEditionFact(MinVectorServerVersion, GreaterThanOrEqualTo)]
    public async Task ShouldSendAndReceiveInt64()
    {
        await TestSendAndReceive(Vector.Create(new long[] { long.MaxValue, long.MinValue, 0 }));
    }

    [RequireEnterpriseEditionFact(MinVectorServerVersion, GreaterThanOrEqualTo)]
    public async Task ShouldSendAndReceiveFloat32()
    {
        await TestSendAndReceive(Vector.Create(new[] { 1.5f, -2.25f, 0f }));
    }

    [RequireEnterpriseEditionFact(MinVectorServerVersion, GreaterThanOrEqualTo)]
    public async Task ShouldSendAndReceiveFloat64()
    {
        await TestSendAndReceive(Vector.Create(new[] { 1.5, -2.25, 0d }));
    }

    private async Task TestSendAndReceive(IVector vector)
    {
        var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
        try
        {
            var cursor = await session.RunAsync("RETURN $vector", new { vector });
            var record = await cursor.SingleAsync();

            record[0].As<IVector>().Should().Be(vector);
        }
        finally
        {
            await session.CloseAsync();
        }
    }
}

// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.TestUtil;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Routing;

[Collection(CcIntegrationCollection.CollectionName)]
public abstract class RoutingDriverTestBase : IDisposable
{
    protected RoutingDriverTestBase(ITestOutputHelper output, CausalClusterIntegrationTestFixture fixture)
    {
        Cluster = fixture.Cluster;
        AuthToken = Cluster.AuthToken;

        Driver = GraphDatabase.Driver(
            RoutingServer,
            AuthToken,
            builder =>
            {
                builder.WithLogger(TestLogger.Create(output));
                Cluster.Configure(builder);
            });
    }

    protected ICausalCluster Cluster { get; }
    protected IAuthToken AuthToken { get; }

    protected string RoutingServer => Cluster.BoltRoutingUri.ToString();
    protected static string WrongServer => "neo4j://localhost:1234";
    private IDriver Driver { get; }

    public void Dispose()
    {
        Driver.Dispose();
        GC.SuppressFinalize(this);
    }
}

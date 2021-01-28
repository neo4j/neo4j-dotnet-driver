﻿// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

namespace Neo4j.Driver.IntegrationTests.Routing
{
    [Collection(CCIntegrationCollection.CollectionName)]
    public abstract class RoutingDriverTestBase : IDisposable
    {
        protected ITestOutputHelper Output { get; }
        protected ICausalCluster Cluster { get; }
        protected IAuthToken AuthToken { get; }

        protected string RoutingServer => Cluster.BoltRoutingUri.ToString();
        protected string WrongServer => "neo4j://localhost:1234";
        protected IDriver Driver { get; }

        public RoutingDriverTestBase(ITestOutputHelper output, CausalClusterIntegrationTestFixture fixture)
        {
            Output = output;
            Cluster = fixture.Cluster;
            AuthToken = Cluster.AuthToken;

            Driver = GraphDatabase.Driver(RoutingServer, AuthToken,
                builder =>
                {
                    builder.WithLogger(TestLogger.Create(output));
                    Cluster.Configure(builder);
                });
        }

        public virtual void Dispose()
        {
            Driver.Dispose();
            // put some code that you want to run after each unit test
        }
    }
}
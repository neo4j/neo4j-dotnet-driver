﻿// Copyright (c) 2002-2023 "Neo4j,"
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

using System.Threading.Tasks;
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;

namespace Neo4j.Driver.IntegrationTests;

[CollectionDefinition(CollectionName)]
public sealed class CausalClusterCollection : ICollectionFixture<CausalClusterFixture>
{
    public const string CollectionName = "CausalClusterIntegration";
}

public sealed class CausalClusterFixture : IAsyncLifetime
{
    public ICausalCluster Cluster { get; }

    public CausalClusterFixture()
    {
        Cluster = ExistingCluster.IsClusterProvided()
            ? new ExistingCluster()
            : new TestContainerCausalCluster();
    }

    public Task InitializeAsync()
    {
        return Cluster.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        return Cluster.DisposeAsync();
    }
}
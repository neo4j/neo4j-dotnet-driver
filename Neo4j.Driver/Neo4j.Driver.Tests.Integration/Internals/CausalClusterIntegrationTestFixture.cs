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

namespace Neo4j.Driver.IntegrationTests.Internals;

public sealed class CausalClusterIntegrationTestFixture : IDisposable
{
    private bool _disposed;

    public CausalClusterIntegrationTestFixture()
    {
        if (ExistingCluster.IsClusterProvided())
        {
            Cluster = new ExistingCluster();
        }
        else
        {
            var isClusterSupported = BoltkitHelper.IsClusterSupported();
            if (!isClusterSupported.Item1)
            {
                return;
            }

            try
            {
                Cluster = new CausalCluster();
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }
    }

    public ICausalCluster Cluster { get; }

    public void Dispose()
    {
        switch (_disposed)
        {
            case true: return;
            case false:
                Cluster?.Dispose();
                break;
        }

        _disposed = true;
    }
}

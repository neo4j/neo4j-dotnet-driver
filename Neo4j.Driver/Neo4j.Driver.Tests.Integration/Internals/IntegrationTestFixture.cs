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
using Xunit;

namespace Neo4j.Driver.IntegrationTests;

public class StandAloneIntegrationTestFixture : IDisposable
{
    private bool _disposed;

    public StandAloneIntegrationTestFixture()
    {
        StandAloneSharedInstance = CreateInstance();
    }

    public IStandAlone StandAloneSharedInstance { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~StandAloneIntegrationTestFixture()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            StandAloneSharedInstance?.Dispose();
        }

        _disposed = true;
    }

    private IStandAlone CreateInstance()
    {
        if (LocalStandAloneInstance.IsServerProvided())
        {
            return new LocalStandAloneInstance();
        }

        if (!BoltkitHelper.ServerAvailable())
        {
            return null;
        }

        try
        {
            return new StandAlone();
        }
        catch (Exception)
        {
            Dispose();
            throw;
        }
    }
}

public class CausalClusterIntegrationTestFixture : IDisposable
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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~CausalClusterIntegrationTestFixture()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            //Dispose managed state (managed objects).                
            Cluster?.Dispose();
        }

        _disposed = true;
    }
}

[CollectionDefinition(CollectionName)]
public class SAIntegrationCollection : ICollectionFixture<StandAloneIntegrationTestFixture>
{
    public const string CollectionName = "StandAloneIntegration";
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

[CollectionDefinition(CollectionName)]
public class CCIntegrationCollection : ICollectionFixture<CausalClusterIntegrationTestFixture>
{
    public const string CollectionName = "CausalClusterIntegration";
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

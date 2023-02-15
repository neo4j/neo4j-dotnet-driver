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

public class LocalStandAloneInstance : SingleInstance, IStandAlone
{
    private const string UsingLocalServer = "DOTNET_DRIVER_USING_LOCAL_SERVER";
    private bool _disposed;

    public LocalStandAloneInstance() :
        base(
            DefaultInstallation.HttpUri,
            DefaultInstallation.BoltUri,
            null,
            DefaultInstallation.Password)
    {
        Driver = DefaultInstallation.NewBoltDriver(BoltUri, AuthToken);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public IDriver Driver { get; }

    ~LocalStandAloneInstance()
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
            Driver.Dispose();
        }

        _disposed = true;
    }

    public static bool IsServerProvided()
    {
        // If a system flag is set, then we use the local single server instead
        return bool.TryParse(Environment.GetEnvironmentVariable(UsingLocalServer), out var usingLocalServer) &&
            usingLocalServer;
    }
}

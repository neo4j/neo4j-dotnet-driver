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
using System.Linq;
using Org.BouncyCastle.Pkcs;

namespace Neo4j.Driver.IntegrationTests.Internals;

public interface IStandAlone : ISingleInstance, IDisposable
{
    IDriver Driver { get; }
}

public sealed class StandAlone : IStandAlone
{
    private readonly ExternalBoltkitInstaller _installer = new();

    private ISingleInstance _delegator;

    public StandAlone()
    {
        try
        {
            _installer.Install();
            RetryIfFailToStart();
        }
        catch
        {
            try
            {
                Dispose();
            }
            catch
            {
                /*Do nothing*/
            }

            throw;
        }

        NewBoltDriver();
    }

    public StandAlone(Pkcs12Store store)
    {
        try
        {
            _installer.Install();
            UpdateCertificate(store);
            RetryIfFailToStart();
        }
        catch
        {
            try
            {
                Dispose();
            }
            catch
            {
                /*Do nothing*/
            }

            throw;
        }

        NewBoltDriver();
    }

    public IDriver Driver { private set; get; }

    public Uri HttpUri => _delegator?.HttpUri;
    public Uri BoltUri => _delegator?.BoltUri;
    public Uri BoltRoutingUri => _delegator?.BoltRoutingUri;
    public string HomePath => _delegator?.HomePath;
    public IAuthToken AuthToken => _delegator?.AuthToken;

    public void Dispose()
    {
        DisposeBoltDriver();

        try
        {
            _installer.Stop();
        }
        catch
        {
            try
            {
                _installer.Kill();
            }
            catch
            {
                // ignored
            }
        }
    }

    private void RetryIfFailToStart()
    {
        try
        {
            _delegator = _installer.Start().Single();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            _delegator = _installer.Start().Single();
        }
    }

    private void NewBoltDriver()
    {
        Driver = DefaultInstallation.NewBoltDriver(BoltUri, AuthToken);
    }

    private void DisposeBoltDriver()
    {
        Driver?.Dispose();
    }

    public override string ToString()
    {
        return _delegator?.ToString() ?? "No server found";
    }

    public void RestartServerWithCertificate(Pkcs12Store store)
    {
        DisposeBoltDriver();
        try
        {
            _installer.EnsureCertificate(store);
        }
        catch
        {
            try
            {
                Dispose();
            }
            catch
            {
                /*Do nothing*/
            }

            throw;
        }

        NewBoltDriver();
    }

    public void UpdateCertificate(Pkcs12Store store)
    {
        _installer.UpdateCertificate(store);
    }
}

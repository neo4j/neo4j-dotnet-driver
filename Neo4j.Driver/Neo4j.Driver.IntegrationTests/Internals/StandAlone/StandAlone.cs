// Copyright (c) 2002-2018 "Neo4j,"
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
using System.Collections.Generic;
using Neo4j.Driver.Internal;
using System.Linq;
using Neo4j.Driver.IntegrationTests.Shared;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.V1;
using Org.BouncyCastle.Pkcs;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public class StandAlone : ISingleInstance, IDisposable
    {
        private readonly ExternalBoltkitInstaller _installer = new ExternalBoltkitInstaller();
        public IDriver Driver { private set; get; }

        private readonly ISingleInstance _delegator;

        public Uri HttpUri => _delegator?.HttpUri;
        public Uri BoltUri => _delegator?.BoltUri;
        public Uri BoltRoutingUri => _delegator?.BoltRoutingUri;
        public string HomePath => _delegator?.HomePath;
        public IAuthToken AuthToken => _delegator?.AuthToken;

        public StandAlone()
        {
            try
            {
                _installer.Install();
                _delegator = _installer.Start().Single();
            }
            catch
            {
                try { Dispose(); } catch { /*Do nothing*/ }
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
                _delegator = _installer.Start().Single();
            }
            catch
            {
                try { Dispose(); } catch { /*Do nothing*/ }
                throw;
            }
            NewBoltDriver();
        }

        private void NewBoltDriver()
        {
            var logger = new TestDriverLogger(s => System.Diagnostics.Debug.WriteLine(s));
#if DEBUG
            logger = new TestDriverLogger(s => System.Diagnostics.Debug.WriteLine(s), ExtendedLogLevel.Debug);
#endif
            var config = Config.Builder.WithDriverLogger(logger).ToConfig();
            Driver = GraphDatabase.Driver(BoltUri, AuthToken, config);
        }

        private void DisposeBoltDriver()
        {
            Driver?.Dispose();
        }

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

        public override string ToString()
        {
            return _delegator?.ToString() ?? "No server found";
        }
        
        /// This method will always restart the server with the updated certificates
        /// </summary>
        /// <param name="sourceProcedureJarPath"></param>
        public void RestartServerWithCertificate(Pkcs12Store store)
        {
            DisposeBoltDriver();
            try
            {
                _installer.EnsureCertificate(store);
            }
            catch
            {
                try { Dispose(); } catch { /*Do nothing*/ }
                throw;
            }
            NewBoltDriver();
        }

        public void UpdateCertificate(Pkcs12Store store)
        {
            _installer.UpdateCertificate(store);
        }
    }
}

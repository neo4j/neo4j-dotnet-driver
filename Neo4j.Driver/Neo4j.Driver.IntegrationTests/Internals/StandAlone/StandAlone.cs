// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using Neo4j.Driver.V1;

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

        private void NewBoltDriver()
        {
            var config = Config.DefaultConfig;
#if DEBUG
            config = Config.Builder.WithLogger(new DebugLogger {Level = LogLevel.Debug}).ToConfig();
#endif
            Driver = GraphDatabase.Driver(BoltUri, AuthToken, config);
        }

        private void DisposeBoltDriver()
        {
            Driver?.Dispose();
        }

        /// <summary>
        /// This method will always restart the server no matter if the setting is the same or not,
        /// so do not call this method unless really necessary
        /// </summary>
        /// <param name="keyValuePair"></param>
        public void RestartServerWithUpdatedSettings(IDictionary<string, string> keyValuePair)
        {
            DisposeBoltDriver();
            try
            {
                _installer.EnsureRunningWithSettings(keyValuePair);
            }
            catch
            {
                try { Dispose(); } catch { /*Do nothing*/ }
                throw;
            }
            NewBoltDriver();
        }

        /// <summary>
        /// This method will not restart the server if the file already exist in path
        /// </summary>
        /// <param name="sourceProcedureJarPath"></param>
        public void RestartServerWithProcedures(string sourceProcedureJarPath)
        {
            DisposeBoltDriver();
            try
            {
                _installer.EnsureProcedures(sourceProcedureJarPath);
            }
            catch
            {
                try { Dispose(); } catch { /*Do nothing*/ }
                throw;
            }
            NewBoltDriver();
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
    }
}
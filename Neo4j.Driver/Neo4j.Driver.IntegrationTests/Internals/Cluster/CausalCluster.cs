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
using System.Linq;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public class CausalCluster : IDisposable
    {
        private readonly ExternalBoltkitClusterInstaller _installer = new ExternalBoltkitClusterInstaller();
        public ISet<ISingleInstance> ClusterMembers { get; }

        // Assume the whole cluster use exact the same authToken
        public IAuthToken AuthToken => ClusterMembers?.First().AuthToken;

        public CausalCluster()
        {
            // start a cluster
            try
            {
                _installer.Install();
                ClusterMembers = _installer.Start();
                foreach (var singleInstance in ClusterMembers)
                {
                    Console.WriteLine(singleInstance);
                }
            }
            catch
            {
                try
                {
                    Kill();
                }
                catch
                {
                    // do nothing
                }
                throw;
            }
        }

        public ISingleInstance AnyCore()
        {
            return ClusterMembers.First();
        }

        public bool IsRunning()
        {
            return ClusterMembers != null;
        }

        private void Kill()
        {
            // Unlike Dispose, this method will always try to execute
            try
            {
                _installer.Kill();
            }
            catch
            {
                // ignored
            }
        }

        public void Dispose()
        {
            // shut down the whole cluster
            try
            {
                _installer.Stop();
            }
            catch
            {
                // if failed to stop properly, then we kill
                try
                {
                    Kill();
                }
                catch
                {
                    // ignored
                }
                // ignored
            }
        }
    }
}

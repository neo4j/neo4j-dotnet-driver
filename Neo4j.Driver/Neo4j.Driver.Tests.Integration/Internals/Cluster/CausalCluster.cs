// Copyright (c) "Neo4j"
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Neo4j.Driver;
using Neo4j.Driver.TestUtil;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public class CausalCluster : ICausalCluster
    {
        private static readonly TimeSpan ClusterOnlineTimeout = TimeSpan.FromMinutes(2);

        private readonly ExternalBoltkitClusterInstaller _installer = new ExternalBoltkitClusterInstaller();
        private ISet<ISingleInstance> Members { get; }

        public Uri BoltRoutingUri => AnyCore()?.BoltRoutingUri;

        // Assume the whole cluster use exact the same authToken
        public IAuthToken AuthToken => AnyCore()?.AuthToken;
        public void Configure(ConfigBuilder builder)
        {
            // no special modification to driver config
        }

        public CausalCluster()
        {
            // start a cluster
            try
            {
                _installer.Install();
                Members = _installer.Start();
                WaitForMembersOnline();
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

        private ISingleInstance AnyCore()
        {
            return Members?.First();
        }

        public bool IsRunning()
        {
            return Members != null;
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

        private void WaitForMembersOnline()
        {
            void VerifyCanExecute(AccessMode mode, List<Exception> exceptions)
            {
                using (var driver = GraphDatabase.Driver(AnyCore().BoltRoutingUri, AuthToken))
                using (var session = driver.Session(o => o.WithDefaultAccessMode(mode)))
                {
                    session.Run("RETURN 1").Consume();
                }
            }

            var expectedOnlineMembers = Members.Select(x => x.BoltUri.Authority).ToHashSet();
            var onlineMembers = Enumerable.Empty<string>();

            var errors = new List<Exception>();
            var timer = Stopwatch.StartNew();
            while (timer.Elapsed < ClusterOnlineTimeout)
            {
                Thread.Sleep(1000);

                if (!expectedOnlineMembers.SetEquals(onlineMembers))
                {
                    try
                    {
                        using (var driver = GraphDatabase.Driver(AnyCore().BoltRoutingUri, AuthToken))
                        using (var session = driver.Session(o => o.WithDefaultAccessMode(AccessMode.Read)))
                        {
                            var addresses = new List<string>();
                            var records = session.Run("CALL dbms.cluster.overview()").ToList();
                            foreach (var record in records)
                            {
                                addresses.Add(
                                    record["addresses"].As<List<object>>().First().As<string>().Replace("bolt://", ""));
                            }

                            onlineMembers = addresses;
                        }
                    }
                    catch (Exception exc)
                    {
                        errors.Add(exc);
                    }
                }

                if (!expectedOnlineMembers.SetEquals(onlineMembers))
                {
                    continue;
                }

                errors.Clear();
                try
                {
                    // Verify that we can connect to a FOLLOWER
                    VerifyCanExecute(AccessMode.Read, errors);

                    // Verify that we can connect to a LEADER
                    VerifyCanExecute(AccessMode.Write, errors);

                    return;
                }
                catch (Exception exc)
                {
                    errors.Add(exc);
                }
            }

            throw new TimeoutException(
                $"Timed out waiting for the cluster to become available. Seen errors: {errors}");
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
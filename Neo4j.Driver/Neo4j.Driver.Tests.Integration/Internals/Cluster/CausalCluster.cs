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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Neo4j.Driver.IntegrationTests.Internals;

public sealed class CausalCluster : ICausalCluster
{
    private static readonly TimeSpan ClusterOnlineTimeout = TimeSpan.FromMinutes(2);

    private readonly ExternalBoltkitClusterInstaller _installer = new();
    private bool _disposed;

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

    private ISet<ISingleInstance> Members { get; }

    public Uri BoltRoutingUri => AnyCore()?.BoltRoutingUri;

    // Assume the whole cluster use exact the same authToken
    public IAuthToken AuthToken => AnyCore()?.AuthToken;

    public void Configure(ConfigBuilder builder)
    {
        // no special modification to driver config
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~CausalCluster()
    {
        Dispose(false);
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
        void VerifyCanExecute(AccessMode mode)
        {
            using var driver = GraphDatabase.Driver(AnyCore().BoltRoutingUri, AuthToken);
            using var session = driver.Session(o => o.WithDefaultAccessMode(mode));
            session.Run("RETURN 1").Consume();
        }

        var expectedOnlineMembers = Members.Select(x => x.BoltUri.Authority).ToHashSet();
        var onlineMembers = Array.Empty<string>();

        var errors = new List<Exception>();
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < ClusterOnlineTimeout)
        {
            Thread.Sleep(1000);

            if (!expectedOnlineMembers.SetEquals(onlineMembers))
            {
                try
                {
                    using var driver = GraphDatabase.Driver(AnyCore().BoltRoutingUri, AuthToken);
                    using var session = driver.Session(o => o.WithDefaultAccessMode(AccessMode.Read));

                    var records = session.Run("CALL dbms.cluster.overview()").ToList();

                    var cluster = session.Run("CALL dbms.cluster.overview()").ToList();
                    if (!DbAvailable(cluster))
                    {
                        continue;
                    }

                    onlineMembers = records.Select(
                            record => record["addresses"]
                                .As<List<object>>()
                                .First()
                                .As<string>()
                                .Replace("bolt://", ""))
                        .ToArray();
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
                VerifyCanExecute(AccessMode.Read);

                // Verify that we can connect to a LEADER
                VerifyCanExecute(AccessMode.Write);

                return;
            }
            catch (Exception exc)
            {
                errors.Add(exc);
            }
        }

        throw new TimeoutException($"Timed out waiting for the cluster to become available. Seen errors: {errors}");
    }

    private static bool DbAvailable(List<IRecord> cluster)
    {
        return cluster.Any(
            x => x.Values.TryGetValue("databases", out var y) &&
                y.As<IDictionary<string, object>>()
                    .TryGetValue("neo4j", out _));
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
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

        _disposed = true;
    }
}

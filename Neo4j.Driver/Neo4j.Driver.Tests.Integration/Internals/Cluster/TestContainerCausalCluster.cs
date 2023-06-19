// Copyright (c) 2002-2023 "Neo4j,"
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Neo4j.Driver.IntegrationTests.Internals;

namespace Neo4j.Driver.IntegrationTests
{
    public class TestContainerCausalCluster : ICausalCluster
    {
        public Uri BoltRoutingUri { get; }
        public IAuthToken AuthToken { get; }

        private readonly IContainer[] _containers;
        private static readonly int[] Ports = {7689, 7690, 7691};
        private readonly INetwork _network;

        public TestContainerCausalCluster()
        {
            BoltRoutingUri = new UriBuilder("neo4j", "localhost", Ports[0]).Uri;
            AuthToken = AuthTokens.Basic(Neo4jDefaultInstallation.User, Neo4jDefaultInstallation.Password);
            _network = new NetworkBuilder()
                .WithName("tc-cc")
                .WithDriver(NetworkDriver.Bridge)
                .Build();
            _containers = Ports.Select(BuildContainer).ToArray();
        }

        private IContainer BuildContainer(int port)
        {
            var member = Array.IndexOf(Ports, port) + 1;
            var name = $"tc-cc-node-{member}";
            var eps = String.Join(",", Ports.Select((_, y) => $"tc-cc-node-{y+1}:5000"));

            // 4.4 cluster config.
            var environment = new Dictionary<string, string>
            {
                ["NEO4J_ACCEPT_LICENSE_AGREEMENT"] = "yes",
                ["NEO4J_dbms_routing_enabled"] = "true",
                ["NEO4J_dbms_backup_enabled"] = "false",
                ["NEO4J_dbms_mode"] = "CORE",
                ["NEO4J_dbms_memory_pagecache_size"] = "100M",
                ["NEO4J_dbms_memory_heap_initial__size"] = "100M",
                ["NEO4J_causal__clustering_initial__discovery__members"] = eps,
                ["NEO4J_causal__clustering_minimum__core__cluster__size__at__formation"] = "3",
                ["NEO4J_causal__clustering_discovery__advertised__address"] = $"{name}:5000",
                ["NEO4J_causal__clustering_transaction__advertised__address"] = $"{name}:6000",
                ["NEO4J_causal__clustering_raft__advertised__address"] = $"{name}:7000",
                ["NEO4J_dbms_connector_bolt_listen__address"] = $":{port}"
            };

            if (Neo4jDefaultInstallation.Password != "neo4j")
            {
                var auth = $"{Neo4jDefaultInstallation.User}/{Neo4jDefaultInstallation.Password}";
                environment.Add("NEO4J_AUTH", auth);
            }
        
            return TestContainerBuilder
                .ImageBase(4, 4, true)
                .WithPortBinding(port, port)
                .WithName(name)
                .WithNetwork(_network)
                .WithEnvironment(environment)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(port))
                .Build();
        }

        public void Configure(ConfigBuilder builder)
        {
        }

        public async Task InitializeAsync()
        {
            await _network.CreateAsync();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            await Task.WhenAll(_containers.Select(x => x.StartAsync(cts.Token)));
        }

        public async Task DisposeAsync()
        {
            try
            {
                await Task.WhenAll(_containers.Select(x => x.StopAsync()));
            }
            catch (Exception)
            {
                // Ignore.
            }
        
            await _network.DeleteAsync();
        }
    }
}
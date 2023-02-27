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
using System.Linq;
using System.Net.NetworkInformation;
using static System.Environment;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public static class TestConfiguration
    {
        public const string TestRequireBoltkit = "Test is skipped due to Boltkit not accessible";
        private const string TestRequireEnterprise = "Test is skipped due to enterprise server is not accessible";
        private static readonly Lazy<string[]> NeoctrlArgs = new(() => GetEnvironmentVariable("NEOCTRL_ARGS")?.Split());

        public static bool ExisingServer => ExistingCluster.IsClusterProvided() || ExistingSingleServer.IsServerProvided();

        public static bool UsingOwnedDatabase => !ExisingServer;

        public static bool IsClusterAvailable()
        {
            return !ExistingSingleServer.IsServerProvided();
        }

        public static bool SingleServerAvailable()
        {
            return !ExistingCluster.IsClusterProvided();
        }

        public static bool IpV6Available()
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Any(nic => nic.Supports(NetworkInterfaceComponent.IPv6));
        }

        public static bool IpV6Enabled()
        {
            var envVar = GetEnvironmentVariable("NEOCTRL_DISABLE_IPV6");
            return !bool.TryParse(envVar, out var disableIpV6) || !disableIpV6;
        }


        public static string ServerVersion()
        {
            return NeoctrlArgs.Value == null ? "4.4.0" : NeoctrlArgs.Value.Last();
        }

        public static bool IsEnterprise()
        {
            return NeoctrlArgs.Value == null || NeoctrlArgs.Value.Contains("-e");
        }
    }
}
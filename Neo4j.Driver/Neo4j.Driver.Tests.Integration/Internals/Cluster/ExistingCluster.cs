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
using Castle.Core.Internal;
using FluentAssertions;
using Neo4j.Driver;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public class ExistingCluster : ICausalCluster
    {
        private const string ClusterUri = "NEO4J_URI";
        private const string ClusterPassword = "NEO4J_PASSWORD";
        private const string ClusterUser = "NEO4J_USER";

        public void Dispose()
        {
        }

        public static bool IsClusterProvided()
        {
            var uri = Environment.GetEnvironmentVariable(ClusterUri);
            var password = Environment.GetEnvironmentVariable(ClusterPassword);
            // both of the two above env var should be provided.
            return !uri.IsNullOrEmpty() && !password.IsNullOrEmpty();
        }

        private static string GetEnvOrThrow(string env)
        {
            var value = Environment.GetEnvironmentVariable(env);
            if (value.IsNullOrEmpty())
                throw new ArgumentException($"Missing env variable {env}");
            return value;
        }

        private static string GetEnvOrDefault(string env, string defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(env);
            return value.IsNullOrEmpty() ? defaultValue : value;
        }

        public Uri BoltRoutingUri => new Uri(GetEnvOrThrow(ClusterUri));

        public IAuthToken AuthToken => AuthTokens.Basic(
            GetEnvOrDefault(ClusterUser, Neo4jDefaultInstallation.User),
            GetEnvOrThrow(ClusterPassword));
        public void Configure(ConfigBuilder builder)
        {
        }
    }
}

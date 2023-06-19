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
using System.Threading.Tasks;
using Castle.Core.Internal;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public sealed class ExistingCluster : ICausalCluster
    {
        private const string ClusterUri = "NEO4J_URI";
        private const string ClusterPassword = "NEO4J_PASSWORD";
        private const string ClusterUser = "NEO4J_USER";

        public Uri BoltRoutingUri { get; }

        public IAuthToken AuthToken { get; }

        public ExistingCluster()
        {
            AuthToken = AuthTokens.Basic(
                GetEnvOrDefault(ClusterUser, Neo4jDefaultInstallation.User),
                GetEnvOrThrow(ClusterPassword));
            
            BoltRoutingUri = new Uri(GetEnvOrThrow(ClusterUri));
        }
        
        public static bool IsClusterProvided()
        {
            var uri = Environment.GetEnvironmentVariable(ClusterUri);

            if (string.IsNullOrWhiteSpace(uri))
                return false;
        
            try
            {
                var parsed = new Uri(uri);
                if (!parsed.Scheme.StartsWith("neo4j"))
                    return false;
            }
            catch (UriFormatException)
            {
                return false;
            }

            var password = Environment.GetEnvironmentVariable(ClusterPassword);
 
            return !password.IsNullOrEmpty();
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

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
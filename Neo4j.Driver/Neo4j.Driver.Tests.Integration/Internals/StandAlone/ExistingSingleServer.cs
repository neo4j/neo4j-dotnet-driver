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

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public sealed class ExistingSingleServer : ISingleServer
    {
     
        public Uri HttpUri { get; }
        public Uri BoltUri { get; }
        public Uri BoltRoutingUri { get; }
        public IAuthToken AuthToken { get; }

        private const string BoltRoutingScheme = "neo4j://";
        private const string Username = "neo4j";

        public ExistingSingleServer()
        {
            HttpUri = new Uri(Neo4jDefaultInstallation.HttpUri);
            BoltUri = new Uri(Neo4jDefaultInstallation.BoltUri);
            BoltRoutingUri = new UriBuilder(BoltRoutingScheme, BoltUri.Host, BoltUri.Port).Uri;
            AuthToken = AuthTokens.Basic(Username, Neo4jDefaultInstallation.Password);
            Driver = Neo4jDefaultInstallation.NewBoltDriver(BoltUri, AuthToken);
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
        
        public Task DisposeAsync()
        {
            Driver?.Dispose();
            return Task.CompletedTask;
        }

        public IDriver Driver { get; }

        public static bool IsServerProvided()
        {
            // TODO: Consider renaming this env variable.
            // If a system flag is set, then we use the local single server instead
            var env = Environment.GetEnvironmentVariable("DOTNET_DRIVER_USING_LOCAL_SERVER");
            return bool.TryParse(env, out var existing) && existing;
        }
   
        public override string ToString()
        {
            return $"Server at endpoint '{HttpUri}', with bolt enabled at endpoint '{BoltUri}'.";
        }
    }
}
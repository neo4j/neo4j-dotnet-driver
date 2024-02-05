// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

using Neo4j.Driver.Tests.BenchkitBackend.Configuration;

namespace Neo4j.Driver.Tests.BenchkitBackend.InfrastructureExtensions;

internal static class Neo4jDriverServiceExtension
{
    public static IServiceCollection AddNeo4jDriver(
        this IServiceCollection services,
        BenchkitBackendConfiguration configuration)
    {
        services.AddSingleton<IDriver>(
            _ =>
            {
                var driverUri = $"{configuration.Neo4jScheme}://{configuration.Neo4jHost}:{configuration.Neo4jPort}";
                var driver = GraphDatabase.Driver(
                    driverUri,
                    AuthTokens.Basic(configuration.Neo4jUser, configuration.Neo4jPassword));

                return driver;
            });

        return services;
    }
}

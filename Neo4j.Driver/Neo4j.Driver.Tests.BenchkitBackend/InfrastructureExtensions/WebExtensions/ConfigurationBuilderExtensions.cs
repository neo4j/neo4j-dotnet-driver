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

namespace Neo4j.Driver.Tests.BenchkitBackend.InfrastructureExtensions;

internal static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Override the configuration with values from environment variables.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <returns>The configuration builder for method chaining.</returns>
    public static IConfigurationBuilder OverrideFromBenchkitEnvironmentVars(this IConfigurationBuilder builder)
    {
        builder
            .OverrideSettingFromEnvironmentVariable("BenchkitBackend:BackendPort", "TEST_BACKEND_PORT")
            .OverrideSettingFromEnvironmentVariable("BenchkitBackend:Neo4jScheme", "TEST_NEO4J_SCHEME")
            .OverrideSettingFromEnvironmentVariable("BenchkitBackend:Neo4jHost", "TEST_NEO4J_HOST")
            .OverrideSettingFromEnvironmentVariable("BenchkitBackend:Neo4jPort", "TEST_NEO4J_PORT")
            .OverrideSettingFromEnvironmentVariable("BenchkitBackend:Neo4jUser", "TEST_NEO4J_USER")
            .OverrideSettingFromEnvironmentVariable("BenchkitBackend:Neo4jPassword", "TEST_NEO4J_PASSWORD");

        return builder;
    }

    private static IConfigurationBuilder OverrideSettingFromEnvironmentVariable(
        this IConfigurationBuilder builder,
        string configurationKey,
        string environmentVariableName)
    {
        var env = Environment.GetEnvironmentVariables();
        var value = env[environmentVariableName]?.ToString();
        if (value != null)
        {
            builder.AddInMemoryCollection(new Dictionary<string, string?> { { configurationKey, value } });
        }

        return builder;
    }
}

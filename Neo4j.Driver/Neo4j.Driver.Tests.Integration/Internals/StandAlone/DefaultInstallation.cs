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
using System.Diagnostics;
using Neo4j.Driver.TestUtil;

namespace Neo4j.Driver.IntegrationTests.Internals;

public static class DefaultInstallation
{
    public static readonly string User = "neo4j";
    public static readonly string Password = "neo4j";
    public static readonly string HttpUri = "http://127.0.0.1:7474";

    public static readonly string BoltHeader = "bolt://";
    private static readonly string BoltHost = "127.0.0.1";
    public static readonly string BoltPort = "7687";

    static DefaultInstallation()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEST_NEO4J_USER")))
        {
            User = Environment.GetEnvironmentVariable("TEST_NEO4J_USER");
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEST_NEO4J_PASS")))
        {
            Password = Environment.GetEnvironmentVariable("TEST_NEO4J_PASS");
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEST_NEO4J_HOST")))
        {
            BoltHost = Environment.GetEnvironmentVariable("TEST_NEO4J_HOST");
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEST_NEO4J_PORT")))
        {
            BoltPort = Environment.GetEnvironmentVariable("TEST_NEO4J_PORT");
        }
    }

    public static string BoltUri => BoltHeader + BoltHost + ":" + BoltPort;

    public static IDriver NewBoltDriver(Uri boltUri, IAuthToken authToken)
    {
        var configuredLevelStr = Environment.GetEnvironmentVariable("NEOLOGLEVEL");
        var logger = Enum.TryParse<ExtendedLogLevel>(configuredLevelStr ?? "", true, out var configuredLevel)
            ? new TestLogger(Console.WriteLine, configuredLevel)
            : new TestLogger(s => Debug.WriteLine(s), ExtendedLogLevel.Debug);

        return GraphDatabase.Driver(boltUri, authToken, o => { o.WithLogger(logger); });
    }
}

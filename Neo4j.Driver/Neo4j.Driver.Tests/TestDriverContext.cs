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

using System;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Tests;

internal static class TestDriverContext
{
    static TestDriverContext()
    {
        MockContext = new DriverContext(
            new Uri("bolt://localhost:7687"),
            AuthTokenManagers.None,
            new ConfigBuilder(new Config()).Build());
    }

    public static DriverContext MockContext { get; }

    public static DriverContext With(
        Uri uri = null,
        IAuthTokenManager authTokenManagers = null,
        Action<ConfigBuilder> config = null)
    {
        var cb = new ConfigBuilder(new Config());
        config?.Invoke(cb);
        return new DriverContext(
            uri ?? MockContext.InitialUri,
            authTokenManagers ?? MockContext.AuthTokenManager,
            cb.Build());
    }
}

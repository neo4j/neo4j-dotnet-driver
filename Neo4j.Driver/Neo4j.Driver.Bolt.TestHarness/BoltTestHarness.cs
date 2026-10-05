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

using Neo4j.Driver.Bolt.TestHarness.DependencyInjection;
using Neo4j.Driver.Bolt.TestHarness.Services;

namespace Neo4j.Driver.Bolt.TestHarness;

/// <summary>
/// Entry point for a bare-bones Bolt harness (placeholders + minimal resolver wiring).
/// </summary>
public static class BoltTestHarness
{
    /// <summary>
    /// Builds a <see cref="SimpleServiceResolver"/> with <see cref="ISomething"/> registered.
    /// </summary>
    public static SimpleServiceResolver CreateResolver()
    {
        var resolver = new SimpleServiceResolver();
        resolver.Register<IConnection, ConnectionStub>();
        resolver.Register<ISession, SessionStub>();
        resolver.Register<ITransaction, TransactionStub>();
        resolver.Register<ISomething, Something>();
        return resolver;
    }
}

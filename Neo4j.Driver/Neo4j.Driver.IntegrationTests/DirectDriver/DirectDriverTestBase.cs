// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    [Collection(SAIntegrationCollection.CollectionName)]
    public abstract class DirectDriverTestBase : IDisposable
    {
        protected ITestOutputHelper Output { get; }
        protected StandAlone Server { get; }
        protected Uri ServerEndPoint { get; }
        protected IAuthToken AuthToken { get; }

        protected DirectDriverTestBase(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
        {
            Output = output;
            Server = fixture.StandAlone;
            ServerEndPoint = Server.BoltUri;
            AuthToken = Server.AuthToken;
        }

        public void Dispose()
        {
            // clean database after each test run
            using (var session = Server.Driver.Session())
            {
                session.Run("MATCH (n) DETACH DELETE n").Consume();
            }
        }
    }
}

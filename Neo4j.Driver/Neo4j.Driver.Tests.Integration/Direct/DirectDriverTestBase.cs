﻿// Copyright (c) "Neo4j"
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
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct;

[Collection(SaIntegrationCollection.CollectionName)]
public abstract class DirectDriverTestBase : IDisposable
{
    private bool _disposed;

    protected DirectDriverTestBase(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
    {
        Output = output;
        Server = fixture.StandAloneSharedInstance;
        ServerEndPoint = Server.BoltUri;
        AuthToken = Server.AuthToken;
    }

    protected ITestOutputHelper Output { get; }
    protected IStandAlone Server { get; }
    protected Uri ServerEndPoint { get; }
    protected IAuthToken AuthToken { get; }

    public void Dispose()
    {
        switch (_disposed)
        {
            case true: return;
            case false:
            {
                using var session = Server.Driver.Session();
                session.Run("MATCH (n) DETACH DELETE n").Consume();
                break;
            }
        }

        _disposed = true;
    }
}

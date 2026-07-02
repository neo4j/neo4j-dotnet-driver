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
using Xunit;

namespace Neo4j.Driver.IntegrationTests.Internals;

[Collection(SaIntegrationCollection.CollectionName)]
public abstract class TransportTestBase<TTransport> : IDisposable
    where TTransport : ITransport, new()
{
    private bool _disposed;

    protected TransportTestBase(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
    {
        Output = output;
        Transport = new TTransport();
        Server = fixture.StandAloneSharedInstance;
        AuthToken = Server.AuthToken;
        ServerEndPoint = Transport.UriFor(Server);
        Driver = Transport.ResolveDriver(Server);
    }

    protected ITestOutputHelper Output { get; }
    protected ITransport Transport { get; }
    protected IStandAlone Server { get; }
    protected Uri ServerEndPoint { get; }
    protected IAuthToken AuthToken { get; }
    protected IDriver Driver { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        using (var session = Server.Driver.Session())
        {
            session.Run("MATCH (n) DETACH DELETE n").Consume();
        }

        if (Transport.OwnsDriver)
        {
            Driver.Dispose();
        }

        _disposed = true;
    }
}

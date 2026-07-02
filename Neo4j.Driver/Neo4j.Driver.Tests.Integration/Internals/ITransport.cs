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

namespace Neo4j.Driver.IntegrationTests.Internals;

/// <summary>
/// Selects which transport a suite runs against by choosing the URI handed to
/// <see cref="GraphDatabase"/>. There is a single <see cref="IDriver"/>; the transport only
/// decides the scheme (and whether the driver is shared or owned by the test).
/// </summary>
public interface ITransport
{
    string Name { get; }
    bool IsQueryApi { get; }
    bool OwnsDriver { get; }
    Uri UriFor(IStandAlone server);
    IDriver ResolveDriver(IStandAlone server);
}

public sealed class BoltTransport : ITransport
{
    public string Name { get; } = "bolt";
    public bool IsQueryApi { get; }
    public bool OwnsDriver { get; }

    public Uri UriFor(IStandAlone server)
    {
        return server.BoltUri;
    }

    public IDriver ResolveDriver(IStandAlone server)
    {
        return server.Driver;
    }
}

public sealed class QueryApiTransport : ITransport
{
    public string Name { get; } = "queryapi";
    public bool IsQueryApi { get; } = true;
    public bool OwnsDriver { get; } = true;

    public Uri UriFor(IStandAlone server)
    {
        return server.HttpUri;
    }

    public IDriver ResolveDriver(IStandAlone server)
    {
        return DefaultInstallation.NewQueryApiDriver(server.HttpUri, server.AuthToken);
    }
}

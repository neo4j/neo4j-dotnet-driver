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
using System.IO;

namespace Neo4j.Driver.IntegrationTests.Internals;

public class SingleInstance : ISingleInstance
{
    private const string BoltRoutingScheme = "neo4j://";
    private const string Username = "neo4j";

    public SingleInstance(string httpUri, string boltUri, string homePath, string password)
    {
        HttpUri = new Uri(httpUri);
        BoltUri = new Uri(boltUri);
        BoltRoutingUri = new Uri($"{BoltRoutingScheme}{BoltUri.Host}:{BoltUri.Port}");
        HomePath = homePath == null 
            ? "UNKNOWN"
            : new DirectoryInfo(homePath).FullName;

        AuthToken = AuthTokens.Basic(Username, password);
    }

    public Uri HttpUri { get; }
    public Uri BoltUri { get; }
    public Uri BoltRoutingUri { get; }
    public string HomePath { get; }
    public IAuthToken AuthToken { get; }

    public override string ToString()
    {
        return
            $"Server at endpoint '{HttpUri}', with bolt enabled at endpoint '{BoltUri}', and home path '{HomePath}'.";
    }
}

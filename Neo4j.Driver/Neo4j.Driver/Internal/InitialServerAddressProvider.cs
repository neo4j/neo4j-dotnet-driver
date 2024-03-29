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
using System.Collections.Generic;

namespace Neo4j.Driver.Internal;

internal interface IInitialServerAddressProvider
{
    ISet<Uri> Get();
}

internal sealed class InitialServerAddressProvider : IInitialServerAddressProvider
{
    private readonly Uri _initAddress;
    private readonly IServerAddressResolver _resolver;

    public InitialServerAddressProvider(Uri initialServerAddress, IServerAddressResolver resolver)
    {
        _initAddress = initialServerAddress;
        _resolver = resolver;
    }

    public ISet<Uri> Get()
    {
        var set = new HashSet<Uri>();
        var addresses = _resolver.Resolve(ServerAddress.From(_initAddress));
        foreach (var address in addresses)
        {
            // for now we convert this ServerAddress back to Uri
            set.Add(new UriBuilder("neo4j://", address.Host, address.Port).Uri);
        }

        return set;
    }
}

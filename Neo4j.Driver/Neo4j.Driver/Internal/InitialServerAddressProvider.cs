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
using System.Collections.Generic;
using Neo4j.Driver.Preview;

namespace Neo4j.Driver.Internal;

internal interface IInitialServerAddressProvider
{
    ISet<Uri> Get();
}

/// <summary>
/// Resolves the initial set of candidate URIs from a single URI + optional custom resolver.
/// This is the legacy path, used when the driver is created with a string/Uri.
/// </summary>
internal sealed class UriAddressProvider : IInitialServerAddressProvider
{
    private readonly Uri _initAddress;
    private readonly IServerAddressResolver _resolver;

    public UriAddressProvider(Uri initialServerAddress, IServerAddressResolver resolver)
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
            // preserve scheme from the original URI when converting back
            set.Add(new UriBuilder(_initAddress.Scheme, address.Host, address.Port).Uri);
        }

        return set;
    }
}

/// <summary>
/// Resolves the initial set of candidate URIs directly from a <see cref="MultiAddress"/>.
/// Each entry in <see cref="MultiAddress.Addresses"/> becomes a candidate URI, all sharing
/// the same scheme and (if present) routing context query.
/// </summary>
internal sealed class MultiAddressProvider : IInitialServerAddressProvider
{
    private readonly MultiAddress _multiAddress;

    public MultiAddressProvider(MultiAddress multiAddress)
    {
        _multiAddress = multiAddress;
    }

    public ISet<Uri> Get()
    {
        var set = new HashSet<Uri>();
        foreach (var address in _multiAddress.Addresses)
        {
            var builder = new UriBuilder(_multiAddress.Scheme, address.Host, address.Port);
            if (!string.IsNullOrEmpty(_multiAddress.Query))
            {
                builder.Query = _multiAddress.Query;
            }

            set.Add(builder.Uri);
        }

        return set;
    }
}

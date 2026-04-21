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
using System.Collections;
using System.Collections.Generic;

namespace Neo4j.Driver;

/// <summary>
/// An <see cref="IServerAddressResolver"/> that always returns a fixed set of addresses, regardless of the address
/// passed to <see cref="Resolve"/>. Use this when you want to provide a static list of cluster members at driver
/// creation time.
/// </summary>
/// <example>
/// Using collection initializer syntax:
/// <code>
/// var resolver = new ListAddressResolver
/// {
///     { "host1", 7687 },
///     { "host2", 7688 }
/// };
/// </code>
/// Using <see cref="ServerAddress"/> directly:
/// <code>
/// var resolver = new ListAddressResolver
/// {
///     ServerAddress.From("host1", 7687),
///     ServerAddress.From("host2", 7688)
/// };
/// </code>
/// </example>
public sealed class ListAddressResolver : IServerAddressResolver, IEnumerable<ServerAddress>
{
    private readonly List<ServerAddress> _addresses = [];

    /// <summary>
    /// Initializes an empty <see cref="ListAddressResolver"/>. Use collection initializer syntax or
    /// <see cref="Add(ServerAddress)"/> to populate the list.
    /// </summary>
    public ListAddressResolver()
    {
    }

    /// <summary>Initializes a <see cref="ListAddressResolver"/> with the given addresses.</summary>
    /// <param name="addresses">The fixed set of addresses to return from <see cref="Resolve"/>.</param>
    public ListAddressResolver(params ServerAddress[] addresses)
    {
        _addresses.AddRange(addresses);
    }

    /// <summary>Adds a <see cref="ServerAddress"/> to the list.</summary>
    /// <param name="address">The address to add.</param>
    public void Add(ServerAddress address) => _addresses.Add(address);

    /// <summary>Adds an address specified by host and port to the list.</summary>
    /// <param name="host">The host name.</param>
    /// <param name="port">The port number.</param>
    public void Add(string host, int port) => _addresses.Add(ServerAddress.From(host, port));

    /// <summary>
    /// Adds an address specified as a <c>"host:port"</c> string to the list.
    /// </summary>
    /// <param name="hostAndPort">
    /// A string in the form <c>"host:port"</c>, e.g. <c>"myserver:7687"</c>.
    /// </param>
    /// <exception cref="FormatException">
    /// Thrown if <paramref name="hostAndPort"/> is not in the expected <c>"host:port"</c> format or the port is not
    /// a valid integer.
    /// </exception>
    public void Add(string hostAndPort)
    {
        var lastColon = hostAndPort.LastIndexOf(':');
        if (lastColon < 0 || !int.TryParse(hostAndPort.AsSpan(lastColon + 1), out var port))
        {
            throw new FormatException(
                $"Expected a string in the form \"host:port\" but got \"{hostAndPort}\".");
        }

        _addresses.Add(ServerAddress.From(hostAndPort[..lastColon], port));
    }

    /// <inheritdoc/>
    public ISet<ServerAddress> Resolve(ServerAddress address) => new HashSet<ServerAddress>(_addresses);

    /// <inheritdoc/>
    public IEnumerator<ServerAddress> GetEnumerator() => _addresses.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

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
using System.Linq;

namespace Neo4j.Driver.Preview;

/// <summary>
/// Configures the driver with multiple initial addresses for higher resilience during cluster discovery.
/// Use as an alternative to a single URI string when creating a driver via
/// <c>GraphDatabase.Driver(MultiAddress, ...)</c> (requires <c>using Neo4j.Driver.Preview</c>).
/// </summary>
/// <remarks>
/// <b>Preview API.</b> This feature is a preview and may change or be removed in a future release.<br/>
/// <br/>
/// At least one address must be provided. When using a direct (bolt) scheme, exactly one address must be
/// provided. Each address in <see cref="Addresses"/> will be tried in order when establishing the initial
/// connection until the first one succeeds.
/// </remarks>
public sealed class MultiAddress
{
    /// <summary>
    /// Gets the URI scheme, e.g. <c>"neo4j"</c>, <c>"neo4j+s"</c>, or <c>"neo4j+ssc"</c>.
    /// </summary>
    public string Scheme { get; }

    /// <summary>
    /// Gets the optional routing context query string, e.g. <c>"region=eu"</c>.
    /// Defaults to an empty string (no routing context).
    /// </summary>
    public string Query { get; }

    /// <summary>
    /// Gets the ordered list of initial addresses to try when connecting to the cluster.
    /// </summary>
    public IReadOnlyList<ServerAddress> Addresses { get; }

    /// <summary>
    /// Creates a new <see cref="MultiAddress"/> with no routing context query string.
    /// </summary>
    /// <param name="scheme">
    /// The URI scheme to use, e.g. <c>"neo4j"</c>, <c>"neo4j+s"</c>, <c>"neo4j+ssc"</c>.
    /// </param>
    /// <param name="addresses">
    /// The ordered list of server addresses. Must contain at least one entry.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="scheme"/> or <paramref name="addresses"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="addresses"/> is empty.</exception>
    public MultiAddress(string scheme, IEnumerable<ServerAddress> addresses)
        : this(scheme, string.Empty, addresses)
    {
    }

    /// <summary>
    /// Creates a new <see cref="MultiAddress"/> with a routing context query string.
    /// </summary>
    /// <param name="scheme">
    /// The URI scheme to use, e.g. <c>"neo4j"</c>, <c>"neo4j+s"</c>, <c>"neo4j+ssc"</c>.
    /// </param>
    /// <param name="query">
    /// The routing context as a query string, e.g. <c>"region=eu"</c>. Use an empty string or
    /// <c>null</c> for no routing context.
    /// </param>
    /// <param name="addresses">
    /// The ordered list of server addresses. Must contain at least one entry.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="scheme"/> or <paramref name="addresses"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="addresses"/> is empty.</exception>
    public MultiAddress(string scheme, string query, IEnumerable<ServerAddress> addresses)
    {
        Scheme = scheme ?? throw new ArgumentNullException(nameof(scheme));
        Query = query ?? string.Empty;

        var list = addresses?.ToList() ?? throw new ArgumentNullException(nameof(addresses));
        if (list.Count == 0)
        {
            throw new ArgumentException("At least one address must be provided.", nameof(addresses));
        }

        Addresses = list.AsReadOnly();
    }

    internal Uri ToCanonicalUri()
    {
        var first = Addresses[0];
        var builder = new UriBuilder(Scheme, first.Host, first.Port);
        if (!string.IsNullOrEmpty(Query))
        {
            builder.Query = Query;
        }

        return builder.Uri;
    }
}

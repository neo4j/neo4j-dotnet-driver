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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;

namespace Neo4j.Driver.Internal.Routing;

internal interface IClusterConnectionPool : IAsyncDisposable
{
    // Try to acquire a connection with the server specified by the uri
    Task<IConnection> AcquireAsync(
        Uri uri,
        AccessMode mode,
        string database,
        SessionConfig sessionConfig,
        Bookmarks bookmarks,
        bool forceAuth);

    // Add a set of uri to this pool
    Task AddAsync(IEnumerable<Uri> uris);

    // Update the pool keys with the new server uris
    Task UpdateAsync(IEnumerable<Uri> added, IEnumerable<Uri> removed);

    // Deactivate all the connection pool with the server specified by the uri
    Task DeactivateAsync(Uri uri);

    // Get number of in-use connections for the uri
    int NumberOfInUseConnections(Uri uri);
}

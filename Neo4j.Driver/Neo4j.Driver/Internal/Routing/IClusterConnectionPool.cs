// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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

namespace Neo4j.Driver.Internal.Routing
{
    internal interface IClusterConnectionPool
    {
        // Try to acquire a connection with the server specified by the uri
        bool TryAcquire(Uri uri, out IPooledConnection conn);
        // Release the connection back to the server connection pool specified by the uri
        void Release(Uri uri, Guid id);
        // Add a pool for a new uri
        void Add(Uri uri);
        // Remove all the connections with the server specified by the uri
        void Purge(Uri uri);
        // Purge all
        void Clear();
        // Test if we have established connections with the server specified by the uri
        bool HasAddress(Uri uri);
    }
}
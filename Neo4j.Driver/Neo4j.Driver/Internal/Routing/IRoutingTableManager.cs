﻿// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
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
using System.Threading.Tasks;
using Neo4j.Driver;

namespace Neo4j.Driver.Internal.Routing
{
    internal interface IRoutingTableManager
    {
        Task<IRoutingTable> EnsureRoutingTableForModeAsync(AccessMode mode, string database, Bookmark bookmark);
        void Clear();
        void ForgetServer(Uri uri, string database);
        void ForgetWriter(Uri uri, string database);
        IRoutingTable RoutingTableFor(string database);
    }
}
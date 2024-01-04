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

namespace Neo4j.Driver.Tests.TestBackend;

internal class BookmarksSupplierCompleted : ProtocolObject
{
#pragma warning disable CS0649 // field will only be assigned to during deserialization from JSON message
    public BookmarksSupplierCompletedDto data;
#pragma warning restore CS0649

    public class BookmarksSupplierCompletedDto
    {
        public string requestId { get; set; }
        public string[] bookmarks { get; set; }
    }
}

// Copyright (c) 2002-2020 "Neo4j,"
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

using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Internal.Messaging.V3;

namespace Neo4j.Driver.Internal
{
    internal static class BookmarkHelper
    {
        public const string BookmarksKey = "bookmarks";

        public static IDictionary<string, object> AsBeginTransactionParameters(this Bookmark bookmark)
        {
            if (bookmark != null && bookmark.Values.Any())
            {
                return new Dictionary<string, object>
                {
                    {BookmarksKey, bookmark.Values}
                };
            }

            return null;
        }
    }
}
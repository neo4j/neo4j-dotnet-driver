// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.MessageHandling.Metadata;

namespace Neo4j.Driver.Internal.MessageHandling.V1
{
    internal class CommitResponseHandler : MetadataCollectingResponseHandler
    {
        private readonly IBookmarkTracker _tracker;

        public CommitResponseHandler(IBookmarkTracker tracker)
        {
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));

            AddMetadata<BookmarkCollector, Bookmark>();
        }

        public override Task OnSuccessAsync(IDictionary<string, object> metadata)
        {
            var result = base.OnSuccessAsync(metadata);

            _tracker.UpdateBookmark(GetMetadata<BookmarkCollector, Bookmark>());

            return result;
        }
    }
}
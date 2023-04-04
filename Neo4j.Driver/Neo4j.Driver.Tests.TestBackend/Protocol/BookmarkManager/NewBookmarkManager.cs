// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class NewBookmarkManager : IProtocolObject
{
    public NewBookmarkManagerDto data { get; set; } = new();

    [JsonIgnore] public IBookmarkManager BookmarkManager { get; set; }

    public override Task Process(Controller controller)
    {
        var initialBookmarks = data.initialBookmarks ?? Array.Empty<string>();

        async Task<string[]> BookmarkSupplier(CancellationToken _)
        {
            if (!data.bookmarksSupplierRegistered)
            {
                return Array.Empty<string>();
            }

            var request = new BookmarkManagerSupplierRequest(ObjManager);

            await controller.SendResponse(GetSupplyRequest(request));
            var result = await controller.TryConsumeStreamObjectOfType<BookmarksSupplierCompleted>();

            return result.data.bookmarks;
        }

        async Task NotifyBookmarks(string[] bookmarks, CancellationToken _)
        {
            if (!data.bookmarksConsumerRegistered)
            {
                return;
            }

            var request = new BookmarkManagerConsumerRequest(ObjManager);

            await controller.SendResponse(GetConsumeRequest(bookmarks, request));
            await controller.TryConsumeStreamObjectOfType<BookmarksConsumerCompleted>();
        }

        BookmarkManager =
            Preview.GraphDatabase.BookmarkManagerFactory.NewBookmarkManager(
                new BookmarkManagerConfig(initialBookmarks, BookmarkSupplier, NotifyBookmarks));

        return Task.CompletedTask;
    }

    private string GetConsumeRequest(string[] bookmarks, BookmarkManagerConsumerRequest request)
    {
        return new ProtocolResponse(
            "BookmarksConsumerRequest",
            new { bookmarks, bookmarkManagerId = uniqueId, id = request.uniqueId }).Encode();
    }

    private string GetSupplyRequest(BookmarkManagerSupplierRequest request)
    {
        return new ProtocolResponse(
            "BookmarksSupplierRequest",
            new { bookmarkManagerId = uniqueId, id = request.uniqueId }).Encode();
    }

    public override string Respond()
    {
        return new ProtocolResponse("BookmarkManager", new { id = uniqueId }).Encode();
    }

    public class NewBookmarkManagerDto
    {
        public string[] initialBookmarks { get; set; }
        public bool bookmarksSupplierRegistered { get; set; }
        public bool bookmarksConsumerRegistered { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class NewBookmarkManager : IProtocolObject
    {
        public NewBookmarkManagerDto data { get; set; } = new NewBookmarkManagerDto();

        [JsonIgnore] public IBookmarkManager BookmarkManager { get; set; }

        public class NewBookmarkManagerDto
        {
            public string[] initialBookmarks { get; set; }
            public bool bookmarksSupplierRegistered { get; set; }
            public bool bookmarksConsumerRegistered { get; set; }
        }

        public override Task Process(Controller controller)
        {
            var initialBookmarks =
                data.initialBookmarks
                ?? Array.Empty<string>();

            async Task<string[]> BookmarkSupplier(CancellationToken _)
            {
                if (!data.bookmarksSupplierRegistered)
                    return Array.Empty<string>();

                var request = new BookmarkManagerSupplierRequest(ObjManager);
                
                await controller.SendResponse(GetSupplyRequest(request));
                var result = await controller.TryConsumeStreamObjectOfType<BookmarksSupplierCompleted>();

                return result.data.bookmarks;
            }

            async Task NotifyBookmarks(string[] bookmarks, CancellationToken _)
            {
                if (!data.bookmarksConsumerRegistered)
                    return;

                var request = new BookmarkManagerConsumerRequest(ObjManager);

                await controller.SendResponse(GetConsumeRequest(bookmarks, request));
                await controller.TryConsumeStreamObjectOfType<BookmarksConsumerCompleted>();
            }

            BookmarkManager =
                Experimental.GraphDatabase.BookmarkManagerFactory.NewBookmarkManager(
                    new BookmarkManagerConfig(initialBookmarks, BookmarkSupplier, NotifyBookmarks));

            return Task.CompletedTask;
        }

        private string GetConsumeRequest(string[] bookmarks, BookmarkManagerConsumerRequest request)
        {
            return new ProtocolResponse("BookmarksConsumerRequest",
                new {bookmarks, bookmarkManagerId = uniqueId, id = request.uniqueId}).Encode();
        }

        private string GetSupplyRequest(BookmarkManagerSupplierRequest request)
        {
            return new ProtocolResponse("BookmarksSupplierRequest",
                new {bookmarkManagerId = uniqueId, id = request.uniqueId}).Encode();
        }

        public override string Respond()
        {
            return new ProtocolResponse("BookmarkManager", new {id = uniqueId}).Encode();
        }
    }
}
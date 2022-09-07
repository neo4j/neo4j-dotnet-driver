using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class NewBookmarkManager : ProtocolObject
    {
        public NewBookmarkManagerDto data { get; set; } = new NewBookmarkManagerDto();

        [JsonIgnore] public IBookmarkManager BookmarkManager { get; set; }

        public class NewBookmarkManagerDto
        {
            public Dictionary<string, string[]> initialBookmarks { get; set; }
            public bool bookmarksSupplierRegistered { get; set; }
            public bool bookmarksConsumerRegistered { get; set; }
        }

        public override Task ProcessAsync(Controller controller)
        {
            var initialBookmarks =
                data.initialBookmarks?.ToDictionary(x => x.Key, x => x.Value as IEnumerable<string>)
                ?? new Dictionary<string, IEnumerable<string>>();

            async Task<string[]> BookmarkSupplier(string database, CancellationToken _)
            {
                if (!data.bookmarksSupplierRegistered)
                    return Array.Empty<string>();

                var request = new BookmarkManagerSupplierRequest(ObjManager);
                
                await controller.SendResponseAsync(GetSupplyRequest(database, request));
                var result = await controller.TryConsumeStreamObjectAsync<BookmarksSupplierCompleted>();

                return result.data.bookmarks;
            }

            async Task NotifyBookmarks(string database, string[] bookmarks, CancellationToken _)
            {
                if (!data.bookmarksConsumerRegistered)
                    return;

                var request = new BookmarkManagerConsumerRequest(ObjManager);

                await controller.SendResponseAsync(GetConsumeRequest(database, bookmarks, request));
                await controller.TryConsumeStreamObjectAsync<BookmarksConsumerCompleted>();
            }

            BookmarkManager =
                Experimental.GraphDatabase.BookmarkManagerFactory.NewBookmarkManager(
                    new BookmarkManagerConfig(initialBookmarks, BookmarkSupplier, NotifyBookmarks));

            return Task.CompletedTask;
        }

        public override Task ReactiveProcessAsync(Controller controller)
        {
            return ProcessAsync(controller);
        }

        private string GetConsumeRequest(string database, string[] bookmarks, BookmarkManagerConsumerRequest request)
        {
            return new ProtocolResponse("BookmarksConsumerRequest",
                new {database, bookmarks, bookmarkManagerId = UniqueId, id = request.UniqueId}).Encode();
        }

        private string GetSupplyRequest(string database, BookmarkManagerSupplierRequest request)
        {
            return new ProtocolResponse("BookmarksSupplierRequest",
                new {database, bookmarkManagerId = UniqueId, id = request.UniqueId}).Encode();
        }

        public override string Respond()
        {
            return new ProtocolResponse("BookmarkManager", new {id = UniqueId}).Encode();
        }
    }
}
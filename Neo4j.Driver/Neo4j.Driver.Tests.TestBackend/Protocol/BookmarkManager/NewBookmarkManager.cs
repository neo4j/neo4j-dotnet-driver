using System;
using System.Collections.Generic;
using System.Linq;
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
            public Dictionary<string, string[]> initialBookmarks { get; set; }
            public bool bookmarksSupplierRegistered { get; set; }
            public bool bookmarksConsumerRegistered { get; set; }
        }

        public override Task Process(Controller controller)
        {
            var initialBookmarks =
                data.initialBookmarks?.ToDictionary(x => x.Key, x => x.Value as IEnumerable<string>)
                ?? new Dictionary<string, IEnumerable<string>>();

            string[] BookmarkSupplier(string database)
            {
                if (!data.bookmarksSupplierRegistered)
                    return Array.Empty<string>();

                var request = new BookmarkManagerSupplierRequest(ObjManager);
                
                controller.SendResponse(GetSupplyRequest(database, request))
                    .GetAwaiter()
                    .GetResult();
                var result = controller.TryConsumeStreamObjectOfType<BookmarksSupplierCompleted>()
                    .GetAwaiter()
                    .GetResult();

                return result.data.bookmarks;
            }

            void NotifyBookmarks(string database, string[] bookmarks)
            {
                if (!data.bookmarksConsumerRegistered)
                    return;

                var request = new BookmarkManagerConsumerRequest(ObjManager);

                controller.SendResponse(GetConsumeRequest(database, bookmarks, request))
                    .GetAwaiter()
                    .GetResult();
                controller.TryConsumeStreamObjectOfType<BookmarksConsumerCompleted>()
                    .GetAwaiter()
                    .GetResult();
            }

            BookmarkManager =
                GraphDatabase.BookmarkManagerFactory.NewBookmarkManager(
                    new BookmarkManagerConfig(initialBookmarks, BookmarkSupplier, NotifyBookmarks));

            return Task.CompletedTask;
        }

        private string GetConsumeRequest(string database, string[] bookmarks, BookmarkManagerConsumerRequest request)
        {
            return new ProtocolResponse("BookmarksConsumerRequest",
                new {database, bookmarks, bookmarkManagerId = uniqueId, id = request.uniqueId}).Encode();
        }

        private string GetSupplyRequest(string database, BookmarkManagerSupplierRequest request)
        {
            return new ProtocolResponse("BookmarksSupplierRequest",
                new {database, bookmarkManagerId = uniqueId, id = request.uniqueId}).Encode();
        }

        public override string Respond()
        {
            return new ProtocolResponse("BookmarkManager", new {id = uniqueId}).Encode();
        }
    }
}
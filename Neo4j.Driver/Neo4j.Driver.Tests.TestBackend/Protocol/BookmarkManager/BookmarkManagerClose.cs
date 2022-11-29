namespace Neo4j.Driver.Tests.TestBackend
{
    internal class BookmarkManagerClose : IProtocolObject
    {
        public BookmarkManagerCloseDto data;

        public class BookmarkManagerCloseDto
        {
            public string id { get; set; }
        }

        public override string Respond()
        {
            return new ProtocolResponse("BookmarkManager", new {data.id}).Encode();
        }
    }
}

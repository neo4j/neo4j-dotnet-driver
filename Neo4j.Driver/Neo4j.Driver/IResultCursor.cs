using System.Collections.Generic;

namespace Neo4j.Driver
{
    public interface IResultCursor
    {
        IEnumerable<Record> Stream();
        //        ResultSummary summarize();
        Record Record();
        long Position();
        bool Next();
    }

    public interface IExtendedResultCursor : IResultCursor
    {
        bool AtEnd();
        long Skip(long records);
        long Limit(long records);
        bool First();
        bool Single();
        Record Peek();
        IList<Record> List();
    }
}
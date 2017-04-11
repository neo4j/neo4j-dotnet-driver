using System;
using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Routing
{
    internal interface IRoutingTable
    {
        bool IsStale();
        bool TryNextRouter(out Uri uri);
        bool TryNextReader(out Uri uri);
        bool TryNextWriter(out Uri uri);
        void Remove(Uri uri);
        void RemoveWriter(Uri uri);
        ISet<Uri> All();
        void Clear();
        void AddRouter(IEnumerable<Uri> uris);
    }
}
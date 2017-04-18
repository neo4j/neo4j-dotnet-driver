using System;
using System.Collections.Generic;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal interface IRoutingTable
    {
        bool IsStale(AccessMode mode);
        bool TryNextRouter(out Uri uri);
        bool TryNextReader(out Uri uri);
        bool TryNextWriter(out Uri uri);
        void Remove(Uri uri);
        void RemoveWriter(Uri uri);
        ISet<Uri> All();
        void Clear();
        void PrependRouters(IEnumerable<Uri> uris);
    }
}